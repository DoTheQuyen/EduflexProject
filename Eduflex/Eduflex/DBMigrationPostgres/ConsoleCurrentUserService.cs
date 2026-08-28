using ShareService.Common;

namespace DBMigrationPostgres;

// AuditableDbSetBase<T> needs an ICurrentUserService to stamp CreatedBy/UpdatedBy — the real
// API resolves this from the HTTP request, but this console tool has no request, so every
// row it touches is attributed to a fixed system identity instead.
public class ConsoleCurrentUserService : ICurrentUserService
{
    public string? UserId => "migration-tool";
}
