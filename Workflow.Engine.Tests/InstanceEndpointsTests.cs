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
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.Engine.Tests;

[TestClass]
public class InstanceEndpointsTests
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

    private static WorkflowInstanceEntity CreateInstanceEntity(
        string id, string defId, WorkflowStatus status)
    {
        var instance = new WorkflowInstance
        {
            Id = id,
            WorkflowDefinitionId = defId,
            WorkflowVersion = 1,
            Status = status
        };
        return new WorkflowInstanceEntity
        {
            Id = id,
            WorkflowDefinitionId = defId,
            WorkflowVersion = 1,
            Status = status.ToString(),
            StateJson = WorkflowJsonConverter.Serialize(instance)
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsAllInstances()
    {
        using var db = CreateDbContext();
        db.WorkflowInstances.Add(CreateInstanceEntity("i1", "d1", WorkflowStatus.Running));
        db.WorkflowInstances.Add(CreateInstanceEntity("i2", "d1", WorkflowStatus.Completed));
        await db.SaveChangesAsync();

        var result = await InstanceEndpoints.GetAll(db);

        var okResult = result as Ok<IEnumerable<WorkflowInstanceDto>>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(2, okResult.Value!.Count());
    }

    [TestMethod]
    public async Task GetById_ExistingId_ReturnsInstance()
    {
        using var db = CreateDbContext();
        db.WorkflowInstances.Add(CreateInstanceEntity("i1", "d1", WorkflowStatus.Running));
        await db.SaveChangesAsync();

        var result = await InstanceEndpoints.GetById("i1", db);

        var okResult = result as Ok<WorkflowInstanceDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("i1", okResult.Value!.Id);
        Assert.AreEqual("Running", okResult.Value.Status);
    }

    [TestMethod]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await InstanceEndpoints.GetById("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Cancel_ExistingRunningInstance_SetsCancelled()
    {
        using var db = CreateDbContext();
        db.WorkflowInstances.Add(CreateInstanceEntity("i1", "d1", WorkflowStatus.Running));
        await db.SaveChangesAsync();

        var result = await InstanceEndpoints.Cancel("i1", db);

        var okResult = result as Ok<WorkflowInstanceDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("Cancelled", okResult.Value!.Status);

        var entity = await db.WorkflowInstances.FirstAsync(e => e.Id == "i1");
        Assert.AreEqual("Cancelled", entity.Status);
        Assert.IsNotNull(entity.CompletedAt);
    }

    [TestMethod]
    public async Task Cancel_NonExistent_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await InstanceEndpoints.Cancel("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Resume_NotSuspended_ReturnsBadRequest()
    {
        using var db = CreateDbContext();
        db.WorkflowInstances.Add(CreateInstanceEntity("i1", "d1", WorkflowStatus.Running));
        await db.SaveChangesAsync();

        var (engine, store) = CreateEngineAndStore();

        var result = await InstanceEndpoints.Resume("i1", null, db, engine, store);

        Assert.IsInstanceOfType<BadRequest<string>>(result);
    }

    [TestMethod]
    public async Task Resume_NonExistent_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var (engine, store) = CreateEngineAndStore();

        var result = await InstanceEndpoints.Resume("non-existent", null, db, engine, store);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task GetLog_ReturnsLogEntries()
    {
        using var db = CreateDbContext();
        db.ActivityExecutionLogs.Add(new ActivityExecutionLogEntity
        {
            WorkflowInstanceId = "i1",
            ActivityId = "a1",
            ActivityType = "LogMessage",
            Status = "Completed",
            StartedAt = DateTime.UtcNow
        });
        db.ActivityExecutionLogs.Add(new ActivityExecutionLogEntity
        {
            WorkflowInstanceId = "i1",
            ActivityId = "a2",
            ActivityType = "SetVariable",
            Status = "Completed",
            StartedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await InstanceEndpoints.GetLog("i1", db);

        var okResult = result as Ok<IEnumerable<ActivityExecutionLogDto>>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(2, okResult.Value!.Count());
    }
}
