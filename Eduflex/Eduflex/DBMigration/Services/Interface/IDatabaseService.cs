using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBMigration.Services.Interface
{
    public interface IDatabaseService
    {
        Task<bool> TestConnectionAsync();
        string GetOutputDirectory();
        Task<BsonDocument> GetCollection(string collectionName);
        Task DropCollectionsAsync();
        Task<List<string>> GetCollectionNamesAsync();
        Task<long> GetCollectionCountAsync(string collectionName);
        Task InsertTestDataAsync();
        Task ClearTestDataAsync();
        Task ExportReferenceDataAsync();
    }
}
