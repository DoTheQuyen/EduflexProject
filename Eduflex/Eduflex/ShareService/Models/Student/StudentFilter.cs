using ShareService.Common;

namespace ShareService.Models.Student
{
    public class StudentFilter : PaginationQuery
    {
        public bool? IsActive { get; set; }
    }
}
