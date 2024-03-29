using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Dapper.Interfaces;
using Credfeto.Database.Dapper.Interfaces.Builders;
using Dapper;
using Polly;
using Polly.Retry;

namespace Credfeto.Database.Dapper;

public abstract class Database : IDatabase
{
    private const int MAX_RETRIES = 3;
    private readonly IAsyncPolicy _retryPolicyAsync;

    protected Database()
    {
        this._retryPolicyAsync = this.DefineAsyncPolicy();
    }

    public async Task<int> ExecuteAsync(string storedProcedure)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        await using (DbConnection connection = await this.GetConnectionAsync(cancellationToken))
        {
            // ReSharper disable once AccessToDisposedClosure
            return await this.ExecuteWithRetriesAsync(func: () => InternalExecuteAsync(storedProcedure: storedProcedure, param: null, connection: connection),
                                                      context: storedProcedure);
        }
    }

    public async Task<int> ExecuteAsync<TQueryParameters>(string storedProcedure, TQueryParameters param)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        await using (DbConnection connection = await this.GetConnectionAsync(cancellationToken))
        {
            // ReSharper disable once AccessToDisposedClosure
            return await this.ExecuteWithRetriesAsync(func: () => InternalExecuteAsync(storedProcedure: storedProcedure, param: param, connection: connection),
                                                      context: storedProcedure);
        }
    }

    public async Task<int> ExecuteArbitrarySqlAsync(string sql)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        await using (DbConnection connection = await this.GetConnectionAsync(cancellationToken))
        {
            // ReSharper disable once AccessToDisposedClosure
            return await this.ExecuteWithRetriesAsync(func: () => connection.ExecuteAsync(sql: sql, commandType: CommandType.Text), context: sql);
        }
    }

    public async Task<TResult> QuerySingleAsync<TSourceObject, TResult>(IObjectBuilder<TSourceObject, TResult> builder, string storedProcedure)
        where TSourceObject : class, new() where TResult : class
    {
        IReadOnlyList<TSourceObject> result = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: null);

        return ExtractUnique(builder: builder, result: result);
    }

    public async Task<TResult> QuerySingleAsync<TQueryParameters, TSourceObject, TResult>(IObjectBuilder<TSourceObject, TResult> builder,
                                                                                          string storedProcedure,
                                                                                          TQueryParameters param)
        where TSourceObject : class, new() where TResult : class
    {
        IReadOnlyList<TSourceObject> result = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: param);

        return ExtractUnique(builder: builder, result: result);
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TSourceObject, TResult>(IObjectBuilder<TSourceObject, TResult> builder, string storedProcedure)
        where TSourceObject : class, new() where TResult : class
    {
        IReadOnlyList<TSourceObject> result = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: null);

        return ExtractAndConvertSingle(builder: builder, result: result);
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TQueryParameters, TSourceObject, TResult>(IObjectBuilder<TSourceObject, TResult> builder,
                                                                                                    string storedProcedure,
                                                                                                    TQueryParameters param)
        where TSourceObject : class, new() where TResult : class
    {
        IReadOnlyList<TSourceObject> result = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: param);

        return ExtractAndConvertSingle(builder: builder, result: result);
    }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TSourceObject, TResult>(IObjectCollectionBuilder<TSourceObject, TResult> builder, string storedProcedure)
        where TSourceObject : class, new() where TResult : class
    {
        IEnumerable<TSourceObject> results = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: null);

        return builder.Build(results);
    }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TQueryParameters, TSourceObject, TResult>(IObjectCollectionBuilder<TSourceObject, TResult> builder,
                                                                                                   string storedProcedure,
                                                                                                   TQueryParameters param)
        where TSourceObject : class, new() where TResult : class
    {
        IEnumerable<TSourceObject> results = await this.InternalQueryAsync<TSourceObject>(storedProcedure: storedProcedure, param: param);

        return builder.Build(results);
    }

    public async Task<IReadOnlyList<TResult>> QueryArbitrarySqlAsync<TResult>(string sql)
        where TResult : new()
    {
        if (string.IsNullOrEmpty(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        CancellationToken cancellationToken = CancellationToken.None;

        await using (DbConnection connection = await this.GetConnectionAsync(cancellationToken))
        {
            // ReSharper disable once AccessToDisposedClosure - The IEnumerable is enumerated inside the using statement, connection can't be disposed before
            // that happens
            IEnumerable<TResult> result = await this.ExecuteWithRetriesAsync(func: () => connection.QueryAsync<TResult>(sql: sql, commandType: CommandType.Text), context: sql);

            return [..result];
        }
    }

    private static TResult? ExtractAndConvertSingle<TSourceObject, TResult>(IObjectBuilder<TSourceObject, TResult> builder, IReadOnlyList<TSourceObject> result)
        where TSourceObject : class, new() where TResult : class
    {
        TSourceObject? item = result.SingleOrDefault();

        return item != null
            ? builder.Build(item)
            : null;
    }

    private AsyncRetryPolicy DefineAsyncPolicy()
    {
        return Policy.Handle((Func<Exception, bool>)this.IsTransientException)
                     .WaitAndRetryAsync(retryCount: MAX_RETRIES,
                                        sleepDurationProvider: RetryDelayCalculator.Calculate,
                                        onRetry: (exception, delay, retryCount, context) =>
                                                 {
                                                     this.LogAndDispatchTransientExceptions(exception: exception,
                                                                                            context: context,
                                                                                            delay: delay,
                                                                                            retryCount: retryCount,
                                                                                            maxRetries: MAX_RETRIES);
                                                 });
    }

    protected abstract bool IsTransientException(Exception exception);

    protected abstract void LogAndDispatchTransientExceptions(Exception exception, Context context, in TimeSpan delay, int retryCount, int maxRetries);

    protected abstract ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);

    private static TReturn ExtractUnique<TSourceObject, TReturn>(IObjectBuilder<TSourceObject, TReturn> builder, IReadOnlyList<TSourceObject> result)
        where TSourceObject : class, new() where TReturn : class
    {
        TReturn? final = ExtractAndConvertSingle(builder: builder, result: result);

        if (final == null)
        {
            ThrowsNoMatch();
        }

        return final;
    }

    [DoesNotReturn]
    private static void ThrowsNoMatch()
    {
        throw new InvalidOperationException(message: "No match");
    }

    private static Task<int> InternalExecuteAsync(string storedProcedure, object? param, IDbConnection connection)
    {
        return connection.ExecuteAsync(sql: storedProcedure, param: param, commandType: CommandType.StoredProcedure);
    }

    private async Task<IReadOnlyList<TReturn>> InternalQueryAsync<TReturn>(string storedProcedure, object? param)
        where TReturn : new()
    {
        CancellationToken cancellationToken = CancellationToken.None;

        await using (DbConnection connection = await this.GetConnectionAsync(cancellationToken))
        {
            // ReSharper disable once AccessToDisposedClosure - The IEnumerable is enumerated inside the using statement, connection can't be disposed before
            // that happens
            IEnumerable<TReturn> result =
                await this.ExecuteWithRetriesAsync(func: () => connection.QueryAsync<TReturn>(sql: storedProcedure, param: param, commandType: CommandType.StoredProcedure),
                                                   context: storedProcedure);

            return [..result];
        }
    }

    private async Task<T1> ExecuteWithRetriesAsync<T1>(Func<Task<T1>> func, string context)
    {
        Context loggingContext = new(context);

        T1 result = await this._retryPolicyAsync.ExecuteAsync(action: WrappedAsync, context: loggingContext);

        return result;

        Task<T1> WrappedAsync(Context c)
        {
            return func();
        }
    }
}