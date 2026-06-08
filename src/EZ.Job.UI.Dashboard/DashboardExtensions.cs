using EZ.Job.UI.Dashboard.Endpoints;
using EZ.Job.UI.Dashboard.Middleware;
using Microsoft.AspNetCore.Builder;

namespace EZ.Job.UI.Dashboard;

public class DashboardOptions
{
    public string Route { get; set; } = "/ez-jobs";
    public bool DisableUI { get; set; } = false;
}

public static class DashboardExtensions
{
    public static WebApplication UseEZJobsDashboard(
        this WebApplication app,
        Action<DashboardOptions>? configure = null)
    {
        var options = new DashboardOptions();
        configure?.Invoke(options);

        var basePath = options.Route.TrimEnd('/');

        DashboardEndpoints.Map(app, basePath);

        if (!options.DisableUI)
            app.UseMiddleware<DashboardMiddleware>(basePath);

        return app;
    }
}