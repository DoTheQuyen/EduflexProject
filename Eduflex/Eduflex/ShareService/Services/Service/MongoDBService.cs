using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShareService.Models;

namespace ShareService.Services.Service
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<UserModel> Users => _database.GetCollection<UserModel>("users");
        public IMongoCollection<StudentModel> Students => _database.GetCollection<StudentModel>("students");
        //public IMongoCollection<Course> Courses => _database.GetCollection<Course>("courses");
        //public IMongoCollection<Institution> Institutions => _database.GetCollection<Institution>("institutions");
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}