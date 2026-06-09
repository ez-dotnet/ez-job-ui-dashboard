using System.Reflection;

namespace EZ.Job.UI.Dashboard.Middleware;

internal static class EmbeddedResourceHelper
{
    private static string? _cachedHtml;

    public static string GetDashboardHtml()
    {
        if (_cachedHtml is not null) return _cachedHtml;

        var assembly      = Assembly.GetExecutingAssembly();
        var resourceName  = "EZ.Job.UI.Dashboard.Resources.dashboard.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' não encontrado.");

        using var reader = new StreamReader(stream);
        _cachedHtml = reader.ReadToEnd();
        return _cachedHtml;
    }
}
