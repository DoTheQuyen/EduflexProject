namespace ShareService.Models.Enquiry
{
    public class CreateEnquiryModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Enquiry { get; set; } = string.Empty;

        /// <summary>
        /// Token returned by the Google reCAPTCHA v2 widget on the client, verified server-side before saving.
        /// </summary>
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}
