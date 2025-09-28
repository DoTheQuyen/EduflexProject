using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Eduflex.API.Models;

namespace Eduflex.API.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<Student> Students => _database.GetCollection<Student>("students");
        //public IMongoCollection<Course> Courses => _database.GetCollection<Course>("courses");
        //public IMongoCollection<Institution> Institutions => _database.GetCollection<Institution>("institutions");
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}