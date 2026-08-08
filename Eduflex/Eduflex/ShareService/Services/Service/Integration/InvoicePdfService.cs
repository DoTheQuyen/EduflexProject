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
            var page = await browser.NewPageAsync();
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
