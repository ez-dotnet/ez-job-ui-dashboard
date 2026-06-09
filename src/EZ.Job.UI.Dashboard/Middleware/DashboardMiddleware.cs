using Microsoft.AspNetCore.Http;

namespace EZ.Job.UI.Dashboard.Middleware;

internal sealed class DashboardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _basePath;
    private readonly string _htmlContent;

    public DashboardMiddleware(RequestDelegate next, string basePath)
    {
        _next     = next;
        _basePath = basePath.TrimEnd('/');
        _htmlContent = EmbeddedResourceHelper.GetDashboardHtml();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;

        // serve o HTML no path base e em /index
        if (path == _basePath || path == _basePath + "/index")
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(_htmlContent);
            return;
        }

        await _next(context);
    }
}
