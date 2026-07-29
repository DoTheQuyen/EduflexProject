using ShareService.Common;

namespace ShareService.Models.CoursePromotion
{
    public class CoursePromotionFilter : PaginationQuery
    {
        public bool? IsFeatured { get; set; }
    }
}
