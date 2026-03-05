using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Workflow.ApiService.Data;
using Workflow.ApiService.Data.Entities;
using Workflow.ApiService.Dtos;
using Workflow.ApiService.Endpoints;
using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;

namespace Workflow.Engine.Tests;

[TestClass]
public class UserTaskEndpointsTests
{
    private static WorkflowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WorkflowDbContext(options);
    }

    private static (WorkflowExecutionEngine Engine, IWorkflowInstanceStore Store) CreateEngineAndStore()
    {
        var registry = new ActivityRegistry();
        var evaluator = new SimpleExpressionEvaluator();
        var sp = Substitute.For<IServiceProvider>();
        var engine = new WorkflowExecutionEngine(registry, evaluator, sp);
        var store = Substitute.For<IWorkflowInstanceStore>();
        return (engine, store);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOnlyPendingTasks()
    {
        using var db = CreateDbContext();
        db.UserTasks.Add(new UserTaskEntity
        {
            Id = "t1", WorkflowInstanceId = "i1", ActivityId = "a1",
            Title = "Pending Task", Status = "Pending"
        });
        db.UserTasks.Add(new UserTaskEntity
        {
            Id = "t2", WorkflowInstanceId = "i1", ActivityId = "a2",
            Title = "Approved Task", Status = "Approved"
        });
        await db.SaveChangesAsync();

        var result = await UserTaskEndpoints.GetAll(db);

        var okResult = result as Ok<IEnumerable<UserTaskDto>>;
        Assert.IsNotNull(okResult);
        var tasks = okResult.Value!.ToList();
        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual("Pending Task", tasks[0].Title);
    }

    [TestMethod]
    public async Task GetById_ExistingTask_ReturnsTask()
    {
        using var db = CreateDbContext();
        db.UserTasks.Add(new UserTaskEntity
        {
            Id = "t1", WorkflowInstanceId = "i1", ActivityId = "a1",
            Title = "Review Document", Description = "Please review", Status = "Pending"
        });
        await db.SaveChangesAsync();

        var result = await UserTaskEndpoints.GetById("t1", db);

        var okResult = result as Ok<UserTaskDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("Review Document", okResult.Value!.Title);
        Assert.AreEqual("Please review", okResult.Value.Description);
    }

    [TestMethod]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await UserTaskEndpoints.GetById("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Complete_PendingTask_UpdatesStatusAndData()
    {
        using var db = CreateDbContext();
        db.UserTasks.Add(new UserTaskEntity
        {
            Id = "t1", WorkflowInstanceId = "i1", ActivityId = "a1",
            Title = "Approve", Status = "Pending"
        });
        await db.SaveChangesAsync();

        var (engine, store) = CreateEngineAndStore();

        var dto = new CompleteTaskDto("Approved", new Dictionary<string, object?> { ["comment"] = "LGTM" });

        var result = await UserTaskEndpoints.Complete("t1", dto, db, engine, store);

        var okResult = result as Ok<UserTaskDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("Approved", okResult.Value!.Status);
        Assert.IsNotNull(okResult.Value.CompletedAt);

        var entity = await db.UserTasks.FirstAsync(e => e.Id == "t1");
        Assert.AreEqual("Approved", entity.Status);
        Assert.IsNotNull(entity.ResponseJson);
        Assert.IsNotNull(entity.CompletedAt);
    }

    [TestMethod]
    public async Task Complete_AlreadyCompleted_ReturnsBadRequest()
    {
        using var db = CreateDbContext();
        db.UserTasks.Add(new UserTaskEntity
        {
            Id = "t1", WorkflowInstanceId = "i1", ActivityId = "a1",
            Title = "Done", Status = "Approved"
        });
        await db.SaveChangesAsync();

        var (engine, store) = CreateEngineAndStore();

        var dto = new CompleteTaskDto("Rejected", null);

        var result = await UserTaskEndpoints.Complete("t1", dto, db, engine, store);

        Assert.IsInstanceOfType<BadRequest<string>>(result);
    }

    [TestMethod]
    public async Task Complete_NonExistent_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var (engine, store) = CreateEngineAndStore();

        var dto = new CompleteTaskDto("Approved", null);

        var result = await UserTaskEndpoints.Complete("non-existent", dto, db, engine, store);

        Assert.IsInstanceOfType<NotFound>(result);
    }
}
