using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _036_RestrictStudentsDeletePermission_060826 : SafeMigrationBase
    {
        public override string MigrationId => "_036_RestrictStudentsDeletePermission_060826";
        public override string Name => "Restrict Students Delete Permission";
        public override string Description => "Deactivating a student is moving from the Manage Students list into the new Student Details page, and is restricted to Manager/Admin — front-line Staff keeps View/Add/Edit on Students but loses Delete (deactivate).";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Skipping.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var staffRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Staff")).FirstOrDefaultAsync();
            if (staffRole == null)
            {
                Console.WriteLine("⚠️ Staff role not found. Skipping.");
                return;
            }

            var deletePermission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "StudentsDelete")).FirstOrDefaultAsync();
            if (deletePermission == null)
            {
                Console.WriteLine("⚠️ StudentsDelete permission not found. Skipping.");
                return;
            }

            var pull = Builders<BsonDocument>.Update.Pull("permissionIds", deletePermission["_id"].AsObjectId.ToString());
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", staffRole["_id"].AsObjectId), pull);

            Console.WriteLine("✅ Removed StudentsDelete from Staff — only Admin and Manager can deactivate students now.");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles")) return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var staffRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Staff")).FirstOrDefaultAsync();
            var deletePermission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "StudentsDelete")).FirstOrDefaultAsync();
            if (staffRole == null || deletePermission == null) return;

            var addBack = Builders<BsonDocument>.Update.AddToSet("permissionIds", deletePermission["_id"].AsObjectId.ToString());
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", staffRole["_id"].AsObjectId), addBack);
        }
    }
}
