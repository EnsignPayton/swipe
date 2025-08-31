using System.IO.Compression;
using System.Security.Cryptography;

namespace wowup;

public static class Utils
{
    public static async Task<string> HashFile(string filePath)
    {
        using var hashi = SHA256.Create();
        await using var fs = File.OpenRead(filePath);
        var hash = await hashi.ComputeHashAsync(fs);
        return Convert.ToHexStringLower(hash);
    }

    public static async Task<List<string>> ApplyZip(string zipPath, string destPath)
    {
        await using var fs = File.OpenRead(zipPath);
        using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);

        zipArchive.ExtractToDirectory(destPath, overwriteFiles: true);

        return zipArchive.Entries
            .Where(x => x.Name.Length == 0 && x.FullName.IndexOf('/') == x.FullName.Length - 1)
            .Select(x => x.FullName.TrimEnd('/'))
            .ToList();
    }
}