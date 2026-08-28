using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using ShareService.Common;
using ShareService.DataAccess.Postgres.Common;
using ShareService.DataAccess.Postgres.Interface;
using ShareService.Models.Auth;
using System.Collections.Generic;

namespace ShareService.DataAccess.Postgres.Service
{
    public class UserPg : AuditableDbSetBase<UserModel>, IUserPg
    {
        private readonly EduflexPostgresContext _context;

        public UserPg(EduflexPostgresContext context, ICurrentUserService currentUser)
            : base(context, context.Users, currentUser)
        {
            _context = context;
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            return await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            return await EntitySet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return null;
            }

            existing.FirstName = updateDto.FirstName;
            existing.LastName = updateDto.LastName;
            existing.Email = updateDto.Email;
            existing.Mobile = updateDto.Mobile;

            await SaveWithStampAsync(existing);
            return existing;
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return false;
            }

            existing.PasswordHash = newPasswordHash;
            existing.MustChangePassword = false;

            return await SaveWithStampAsync(existing);
        }

        public async Task<bool> CreateUserAsync(UserModel user)
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = ObjectId.GenerateNewId().ToString();
            }

            await InsertAsync(user);
            return true;
        }

        public async Task<bool> UpdateUserAsync(string id, UserModel user)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return false;
            }

            return await ReplaceAsync(existing, user);
        }

        public Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter)
        {
            var users = EntitySet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = $"%{filter.SearchTerm}%";
                users = users.Where(u =>
                    EF.Functions.ILike(u.Email, term) ||
                    EF.Functions.ILike(u.FirstName, term) ||
                    EF.Functions.ILike(u.LastName, term));
            }

            if (!string.IsNullOrWhiteSpace(filter.RoleId))
            {
                users = users.Where(u => u.RoleId == filter.RoleId);
            }

            if (filter.RoleIds != null && filter.RoleIds.Count > 0)
            {
                users = users.Where(u => filter.RoleIds.Contains(u.RoleId));
            }

            if (filter.IsActive.HasValue)
            {
                users = users.Where(u => u.IsActive == filter.IsActive.Value);
            }

            users = users.OrderByDescending(u => u.CreatedAt);

            return GetPagedAsync(users, filter.PageNumber, filter.PageSize);
        }

        public async Task<UserModel?> GetUserByMobileAsync(string mobile)
        {
            return await EntitySet.FirstOrDefaultAsync(u => u.Mobile == mobile);
        }

        public async Task<List<UserModel>> GetUsersByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0) return new List<UserModel>();

            return await EntitySet.Where(u => idList.Contains(u.Id)).ToListAsync();
        }

        public async Task<List<string>> GetUserIdsByActiveStatusAsync(bool isActive)
        {
            return await EntitySet.Where(u => u.IsActive == isActive).Select(u => u.Id).ToListAsync();
        }

        public async Task<List<string>> GetUserIdsByRoleIdAsync(string roleId)
        {
            return await EntitySet.Where(u => u.RoleId == roleId && u.IsActive).Select(u => u.Id).ToListAsync();
        }

        public async Task<bool> SetActiveStatusAsync(string userId, bool isActive)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return false;
            }

            existing.IsActive = isActive;
            return await SaveWithStampAsync(existing);
        }

        public async Task<bool> UpdateContactInfoAsync(string userId, string email, string mobile)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return false;
            }

            existing.Email = email;
            existing.Mobile = mobile;
            return await SaveWithStampAsync(existing);
        }

        public async Task<bool> SetPasswordResetTokenAsync(string userId, string token, DateTime expiry)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return false;
            }

            existing.ResetToken = token;
            existing.ResetTokenExpiry = expiry;
            return await SaveWithStampAsync(existing);
        }

        public async Task<UserModel?> GetUserByResetTokenAsync(string token)
        {
            return await EntitySet.FirstOrDefaultAsync(u => u.ResetToken == token);
        }

        public async Task<bool> CompletePasswordResetAsync(string userId, string newPasswordHash)
        {
            var existing = await EntitySet.FirstOrDefaultAsync(u => u.Id == userId);
            if (existing == null)
            {
                return false;
            }

            existing.PasswordHash = newPasswordHash;
            existing.MustChangePassword = false;
            existing.ResetToken = null;
            existing.ResetTokenExpiry = null;
            return await SaveWithStampAsync(existing);
        }

        public async Task<Dictionary<string, int>> CountUsersByRoleIdsAsync(IEnumerable<string> roleIds)
        {
            var idList = roleIds.ToList();
            if (idList.Count == 0) return new Dictionary<string, int>();

            return await EntitySet
                .Where(u => idList.Contains(u.RoleId))
                .GroupBy(u => u.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);
        }

        public async Task<KeysetPageResult<UserModel>> GetUsersKeysetAsync(UserKeysetFilter filter)
        {
            var query = EntitySet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = $"%{filter.SearchTerm}%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.Email, term) ||
                    EF.Functions.ILike(u.FirstName, term) ||
                    EF.Functions.ILike(u.LastName, term));
            }

            if (!string.IsNullOrWhiteSpace(filter.RoleId))
            {
                query = query.Where(u => u.RoleId == filter.RoleId);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (filter.AfterCreatedAt.HasValue && filter.AfterId != null)
            {
                query = query.Where(u =>
                    u.CreatedAt < filter.AfterCreatedAt.Value ||
                    (u.CreatedAt == filter.AfterCreatedAt.Value && string.Compare(u.Id, filter.AfterId) < 0));
            }

            query = query
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id);

            var items = await query.Take(filter.PageSize + 1).ToListAsync();

            var hasMore = items.Count > filter.PageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            var last = items.LastOrDefault();

            return new KeysetPageResult<UserModel>
            {
                Items = items,
                HasMore = hasMore,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id
            };
        }
    }
}