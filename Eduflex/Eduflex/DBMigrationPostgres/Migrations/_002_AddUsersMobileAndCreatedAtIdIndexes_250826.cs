using DBMigrationPostgres.Services.Services;

namespace DBMigrationPostgres.Migrations
{
    public class _002_AddUsersMobileAndCreatedAtIdIndexes_250826 : SafePgMigrationBase
    {
        public override string MigrationId => "002_AddUsersMobileAndCreatedAtIdIndexes_250826";
        public override string Name => "Add Users Mobile and CreatedAt/Id Indexes";
        public override string Description => "Adds an index on Users.Mobile for exact-match lookups (GetUserByMobileAsync) and a composite (CreatedAt DESC, Id DESC) index for keyset pagination";

        protected override async Task UpCore()
        {
            await CreateIndexAsync("IX_Users_Mobile", "Users", "Mobile");

            // Kept as raw SQL — the DESC sort direction on each column is what makes this
            // index usable for keyset pagination's ORDER BY CreatedAt DESC, Id DESC, and
            // CreateIndexAsync's column list doesn't carry per-column sort direction.
            await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS \"IX_Users_CreatedAt_Id\" ON \"Users\" (\"CreatedAt\" DESC, \"Id\" DESC);");
        }

        protected override async Task DownCore()
        {
            await DropIndexAsync("IX_Users_Mobile");
            await DropIndexAsync("IX_Users_CreatedAt_Id");
        }
    }
}
