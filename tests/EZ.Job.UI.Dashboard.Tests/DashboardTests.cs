using Xunit;
using EZ.Job.Core;
using EZ.Job.UI.Dashboard.Endpoints;

namespace EZ.Job.UI.Dashboard.Tests;

public sealed class DashboardTests
{
    [Fact]
    public void DashboardStats_record_should_have_required_fields()
    {
        var stats = new DashboardStats(1, 2, 3, 4);
        Assert.Equal(1, stats.Processing);
        Assert.Equal(2, stats.Enqueued);
        Assert.Equal(3, stats.Succeeded);
        Assert.Equal(4, stats.Failed);
    }

    [Fact]
    public void JobSummary_record_should_have_required_fields()
    {
        var createdAt = DateTime.UtcNow;
        var summary = new JobSummary("id-1", "MyJob", "enqueued", null, createdAt);
        Assert.Equal("id-1", summary.Id);
        Assert.Equal("MyJob", summary.JobType);
        Assert.Equal("enqueued", summary.Status);
        Assert.Null(summary.Error);
        Assert.Equal(createdAt, summary.CreatedAt);
    }

    [Fact]
    public void JobsResponse_record_should_have_required_fields()
    {
        var items = new[] { new JobSummary("id-1", "MyJob", "enqueued", null, DateTime.UtcNow) };
        var response = new JobsResponse(items, 1, 1, 20);
        Assert.Single(response.Items);
        Assert.Equal(1, response.Total);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
    }
}