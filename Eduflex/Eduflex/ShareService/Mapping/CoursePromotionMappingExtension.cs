using ShareService.Models.CoursePromotion;

namespace ShareService.Mapping
{
    public static class CoursePromotionMappingExtension
    {
        public static void ApplyEditableFields(this CoursePromotionModel existing, CoursePromotionModel updateModel)
        {
            existing.CourseName = updateModel.CourseName;
            existing.UniversityName = updateModel.UniversityName;
            existing.Semester = updateModel.Semester;
            existing.ScholarshipLabel = updateModel.ScholarshipLabel;
            existing.Location = updateModel.Location;
            existing.Tuition = updateModel.Tuition;
            existing.Opportunities = updateModel.Opportunities;
            existing.ExpiryDate = updateModel.ExpiryDate;
            existing.Note = updateModel.Note;
            existing.WebsiteUrl = updateModel.WebsiteUrl;
            existing.IsFeatured = updateModel.IsFeatured;
            existing.DisplayOrder = updateModel.DisplayOrder;
        }
    }
}
