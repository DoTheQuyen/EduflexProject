using ShareService.Common;

namespace ShareService.Models.MigrationCase
{
    public class MigrationCaseFilter : PaginationQuery
    {
        public List<string>? Statuses { get; set; }
        public string? OwnerUserId { get; set; }
        public string? Category { get; set; }
    }
}
