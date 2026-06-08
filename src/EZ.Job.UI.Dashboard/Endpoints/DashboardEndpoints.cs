using EZ.Job.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EZ.Job.UI.Dashboard.Endpoints;

internal static class DashboardEndpoints
{
    internal static void Map(IEndpointRouteBuilder app, string basePath)
    {
        var group = app.MapGroup(basePath + "/api");

        group.MapGet("/stats", async (IJobStore store, CancellationToken ct) =>
        {
            var all = await store.GetAllAsync(ct);
            var allList = all.ToList();

            return Results.Ok(new DashboardStats(
                Processing: allList.Count(j => j.Status == JobStatus.Processing),
                Enqueued:   allList.Count(j => j.Status == JobStatus.Enqueued),
                Succeeded:  allList.Count(j => j.Status == JobStatus.Succeeded),
                Failed:     allList.Count(j => j.Status == JobStatus.Failed)
            ));
        });

        group.MapGet("/jobs", async (
            IJobStore store,
            string? status,
            string? name,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var all = await store.GetAllAsync(ct);

            var filtered = (status?.ToLower()) switch
            {
                "processing" => all.Where(j => j.Status == JobStatus.Processing),
                "enqueued"   => all.Where(j => j.Status == JobStatus.Enqueued),
                "succeeded"  => all.Where(j => j.Status == JobStatus.Succeeded),
                "failed"     => all.Where(j => j.Status == JobStatus.Failed),
                _            => all
            };

            if (!string.IsNullOrWhiteSpace(name))
                filtered = filtered.Where(j => j.TypeName.Contains(name, StringComparison.OrdinalIgnoreCase));

            var list = filtered
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            var total = list.Count;
            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JobSummary(
                    j.Id,
                    ShortTypeName(j.TypeName),
                    j.Status.ToString().ToLower(),
                    j.Error,
                    j.CreatedAt
                ));

            return Results.Ok(new JobsResponse(items, total, page, pageSize));
        });

        group.MapGet("/jobs/{id}", async (string id, IJobStore store, CancellationToken ct) =>
        {
            var job = await store.GetAsync(id, ct);
            if (job is null) return Results.NotFound();

            return Results.Ok(new JobDetail(
                job.Id,
                ShortTypeName(job.TypeName),
                job.MethodName,
                job.Status.ToString().ToLower(),
                job.Error,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt,
                job.Arguments
            ));
        });

        group.MapPost("/jobs/{id}/retry", async (string id, IJobStore store, CancellationToken ct) =>
        {
            var job = await store.GetAsync(id, ct);
            if (job is null) return Results.NotFound();
            if (job.Status != JobStatus.Failed)
                return Results.BadRequest("Apenas jobs com status 'failed' podem ser reprocessados.");

            await store.UpdateStatusAsync(id, JobStatus.Enqueued, null, ct);
            return Results.Ok();
        });

        group.MapDelete("/jobs/{id}", async (string id, IJobStore store, CancellationToken ct) =>
        {
            var job = await store.GetAsync(id, ct);
            if (job is null) return Results.NotFound();
            if (job.Status == JobStatus.Processing)
                return Results.BadRequest("Não é possível remover um job em processamento.");

            await store.UpdateStatusAsync(id, JobStatus.Failed, "Cancelado manualmente via dashboard.", ct);
            return Results.Ok();
        });

        group.MapGet("/recurring", async (IRecurringStore recurringStore, IJobStore jobStore, CancellationToken ct) =>
        {
            var definitions = await recurringStore.GetAllAsync(ct).ConfigureAwait(false);
            var allJobs = await jobStore.GetAllAsync(ct).ConfigureAwait(false);
            var defsList = definitions.ToList();

            var result = defsList.Select(def =>
            {
                var jobs = allJobs.Where(j => j.RecurringJobId == def.Id.ToString()).ToList();
                return new RecurringDefinitionSummary(
                    def.Id,
                    ShortTypeName(def.TypeName),
                    def.CronExpression,
                    def.IsActive,
                    def.CreatedAtUtc,
                    def.LastExecutionUtc,
                    jobs.Count,
                    jobs.Count(j => j.Status == JobStatus.Succeeded),
                    jobs.Count(j => j.Status == JobStatus.Failed)
                );
            });

            return Results.Ok(result);
        });

        group.MapGet("/recurring/{id:guid}", async (Guid id, IRecurringStore recurringStore, IJobStore jobStore, CancellationToken ct) =>
        {
            var def = await recurringStore.GetAsync(id, ct).ConfigureAwait(false);
            if (def is null) return Results.NotFound();

            var jobs = (await jobStore.GetAllAsync(ct).ConfigureAwait(false))
                .Where(j => j.RecurringJobId == id.ToString()).ToList();

            return Results.Ok(new RecurringDefinitionDetail(
                def.Id,
                def.TypeName,
                def.MethodName,
                def.ArgumentTypes,
                def.CronExpression,
                def.IsActive,
                def.CreatedAtUtc,
                def.LastExecutionUtc,
                jobs.Count,
                jobs.Count(j => j.Status == JobStatus.Succeeded),
                jobs.Count(j => j.Status == JobStatus.Failed),
                jobs.MaxBy(j => j.CreatedAt)?.CreatedAt
            ));
        });

        group.MapGet("/recurring/{id:guid}/jobs", async (
            Guid id,
            IRecurringStore recurringStore,
            IJobStore jobStore,
            string? status,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var def = await recurringStore.GetAsync(id, ct).ConfigureAwait(false);
            if (def is null) return Results.NotFound();

            var all = (await jobStore.GetAllAsync(ct).ConfigureAwait(false))
                .Where(j => j.RecurringJobId == id.ToString());

            var filtered = (status?.ToLower()) switch
            {
                "processing" => all.Where(j => j.Status == JobStatus.Processing),
                "enqueued"   => all.Where(j => j.Status == JobStatus.Enqueued),
                "succeeded"  => all.Where(j => j.Status == JobStatus.Succeeded),
                "failed"     => all.Where(j => j.Status == JobStatus.Failed),
                _            => all
            };

            var list = filtered.OrderByDescending(j => j.CreatedAt).ToList();
            var total = list.Count;
            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JobSummary(
                    j.Id,
                    ShortTypeName(j.TypeName),
                    j.Status.ToString().ToLower(),
                    j.Error,
                    j.CreatedAt
                ));

            return Results.Ok(new JobsResponse(items, total, page, pageSize));
        });

        group.MapPost("/recurring/{id:guid}/pause", async (Guid id, IRecurringStore store, CancellationToken ct) =>
        {
            var def = await store.GetAsync(id, ct).ConfigureAwait(false);
            if (def is null) return Results.NotFound();
            if (!def.IsActive) return Results.BadRequest("Definição já está pausada.");

            await store.SetActiveAsync(id, false, ct).ConfigureAwait(false);
            return Results.Ok();
        });

        group.MapPost("/recurring/{id:guid}/activate", async (Guid id, IRecurringStore store, CancellationToken ct) =>
        {
            var def = await store.GetAsync(id, ct).ConfigureAwait(false);
            if (def is null) return Results.NotFound();
            if (def.IsActive) return Results.BadRequest("Definição já está ativa.");

            await store.SetActiveAsync(id, true, ct).ConfigureAwait(false);
            return Results.Ok();
        });

        group.MapDelete("/recurring/{id:guid}", async (Guid id, IRecurringStore store, CancellationToken ct) =>
        {
            var def = await store.GetAsync(id, ct).ConfigureAwait(false);
            if (def is null) return Results.NotFound();

            await store.RemoveAsync(id, ct).ConfigureAwait(false);
            return Results.Ok();
        });
    }

    private static string ShortTypeName(string assemblyQualifiedName)
    {
        var typeName = assemblyQualifiedName.Split(',')[0].Trim();
        return typeName.Split('.').Last();
    }
}