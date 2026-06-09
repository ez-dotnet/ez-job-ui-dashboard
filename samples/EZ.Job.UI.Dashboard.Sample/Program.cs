using EZ.Job.Core;
using EZ.Job.UI.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEZJob(o => o.WorkerCount = 2);
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FailingService>();

var app = builder.Build();

app.UseEZJobsDashboard();

app.MapGet("/enqueue", async (IJobDispatcher dispatcher) =>
{
    await dispatcher.EnqueueAsync<EmailService>(s => s.SendWelcomeAsync("user@example.com"));
    await dispatcher.EnqueueAsync<EmailService>(s => s.SendResetPasswordAsync("user@example.com"));
    return Results.Ok(new { enqueued = 2 });
});

app.MapGet("/enqueue-fail", async (IJobDispatcher dispatcher) =>
{
    await dispatcher.EnqueueAsync<FailingService>(s => s.ThrowExceptionAsync("teste@example.com"));
    return Results.Ok(new { enqueued = 1 });
});

app.MapGet("/seed", async (IJobStore store, IRecurringStore recurringStore) =>
{
    var now = DateTime.UtcNow;
    var jobs = new[]
    {
        new Job("seed-succeeded-1", "EmailService", "SendWelcomeAsync", ["System.String"], new object?[] {"alice@example.com"}, JobStatus.Succeeded, now.AddMinutes(-10), null, now.AddMinutes(-10).AddSeconds(1), now.AddMinutes(-10).AddSeconds(3)),
        new Job("seed-succeeded-2", "EmailService", "SendResetPasswordAsync", ["System.String"], new object?[] {"bob@example.com"}, JobStatus.Succeeded, now.AddMinutes(-9), null, now.AddMinutes(-9).AddSeconds(1), now.AddMinutes(-9).AddSeconds(2)),
        new Job("seed-failed-1", "FailingService", "ThrowExceptionAsync", ["System.String"], new object?[] {"fail@example.com"}, JobStatus.Failed, now.AddMinutes(-8), "System.InvalidOperationException: Falha simulada no processamento do email.\n   at FailingService.ThrowExceptionAsync(String email) in /samples/Program.cs:line 99\n   at JobWorker.ProcessJobAsync(Job job, CancellationToken ct) in /src/EZ.Job.Core/Infra/JobWorker.cs:line 42", now.AddMinutes(-8).AddSeconds(2), now.AddMinutes(-8).AddSeconds(5)),
        new Job("seed-failed-2", "EmailService", "SendWelcomeAsync", ["System.String"], new object?[] {"timeout@example.com"}, JobStatus.Failed, now.AddMinutes(-7), "System.TimeoutException: A opera\u00E7\u00E3o atingiu o tempo limite.\n   at EmailService.SendWelcomeAsync(String email) in /samples/Program.cs:line 60\n   at JobWorker.ProcessJobAsync(Job job, CancellationToken ct) in /src/EZ.Job.Core/Infra/JobWorker.cs:line 42", now.AddMinutes(-7).AddSeconds(1), now.AddMinutes(-7).AddSeconds(30)),
        new Job("seed-processing-1", "EmailService", "SendResetPasswordAsync", ["System.String"], new object?[] {"proc@example.com"}, JobStatus.Processing, now.AddMinutes(-3), null, now.AddMinutes(-3).AddSeconds(1), null),
        new Job("seed-enqueued-1", "FailingService", "ThrowExceptionAsync", ["System.String"], new object?[] {"pending@example.com"}, JobStatus.Enqueued, now.AddMinutes(-1), null, null, null),
    };

    foreach (var job in jobs) await store.AddAsync(job);

    var recurringDefs = new[]
    {
        new RecurringDefinition(
            Id: Guid.NewGuid(),
            TypeName: "EmailService",
            MethodName: "SendWelcomeAsync",
            ArgumentTypes: ["System.String"],
            Arguments: new object?[] { "recurring-5min@example.com" },
            CronExpression: "*/5 * * * *",
            IsActive: true,
            CreatedAtUtc: now,
            LastExecutionUtc: null),
        new RecurringDefinition(
            Id: Guid.NewGuid(),
            TypeName: "FailingService",
            MethodName: "ThrowExceptionAsync",
            ArgumentTypes: ["System.String"],
            Arguments: new object?[] { "recurring-hourly@example.com" },
            CronExpression: "0 * * * *",
            IsActive: false,
            CreatedAtUtc: now,
            LastExecutionUtc: null),
    };

    foreach (var def in recurringDefs) await recurringStore.AddOrUpdateAsync(def);

    return Results.Ok(new { seeded = jobs.Length, recurring = recurringDefs.Length });
});

app.MapGet("/", () => Results.Redirect("/ez-jobs"));

app.Run();

public class EmailService
{
    public async Task SendWelcomeAsync(string email)
    {
        await Task.Delay(100);
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Welcome email sent to {email}");
    }

    public async Task SendResetPasswordAsync(string email)
    {
        await Task.Delay(200);
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Password reset email sent to {email}");
    }
}

public class FailingService
{
    public async Task ThrowExceptionAsync(string email)
    {
        await Task.Delay(50);
        throw new InvalidOperationException($"Falha simulada no processamento do email: {email}");
    }
}