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

        public static (string Subject, string Html, string PlainText) PasswordReset(
            string firstName, string resetLink)
        {
            var subject = "Reset your Eduflex password";

            var html = $@"
                <p>Dear {firstName},</p>
                <p>We received a request to reset your Eduflex password. Click the link below to choose a new one — this link expires in 1 hour.</p>
                <p><a href=""{resetLink}"">Reset your password</a></p>
                <p>If you didn't request this, you can safely ignore this email.</p>";

            var plainText =
                $"Dear {firstName},\n\n" +
                "We received a request to reset your Eduflex password. Use the link below to choose a new one — it expires in 1 hour.\n\n" +
                $"{resetLink}\n\n" +
                "If you didn't request this, you can safely ignore this email.";

            return (subject, html, plainText);
        }
    }
}