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
                .Set(u => u.Email, updateDto.Email)
                .Set(u => u.Mobile, updateDto.Mobile);

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

            if (filter.RoleIds != null && filter.RoleIds.Count > 0)
            {
                mongoFilters.Add(Builders<UserModel>.Filter.In(u => u.RoleId, filter.RoleIds));
            }

            if (filter.IsActive.HasValue)
            {
                mongoFilters.Add(Builders<UserModel>.Filter.Eq(u => u.IsActive, filter.IsActive.Value));
            }

            var mongoFilter = Builders<UserModel>.Filter.And(mongoFilters);
            var sort = Builders<UserModel>.Sort.Descending(u => u.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<UserModel?> GetUserByMobileAsync(string mobile)
        {
            return await Collection
                .Find(u => u.Mobile == mobile)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserModel>> GetUsersByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0) return new List<UserModel>();

            return await Collection
                .Find(Builders<UserModel>.Filter.In(u => u.Id, idList))
                .ToListAsync();
        }

        public async Task<List<string>> GetUserIdsByActiveStatusAsync(bool isActive)
        {
            return await Collection
                .Find(u => u.IsActive == isActive)
                .Project(u => u.Id)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserIdsByRoleIdAsync(string roleId)
        {
            return await Collection
                .Find(u => u.RoleId == roleId && u.IsActive)
                .Project(u => u.Id)
                .ToListAsync();
        }

        public async Task<bool> SetActiveStatusAsync(string userId, bool isActive)
        {
            var update = Builders<UserModel>.Update.Set(u => u.IsActive, isActive);
            var result = await UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateContactInfoAsync(string userId, string email, string mobile)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.Email, email)
                .Set(u => u.Mobile, mobile);

            var result = await UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> SetPasswordResetTokenAsync(string userId, string token, DateTime expiry)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.ResetToken, token)
                .Set(u => u.ResetTokenExpiry, expiry);

            var result = await UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<UserModel?> GetUserByResetTokenAsync(string token)
        {
            return await Collection
                .Find(u => u.ResetToken == token)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CompletePasswordResetAsync(string userId, string newPasswordHash)
        {
            var update = Builders<UserModel>.Update
                .Set(u => u.PasswordHash, newPasswordHash)
                .Set(u => u.MustChangePassword, false)
                .Set(u => u.ResetToken, null)
                .Set(u => u.ResetTokenExpiry, null);

            var result = await UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<Dictionary<string, int>> CountUsersByRoleIdsAsync(IEnumerable<string> roleIds)
        {
            var idList = roleIds.ToList();
            if (idList.Count == 0) return new Dictionary<string, int>();

            var roleIdsOfUsers = await Collection
                .Find(Builders<UserModel>.Filter.In(u => u.RoleId, idList))
                .Project(u => u.RoleId)
                .ToListAsync();

            return roleIdsOfUsers
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
