using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;

namespace ShareService.DataAccess
{
    public class UserDB : AuditableCollectionBase<UserModel>, IUserDB
    {
        public UserDB(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<UserModel>("Users"), currentUser)
        {
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            return await Collection
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            return await Collection
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.FirstName, updateDto.FirstName)
                .Set(u => u.LastName, updateDto.LastName)
                .Set(u => u.Email, updateDto.Email);

            var options = new FindOneAndUpdateOptions<UserModel>
            {
                ReturnDocument = ReturnDocument.After
            };

            return await FindOneAndUpdateAsync(u => u.Id == userId, update, options);
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.PasswordHash, newPasswordHash)
                .Set(u => u.MustChangePassword, false);

            var result = await UpdateOneAsync(u => u.Id == userId, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> CreateUserAsync(UserModel user)
        {
            await InsertOneAsync(user);
            return true;
        }

        public async Task<bool> UpdateUserAsync(string id, UserModel user)
        {
            return await ReplaceOneAsync(p => p.Id == id, user);
        }

        public Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter)
        {
            var mongoFilters = new List<FilterDefinition<UserModel>>
            {
                BuildSearchFilter(filter.SearchTerm, u => u.Email, u => u.FirstName, u => u.LastName)
            };

            if (!string.IsNullOrWhiteSpace(filter.RoleId))
            {
                mongoFilters.Add(Builders<UserModel>.Filter.Eq(u => u.RoleId, filter.RoleId));
            }

            if (filter.IsActive.HasValue)
            {
                mongoFilters.Add(Builders<UserModel>.Filter.Eq(u => u.IsActive, filter.IsActive.Value));
            }

            var mongoFilter = Builders<UserModel>.Filter.And(mongoFilters);
            var sort = Builders<UserModel>.Sort.Descending(u => u.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }
    }
}
