namespace EZ.Job.UI.Dashboard.Endpoints;

public record DashboardStats(
    int Processing,
    int Enqueued,
    int Succeeded,
    int Failed
);

public record JobSummary(
    string Id,
    string JobType,
    string Status,
    string? Error,
    DateTime CreatedAt
);

public record JobDetail(
    string Id,
    string JobType,
    string MethodName,
    string Status,
    string? Error,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    object?[] Arguments
);

public record JobsResponse(
    IEnumerable<JobSummary> Items,
    int Total,
    int Page,
    int PageSize
);

public record RecurringDefinitionSummary(
    Guid Id,
    string JobType,
    string CronExpression,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastExecutionUtc,
    int TotalExecutions,
    int SucceededExecutions,
    int FailedExecutions
);

public record RecurringDefinitionDetail(
    Guid Id,
    string TypeName,
    string MethodName,
    string[] ArgumentTypes,
    string CronExpression,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastExecutionUtc,
    int TotalExecutions,
    int SucceededExecutions,
    int FailedExecutions,
    DateTime? LastExecutionAt
);