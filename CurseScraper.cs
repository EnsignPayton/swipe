using Microsoft.Playwright;

namespace Swipe;

public sealed class CurseScraper : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IPage _page;

    public static async Task<CurseScraper> CreateAsync()
    {
        var browserOptions = new BrowserTypeLaunchOptions { Headless = true };

        var playwright = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await playwright.Firefox.LaunchAsync(browserOptions);
        }
        catch (PlaywrightException)
        {
            Microsoft.Playwright.Program.Main(["install", "firefox"]);
            browser = await playwright.Firefox.LaunchAsync(browserOptions);
        }

        var page = await browser.NewPageAsync();
        return new CurseScraper(playwright, browser, page);
    }

    private CurseScraper(IPlaywright playwright, IBrowser browser, IPage page)
    {
        _playwright = playwright;
        _browser = browser;
        _page = page;
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    public async Task<AddOnInfo?> GetLatestInfo(string name)
    {
        var response = await _page.GotoAsync($"https://www.curseforge.com/wow/addons/{name}/files/all",
            new() { Timeout = 10_000 });
        if (response!.Status == 404) return null;

        var table = _page.Locator(".files-table-card").First;
        var row1 = table.Locator(".file-row-summary").First;
        var version = await row1.Locator(".name").InnerTextAsync();
        var link = await row1.Locator(".file-download-button").GetAttributeAsync("href");
        var downloadId = int.Parse(link!.Substring(link.LastIndexOf('/') + 1));

        return new AddOnInfo(name, version, downloadId);
    }

    public async Task<string> Download(AddOnInfo value)
    {
        Exception? exception = null;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                return await Download(value, 30_000 * (int)Math.Pow(2, i));
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        throw exception ?? new Exception("Retry count exceeded");
    }

    private async Task<string> Download(AddOnInfo value, int timeout)
    {
        var download = await _page.RunAndWaitForDownloadAsync(() => _page.GotoAsync(
            $"https://www.curseforge.com/wow/addons/{value.Name}/download/{value.DownloadId}",
            new() { Timeout = timeout }));
        await download.SaveAsAsync(Path.Combine(Paths.Cache, download.SuggestedFilename));
        return download.SuggestedFilename;
    }
}

public record AddOnInfo(string Name, string Version, int DownloadId);
