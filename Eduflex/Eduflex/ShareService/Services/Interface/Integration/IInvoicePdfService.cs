namespace ShareService.Services.Interface.Integration
{
    public interface IInvoicePdfService
    {
        // Renders the given HTML (the Invoice tab's rich-text editor content, verbatim)
        // to a PDF byte array via headless Chromium, so the generated PDF matches exactly
        // what staff saw in the editor.
        Task<byte[]> RenderToPdfAsync(string htmlContent);
    }
}
