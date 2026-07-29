using Microsoft.Playwright;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
    // Wraps Microsoft Playwright's headless Chromium to convert HTML to PDF. Chosen over
    // a code-first layout library (e.g. QuestPDF) because the Invoice tab's rich-text
    // editor content needs to be rendered exactly as staff see it, not rebuilt as a
    // fluent-API document.
    //
    // Local dev one-time setup: run `pwsh bin/Debug/net8.0/playwright.ps1 install chromium`
    // (or the equivalent `playwright install chromium` .sh on non-Windows) from the
    // ShareService build output folder after the first `dotnet build` — Playwright's
    // browser binaries are not restored by NuGet/dotnet build itself.
    //
    // Deployment note: whatever environment ultimately runs the API needs those same
    // Chromium binaries + native dependencies present (straightforward in a Docker/
    // container-based deployment via `RUN playwright install --with-deps chromium`,
    // meaningfully harder on a plain Azure App Service without a custom container) — a
    // deployment-environment decision to make before shipping the Invoice tab's
    // "Generate PDF" button to production, not something this class can work around.
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
