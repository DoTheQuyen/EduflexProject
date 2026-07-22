namespace ShareService.Models.Setting
{
    public class RecaptchaSettings
    {
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
        public string SecretKey { get; set; } = string.Empty;
    }
}
