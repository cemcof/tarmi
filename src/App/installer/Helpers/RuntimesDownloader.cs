using Spectre.Console;

namespace Tarmi.Installer.Helpers;

internal static class RuntimesDownloader
{
    private const string DesktopRuntimeUrl = """https://aka.ms/dotnet/{0}.0/windowsdesktop-runtime-win-x64.exe""";
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromMinutes(15) };

    public static async Task<string> DownloadDesktopRuntime(string outDir, short majorVersion = 8)
    {
        string url = string.Format(DesktopRuntimeUrl, majorVersion);
        return await DownloadFile(url, outDir);
    }

    private static async Task<string> DownloadFile(string downloadUrl, string outDir)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]Downloading:[/] [yellow]{downloadUrl}[/]");
        using HttpResponseMessage response = await GetHttpResponse(downloadUrl);
        string savePath = await SaveFile(response.Content, outDir, downloadUrl.Split('/')[^1]);
        AnsiConsole.MarkupLine("[green]Download complete.[/]");
        return savePath;
    }

    private static async Task<string> SaveFile(HttpContent content, string dir, string fileName)
    {
        _ = Directory.CreateDirectory(dir);
        AnsiConsole.WriteLine($"Saving content to {dir}");
        string path = Path.Combine(dir, fileName);
        using var contentStream = await content.ReadAsStreamAsync();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await contentStream.CopyToAsync(fileStream);
        AnsiConsole.MarkupLine("[green]Save complete.[/]");
        return path;
    }

    private static async Task<HttpResponseMessage> GetHttpResponse(string url)
    {
        var response = await client.GetAsync(url);
        return response.EnsureSuccessStatusCode();
    }
}
