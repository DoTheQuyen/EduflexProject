using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Task;

namespace ShareService.DataAccess
{
    public class TaskItem : AuditableCollectionBase<TaskItemModel>, ITaskItem
    {
        public TaskItem(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<TaskItemModel>("Tasks"), currentUser)
        {
        }

        public async Task<bool> CreateTaskAsync(TaskItemModel task)
        {
            await InsertOneAsync(task);
            return true;
        }

        public async Task<TaskItemModel?> GetTaskByIdAsync(string id)
        {
            return await Collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public Task<PagedResult<TaskItemModel>> SearchTasksAsync(TaskItemFilter filter, List<string>? restrictToInvolvedUserIds)
        {
            var filters = new List<FilterDefinition<TaskItemModel>>();

            if (restrictToInvolvedUserIds != null)
            {
                filters.Add(Builders<TaskItemModel>.Filter.Or(
                    Builders<TaskItemModel>.Filter.In(t => t.AssignerUserId, restrictToInvolvedUserIds),
                    Builders<TaskItemModel>.Filter.In(t => t.AssigneeUserId, restrictToInvolvedUserIds)));
            }

            if (filter.Status.HasValue)
            {
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.Status, filter.Status.Value.ToString()));
            }

            if (filter.ExcludeStatus.HasValue)
            {
                filters.Add(Builders<TaskItemModel>.Filter.Ne(t => t.Status, filter.ExcludeStatus.Value.ToString()));
            }

            if (!string.IsNullOrEmpty(filter.EnrolmentId))
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.EnrolmentId, filter.EnrolmentId));
            if (!string.IsNullOrEmpty(filter.EnquiryId))
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.EnquiryId, filter.EnquiryId));
            if (!string.IsNullOrEmpty(filter.ApplicationId))
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.ApplicationId, filter.ApplicationId));
            if (!string.IsNullOrEmpty(filter.FinancialRecordId))
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.FinancialRecordId, filter.FinancialRecordId));
            if (!string.IsNullOrEmpty(filter.MigrationCaseId))
                filters.Add(Builders<TaskItemModel>.Filter.Eq(t => t.MigrationCaseId, filter.MigrationCaseId));

            var searchFilter = BuildSearchFilter(filter.SearchTerm, t => t.Name);
            filters.Add(searchFilter);

            var combined = filters.Count > 0 ? Builders<TaskItemModel>.Filter.And(filters) : FilterDefinition<TaskItemModel>.Empty;
            var sort = Builders<TaskItemModel>.Sort.Ascending(t => t.DueDateTime);

            return GetPagedAsync(combined, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> ReplaceTaskAsync(string id, TaskItemModel task)
        {
            return await ReplaceOneAsync(t => t.Id == id, task);
        }
    }
}
