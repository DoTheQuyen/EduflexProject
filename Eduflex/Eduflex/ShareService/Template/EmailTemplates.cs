namespace ShareService.Services.Service.Integration
{
    public static class EmailTemplates
    {
        public static (string Subject, string Html, string PlainText) NewUserWelcome(
            string firstName, string email, string temporaryPassword, string loginUrl)
        {
            var subject = "Welcome to Eduflex";

            var html = $@"
                <p>Dear {firstName},</p>
                <p>Welcome to Eduflex portal.</p>
                <p>Your account has been created. Please use the credentials below to log in, and you will be asked to set a new password on your first login.</p>
                <p>
                    <strong>Email:</strong> {email}<br />
                    <strong>Temporary password:</strong> {temporaryPassword}
                </p>
                <p><a href=""{loginUrl}"">Log in to Eduflex</a></p>";

            var plainText =
                $"Dear {firstName},\n\n" +
                "Welcome to Eduflex portal.\n" +
                "Your account has been created. Please use the credentials below to log in, and you will be asked to set a new password on your first login.\n\n" +
                $"Email: {email}\n" +
                $"Temporary password: {temporaryPassword}\n\n" +
                $"Log in here: {loginUrl}";

            return (subject, html, plainText);
        }
    }
}