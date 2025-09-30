using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Services
{
    public abstract class SafeMigrationBase : IMigration
    {
        public abstract string MigrationId { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract Task Up(IMongoDatabase database);
        public abstract Task Down(IMongoDatabase database);

        protected async Task<bool> IndexExistsAsync(IMongoDatabase database, string collectionName, string indexName)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var indexes = await collection.Indexes.ListAsync();
            var indexList = await indexes.ToListAsync();
            return indexList.Any(index => index["name"] == indexName);
        }

        protected async Task<List<string>> GetExistingIndexNamesAsync(IMongoDatabase database, string collectionName)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var indexes = await collection.Indexes.ListAsync();
            var indexList = await indexes.ToListAsync();
            return indexList.Select(index => index["name"].ToString()).ToList();
        }

        protected async Task CreateIndexSafeAsync(IMongoDatabase database, string collectionName, CreateIndexModel<BsonDocument> model)
        {
            if (!await IndexExistsAsync(database, collectionName, model.Options.Name))
            {
                var collection = database.GetCollection<BsonDocument>(collectionName);
                await collection.Indexes.CreateOneAsync(model);
            }
        }

        protected async Task DropIndexSafeAsync(IMongoDatabase database, string collectionName, string indexName)
        {
            if (await IndexExistsAsync(database, collectionName, indexName))
            {
                var collection = database.GetCollection<BsonDocument>(collectionName);
                await collection.Indexes.DropOneAsync(indexName);
            }
        }

        protected async Task<bool> CollectionExistsAsync(IMongoDatabase database, string collectionName)
        {
            var collections = await database.ListCollectionNamesAsync();
            var collectionList = await collections.ToListAsync();
            return collectionList.Contains(collectionName);
        }

        protected async Task<bool> FieldExistsAsync(IMongoDatabase database, string collectionName, string fieldName)
        {
            if (!await CollectionExistsAsync(database, collectionName))
                return false;

            var collection = database.GetCollection<BsonDocument>(collectionName);
            var document = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
            return document != null && document.Contains(fieldName);
        }

        protected ObjectId ParseObjectId(string id)
        {
            if (ObjectId.TryParse(id, out var objectId))
                return objectId;
            throw new ArgumentException($"Invalid ObjectId: {id}");
        }
    }
}