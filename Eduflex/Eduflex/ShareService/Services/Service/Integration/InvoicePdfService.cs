using Microsoft.Playwright;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
   
    public class InvoicePdfService : IInvoicePdfService
    {
        public async Task<byte[]> RenderToPdfAsync(string htmlContent)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            // JS disabled: this HTML embeds free-text fields (e.g. rich-text form answers) that
            // are never server-side sanitized, so a malicious answer containing a <script> tag
            // must not be able to execute in this headless page.
            var page = await browser.NewPageAsync(new BrowserNewPageOptions { JavaScriptEnabled = false });
            await page.SetContentAsync(htmlContent, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });

            return await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
                Margin = new Margin { Top = "16mm", Bottom = "16mm", Left = "14mm", Right = "14mm" }
            });
        }
    }
}
