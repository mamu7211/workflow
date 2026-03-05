using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.Engine.Models;

namespace Workflow.Engine.Tests;

[TestClass]
public class EfWorkflowInstanceStoreTests
{
    private static WorkflowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WorkflowDbContext(options);
    }

    [TestMethod]
    public async Task SaveAsync_NewInstance_InsertsEntity()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        var instance = new WorkflowInstance
        {
            Id = "inst-1",
            WorkflowDefinitionId = "def-1",
            WorkflowVersion = 1,
            Status = WorkflowStatus.Running
        };

        await store.SaveAsync(instance);

        var entity = await db.WorkflowInstances.FirstOrDefaultAsync(e => e.Id == "inst-1");
        Assert.IsNotNull(entity);
        Assert.AreEqual("Running", entity.Status);
        Assert.AreEqual("def-1", entity.WorkflowDefinitionId);
    }

    [TestMethod]
    public async Task SaveAsync_ExistingInstance_UpdatesEntity()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        var instance = new WorkflowInstance
        {
            Id = "inst-2",
            WorkflowDefinitionId = "def-1",
            WorkflowVersion = 1,
            Status = WorkflowStatus.Running
        };

        await store.SaveAsync(instance);

        instance.Status = WorkflowStatus.Completed;
        instance.CompletedAt = DateTime.UtcNow;

        await store.SaveAsync(instance);

        var entities = await db.WorkflowInstances.Where(e => e.Id == "inst-2").ToListAsync();
        Assert.AreEqual(1, entities.Count);
        Assert.AreEqual("Completed", entities[0].Status);
        Assert.IsNotNull(entities[0].CompletedAt);
    }

    [TestMethod]
    public async Task GetAsync_ExistingInstance_ReturnsInstance()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        var instance = new WorkflowInstance
        {
            Id = "inst-3",
            WorkflowDefinitionId = "def-1",
            WorkflowVersion = 1,
            Status = WorkflowStatus.Suspended,
            Variables = new Dictionary<string, object?> { ["key"] = "value" }
        };

        await store.SaveAsync(instance);

        var loaded = await store.GetAsync("inst-3");

        Assert.IsNotNull(loaded);
        Assert.AreEqual("inst-3", loaded.Id);
        Assert.AreEqual(WorkflowStatus.Suspended, loaded.Status);
        Assert.AreEqual("def-1", loaded.WorkflowDefinitionId);
    }

    [TestMethod]
    public async Task GetAsync_NonExistentInstance_ReturnsNull()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        var loaded = await store.GetAsync("non-existent");

        Assert.IsNull(loaded);
    }

    [TestMethod]
    public async Task GetByStatusAsync_ReturnsMatchingInstances()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        await store.SaveAsync(new WorkflowInstance
        {
            Id = "inst-a", WorkflowDefinitionId = "def-1", WorkflowVersion = 1,
            Status = WorkflowStatus.Suspended
        });
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "inst-b", WorkflowDefinitionId = "def-1", WorkflowVersion = 1,
            Status = WorkflowStatus.Running
        });
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "inst-c", WorkflowDefinitionId = "def-1", WorkflowVersion = 1,
            Status = WorkflowStatus.Suspended
        });

        var suspended = await store.GetByStatusAsync(WorkflowStatus.Suspended);

        Assert.AreEqual(2, suspended.Count);
        Assert.IsTrue(suspended.All(i => i.Status == WorkflowStatus.Suspended));
    }

    [TestMethod]
    public async Task GetByStatusAsync_NoMatches_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var store = new EfWorkflowInstanceStore(db);

        var result = await store.GetByStatusAsync(WorkflowStatus.Faulted);

        Assert.AreEqual(0, result.Count);
    }
}
