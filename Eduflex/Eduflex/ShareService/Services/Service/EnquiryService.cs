using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess;
using ShareService.DataAccess.Interface;
using ShareService.Enums.Roles;
using ShareService.Models.Auth;
using ShareService.Models.Enquiry;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class EnquiryService : IEnquiryService
    {
        private readonly IEnquiry _enquiryDataAccess;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IValidator<EnquiryModel> _createEnquiryValidator;
        private readonly ILogger<EnquiryService> _logger;

        public EnquiryService(
            IEnquiry enquiryDataAccess,
            IRecaptchaService recaptchaService,
            IValidator<EnquiryModel> createEnquiryValidator,
            ILogger<EnquiryService> logger)
        {
            _enquiryDataAccess = enquiryDataAccess;
            _recaptchaService = recaptchaService;
            _createEnquiryValidator = createEnquiryValidator;
            _logger = logger;
        }

        public async Task<bool> CreateEnquiry(EnquiryModel enquiry)
        {
            var validate = await _createEnquiryValidator.ValidateAsync(enquiry);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for enquiry creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var isHuman = await _recaptchaService.VerifyAsync(enquiry.RecaptchaToken);
            if (!isHuman)
            {
                _logger.LogWarning("reCAPTCHA verification failed for enquiry from {Email}", enquiry.Email);
                throw new ArgumentException("reCAPTCHA verification failed. Please try again.");
            }

            var existingEnquiry = await _enquiryDataAccess.GetEnquiryAsync(enquiry.Email, enquiry.Mobile);
            if (existingEnquiry != null && existingEnquiry.Status == EnquiryEnums.New.ToString())
            {
                throw new Exception("Your enquiry is sent to our staff. We will contact you shortly.");
            }

            try
            {
                enquiry.Id = string.Empty;
                enquiry.Status = EnquiryEnums.New.ToString();
                enquiry.CreatedAt = DateTime.UtcNow;

                var created = await _enquiryDataAccess.CreateEnquiryAsync(enquiry);

                _logger.LogInformation("Created new enquiry with ID: {EnquiryId} from {Email}",
                    enquiry.Id, enquiry.Email);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateEnquiry for {Email}", enquiry.Email);
                throw new Exception("Error creating enquiry", ex);
            }
        }

        public async Task<PagedResult<EnquiryModel>> GetEnquiries(EnquiryFilter filter)
        {
            try
            {
                return await _enquiryDataAccess.GetEnquiriesAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiries");
                throw;
            }
        }

        public async Task<EnquiryModel?> GetEnquiryAsync(string id)
        {
            try
            {
                return await _enquiryDataAccess.GetEnquiryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiry by ID: {id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateEnquiriesAsync(string id, EnquiryModel updateModel)
        {
            try
            {
                var validationResult = await _createEnquiryValidator.ValidateAsync(updateModel);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existingEnquiry = await _enquiryDataAccess.GetEnquiryAsync(updateModel.Id);
                if (existingEnquiry == null)
                {
                    throw new ArgumentException("Enquiry not found");
                }


                existingEnquiry.ApplyEditableFields(updateModel);

                return await _enquiryDataAccess.UpdateEnquiriesAsync(existingEnquiry.Id, existingEnquiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Enquiry: {id}", updateModel.Id);
                throw;
            }
        }

        public async Task<bool> DeleteEnquiriesAsync(string id)
        {
            var deleted = await _enquiryDataAccess.DeleteEnquiriesAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted enquiry with ID: {EnquiryId}", id);
            }
            return deleted;
        }
    }
}
