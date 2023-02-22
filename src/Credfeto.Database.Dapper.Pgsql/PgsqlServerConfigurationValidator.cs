using FluentValidation;

namespace Credfeto.Database.Dapper.Pgsql;

public sealed class PgsqlServerConfigurationValidator : AbstractValidator<PgsqlServerConfiguration>
{
    public PgsqlServerConfigurationValidator()
    {
        this.RuleFor(x => x.ConnectionString)
            .NotEmpty();
    }
}