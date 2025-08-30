using Microsoft.Playwright;

namespace wowup;

public sealed class CurseScraper : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IPage _page;

    public static async Task<CurseScraper> CreateAsync()
    {
        Console.WriteLine("Initializing...");
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

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

    public async Task<AddOnEntry?> GetLatestInfo(string name)
    {
        Console.WriteLine("Loading information for {0}...", name);
        var response = await _page.GotoAsync($"https://www.curseforge.com/wow/addons/{name}/files");
        if (response!.Status == 404) return null;

        var table = _page.Locator(".files-table").First;
        var row1 = table.Locator(".file-row-details").First;
        var version = await row1.Locator(".name").InnerTextAsync();
        var link = await row1.GetAttributeAsync("href");
        var downloadId = link!.Substring(link.LastIndexOf('/') + 1);

        return new AddOnEntry
        {
            Name = name,
            Version = version,
            VersionDownloadId = downloadId,
        };
    }

    public async Task<string> Download(AddOnEntry entry, string destination)
    {
        Console.WriteLine("Downloading binary for {0}...", entry.Name);
        var download = await _page.RunAndWaitForDownloadAsync(() => _page.GotoAsync(
            $"https://www.curseforge.com/wow/addons/{entry.Name}/download/{entry.VersionDownloadId}"));
        await download.SaveAsAsync(Path.Combine(destination, download.SuggestedFilename));
        return download.SuggestedFilename;
    }
}