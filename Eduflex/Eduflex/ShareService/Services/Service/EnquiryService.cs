using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enquiry;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class EnquiryService : IEnquiryService
    {
        private readonly IEnquiry _enquiryDataAccess;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IValidator<CreateEnquiryModel> _createEnquiryValidator;
        private readonly ILogger<EnquiryService> _logger;

        public EnquiryService(
            IEnquiry enquiryDataAccess,
            IRecaptchaService recaptchaService,
            IValidator<CreateEnquiryModel> createEnquiryValidator,
            ILogger<EnquiryService> logger)
        {
            _enquiryDataAccess = enquiryDataAccess;
            _recaptchaService = recaptchaService;
            _createEnquiryValidator = createEnquiryValidator;
            _logger = logger;
        }

        public async Task<EnquiryModel> CreateEnquiry(CreateEnquiryModel createDto)
        {
            var validate = await _createEnquiryValidator.ValidateAsync(createDto);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for enquiry creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var isHuman = await _recaptchaService.VerifyAsync(createDto.RecaptchaToken);
            if (!isHuman)
            {
                _logger.LogWarning("reCAPTCHA verification failed for enquiry from {Email}", createDto.Email);
                throw new ArgumentException("reCAPTCHA verification failed. Please try again.");
            }

            try
            {
                var enquiry = new EnquiryModel
                {
                    FirstName = createDto.FirstName,
                    MiddleName = createDto.MiddleName,
                    LastName = createDto.LastName,
                    Email = createDto.Email,
                    Mobile = createDto.Mobile,
                    Enquiry = createDto.Enquiry,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow
                };

                var createdEnquiry = await _enquiryDataAccess.CreateEnquiryAsync(enquiry);

                _logger.LogInformation("Created new enquiry with ID: {EnquiryId} from {Email}",
                    createdEnquiry.Id, createdEnquiry.Email);

                return createdEnquiry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateEnquiry for {Email}", createDto.Email);
                throw new Exception("Error creating enquiry", ex);
            }
        }
    }
}
