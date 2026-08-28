using ShareService.Common;
using ShareService.Enums.Student;

namespace ShareService.Models.Student
{
    public class StudentFilter : PaginationQuery
    {
        public bool? IsActive { get; set; }
        public PersonType? Type { get; set; }
    }
}
