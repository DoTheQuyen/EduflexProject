using Eduflex.DTOs.Feedback;
using ShareService.Common;
using ShareService.Models.Feedback;

namespace Eduflex.Mapping.Feedback
{
    public static class FeedbackMappingExtension
    {
        public static PaginationQuery ToFilter(this FeedbackFilterDto dto)
        {
            return new PaginationQuery
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

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