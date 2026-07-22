using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;
using ShareService.Services.Interface;

namespace ShareService.DataAccess
{
    public class UserDB : IUserDB
    {
        private readonly IMongoCollection<UserModel> _usersCollection;

        public UserDB(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<UserModel>("Users");
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            return await _usersCollection
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            return await _usersCollection
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.FirstName, updateDto.FirstName)
                .Set(u => u.LastName, updateDto.LastName)
                .Set(u => u.Email, updateDto.Email);
                //.Set(u => u.UpdatedAt, DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<UserModel>
            {
                ReturnDocument = ReturnDocument.After
            };

            return await _usersCollection
                .FindOneAndUpdateAsync(u => u.Id == userId, update, options);
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.PasswordHash, newPasswordHash)
                .Set(u => u.MustChangePassword, false);

            var result = await _usersCollection
                .UpdateOneAsync(u => u.Id == userId, update);

            return result.ModifiedCount > 0;
        }

        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            return await _usersCollection
                .Find(FilterDefinition<UserModel>.Empty)
                .ToListAsync();
        }
    }
}