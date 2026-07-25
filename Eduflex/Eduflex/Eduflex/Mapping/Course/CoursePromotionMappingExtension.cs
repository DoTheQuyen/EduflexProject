using Eduflex.DTOs.Course;
using ShareService.Models.CoursePromotion;

namespace Eduflex.Mapping.Course
{
    public static class CoursePromotionMappingExtension
    {
        public static CoursePromotionModel ToModel(this CreateCoursePromotionDto dto)
        {
            return new CoursePromotionModel
            {
                CourseName = dto.CourseName,
                UniversityName = dto.UniversityName,
                Semester = dto.Semester,
                ScholarshipLabel = dto.ScholarshipLabel,
                Location = dto.Location,
                Tuition = dto.Tuition,
                Opportunities = dto.Opportunities,
                ExpiryDate = dto.ExpiryDate,
                Note = dto.Note,
                WebsiteUrl = dto.WebsiteUrl,
                IsFeatured = dto.IsFeatured,
                DisplayOrder = dto.DisplayOrder
            };
        }

        public static CoursePromotionDto ToDto(this CoursePromotionModel model)
        {
            return new CoursePromotionDto
            {
                Id = model.Id,
                CourseName = model.CourseName,
                UniversityName = model.UniversityName,
                Semester = model.Semester,
                ScholarshipLabel = model.ScholarshipLabel,
                Location = model.Location,
                Tuition = model.Tuition,
                Opportunities = model.Opportunities,
                ExpiryDate = model.ExpiryDate,
                Note = model.Note,
                WebsiteUrl = model.WebsiteUrl,
                IsFeatured = model.IsFeatured,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
