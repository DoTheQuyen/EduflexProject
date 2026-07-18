namespace ShareService.Services.Interface
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string token);
    }
}
