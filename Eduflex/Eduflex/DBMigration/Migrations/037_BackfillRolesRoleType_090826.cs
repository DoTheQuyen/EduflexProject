using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // RoleModel.RoleType (ShareService.Enums.Roles.RoleTypeEnums) was added after the
    // original Admin/Student/Staff/Manager roles were seeded, so those documents predate
    // the field entirely and deserialize with RoleType defaulting to 0 — not a valid named
    // enum member (Admin starts at 1). Every RoleType-based check added since (role
    // hierarchy in UserService, default-Student-role lookups in StudentService/
    // EnrolmentService, the "no student members" check in DepartmentService) silently
    // fails to match these roles until this is backfilled.
    public class _037_BackfillRolesRoleType_090826 : SafeMigrationBase
    {
        public override string MigrationId => "_037_BackfillRolesRoleType_090826";
        public override string Name => "Backfill Roles.roleType for pre-existing roles";
        public override string Description => "Sets roleType on any Role document that predates the field, mapping the well-known seeded role names (Admin/Manager/Staff/Student) to their RoleTypeEnums value. Any other role missing roleType is left alone and flagged for manual follow-up, since its intended type can't be safely guessed from its name.";

        // Mirrors ShareService.Enums.Roles.RoleTypeEnums exactly (Admin=1, Manager=2,
        // Staff=3, Student=4, Customer=5). Duplicated here rather than referenced because
        // DBMigration doesn't take a project reference on that enum's assembly — same
        // pattern as every other migration in this folder that writes raw BsonDocuments.
        private static readonly Dictionary<string, int> RoleTypeByName = new()
        {
            ["Admin"] = 1,
            ["Manager"] = 2,
            ["Staff"] = 3,
            ["Student"] = 4,
        };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Skipping.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var missingRoleTypeFilter = Builders<BsonDocument>.Filter.Exists("roleType", false);

            foreach (var (name, roleType) in RoleTypeByName)
            {
                var filter = Builders<BsonDocument>.Filter.And(
                    missingRoleTypeFilter,
                    Builders<BsonDocument>.Filter.Eq("name", name));

                var update = Builders<BsonDocument>.Update.Set("roleType", roleType);
                var result = await rolesCollection.UpdateManyAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    Console.WriteLine($"✅ Backfilled roleType={roleType} on {result.ModifiedCount} role(s) named \"{name}\"");
                }
            }

            var stillMissing = await rolesCollection
                .Find(missingRoleTypeFilter)
                .Project(Builders<BsonDocument>.Projection.Include("name"))
                .ToListAsync();

            if (stillMissing.Count > 0)
            {
                var names = string.Join(", ", stillMissing.Select(d => d.GetValue("name", "(unnamed)").ToString()));
                Console.WriteLine($"⚠️ {stillMissing.Count} role(s) still missing roleType and weren't a recognised seeded name — set their Role Type manually in Roles management: {names}");
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
                return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");

            foreach (var (name, roleType) in RoleTypeByName)
            {
                // Best-effort: unsets roleType only where it still matches what this
                // migration would have set. If someone deliberately changed a backfilled
                // role's type afterward to that same value, this will also revert it —
                // no way to distinguish the two without a migration-run marker, same
                // trade-off migration 010's Down() accepts for its own role reversal.
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("name", name),
                    Builders<BsonDocument>.Filter.Eq("roleType", roleType));

                var result = await rolesCollection.UpdateManyAsync(filter, Builders<BsonDocument>.Update.Unset("roleType"));
                if (result.ModifiedCount > 0)
                {
                    Console.WriteLine($"✅ Reverted roleType on {result.ModifiedCount} role(s) named \"{name}\"");
                }
            }
        }
    }
}