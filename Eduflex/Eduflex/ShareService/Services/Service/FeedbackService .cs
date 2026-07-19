using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models.Feedback;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedback _feedbackDataAccess;
        private readonly IValidator<CreateFeedbackModel> _createFeedbackValidator;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            IFeedback feedbackDataAccess,
            IValidator<CreateFeedbackModel> createFeedbackValidator,
            ILogger<FeedbackService> logger)
        {
            _feedbackDataAccess = feedbackDataAccess;
            _createFeedbackValidator = createFeedbackValidator;
            _logger = logger;
        }

        public async Task<FeedbackModel> CreateFeedback(CreateFeedbackModel createDto)
        {
            var validate = await _createFeedbackValidator.ValidateAsync(createDto);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for feedback creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var feedback = new FeedbackModel
            {
                Name = createDto.Name,
                PhotoData = createDto.PhotoData,
                PhotoContentType = createDto.PhotoContentType,
                CourseName = createDto.CourseName,
                Comment = createDto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _feedbackDataAccess.CreateFeedbackAsync(feedback);
            _logger.LogInformation("Created new feedback with ID: {FeedbackId} for {Name}", created.Id, created.Name);
            return created;
        }

        public async Task<List<FeedbackModel>> GetLatestFeedback(int count)
        {
            return await _feedbackDataAccess.GetLatestFeedbackAsync(count);
        }

        public async Task<List<FeedbackModel>> GetAllFeedback()
        {
            return await _feedbackDataAccess.GetAllFeedbackAsync();
        }

        public async Task<bool> DeleteFeedback(string id)
        {
            var deleted = await _feedbackDataAccess.DeleteFeedbackAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted feedback with ID: {FeedbackId}", id);
            }
            return deleted;
        }
    }
}