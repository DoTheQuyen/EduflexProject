namespace ShareService.Models.Course
{
    public class CourseSearchResult
    {
        public CourseModel Course { get; set; } = null!;
        public ShareService.Models.EducationPartner.EducationPartnerModel Partner { get; set; } = null!;
    }
}
