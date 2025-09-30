using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models;

namespace ShareService.DataAccess
{
    public class Authentication : IAuthentication
    {
        private readonly IMongoCollection<UserModel> _usersCollection;

        public Authentication(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<UserModel>("Users");
            // Remove index creation since it already exists in the database
        }

        public async Task<UserModel?> FindByEmailAsync(string email)
        {
            return await _usersCollection
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.LastLogin, DateTime.UtcNow);

            await _usersCollection
                .UpdateOneAsync(u => u.Id == userId, update);
        }
    }
}