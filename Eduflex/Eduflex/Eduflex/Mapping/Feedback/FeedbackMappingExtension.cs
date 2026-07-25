using Eduflex.DTOs.Feedback;
using ShareService.Models.Feedback;

namespace Eduflex.Mapping.Feedback
{
    public static class FeedbackMappingExtension
    {
        public static FeedbackModel ToModel(this CreateFeedbackDto dto)
        {
            return new FeedbackModel
            {
                Name = dto.Name,
                PhotoData = dto.PhotoData,
                PhotoContentType = dto.PhotoContentType,
                CourseName = dto.CourseName,
                Comment = dto.Comment
            };
        }

        public static FeedbackDto ToDto(this FeedbackModel model)
        {
            return new FeedbackDto
            {
                Id = model.Id,
                Name = model.Name,
                PhotoUrl = $"data:{model.PhotoContentType};base64,{model.PhotoData}",
                CourseName = model.CourseName,
                Comment = model.Comment,
                CreatedAt = model.CreatedAt
            };
        }
    }
}