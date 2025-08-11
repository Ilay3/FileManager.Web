namespace FileManager.Domain.Common;

public static class PathHelper
{
    public static string NormalizeYandexPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.Contains("//"))
            normalized = normalized.Replace("//", "/");
        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;
        return normalized.ToLowerInvariant();
    }
}
