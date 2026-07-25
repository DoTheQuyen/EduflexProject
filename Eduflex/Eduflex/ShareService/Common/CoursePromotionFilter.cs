namespace ShareService.Common
{
    public class CoursePromotionFilter : PaginationQuery
    {
        public bool? IsFeatured { get; set; }
    }
}
