namespace ShareService.Services.Interface.Integration
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string token);
    }
}
