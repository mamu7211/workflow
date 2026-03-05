using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.ApiService.Data.Entities;
using Workflow.ApiService.Dtos;
using Workflow.ApiService.Endpoints;
using Workflow.Engine.Serialization;

namespace Workflow.Engine.Tests;

[TestClass]
public class WorkflowEndpointsTests
{
    private static WorkflowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WorkflowDbContext(options);
    }

    private static WorkflowDefinitionEntity CreateSampleEntity(string id = "def-1", string name = "Test Workflow")
    {
        var definition = new Models.WorkflowDefinition { Id = id, Name = name };
        return new WorkflowDefinitionEntity
        {
            Id = id,
            Name = name,
            Version = 1,
            DefinitionJson = WorkflowJsonConverter.Serialize(definition)
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsAllDefinitions()
    {
        using var db = CreateDbContext();
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-1", "Workflow A"));
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-2", "Workflow B"));
        await db.SaveChangesAsync();

        var result = await WorkflowEndpoints.GetAll(db);

        var okResult = result as Ok<IEnumerable<WorkflowDefinitionDto>>;
        Assert.IsNotNull(okResult);
        var items = okResult.Value!.ToList();
        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    public async Task GetById_ExistingId_ReturnsDefinition()
    {
        using var db = CreateDbContext();
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-1", "My Workflow"));
        await db.SaveChangesAsync();

        var result = await WorkflowEndpoints.GetById("def-1", db);

        var okResult = result as Ok<WorkflowDefinitionDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("def-1", okResult.Value!.Id);
        Assert.AreEqual("My Workflow", okResult.Value.Name);
    }

    [TestMethod]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await WorkflowEndpoints.GetById("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        using var db = CreateDbContext();
        var emptyDef = JsonDocument.Parse("{}").RootElement;
        var dto = new CreateWorkflowDto("New Workflow", "A description", emptyDef);

        var result = await WorkflowEndpoints.Create(dto, db);

        var createdResult = result as Created<WorkflowDefinitionDto>;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual("New Workflow", createdResult.Value!.Name);
        Assert.AreEqual("A description", createdResult.Value.Description);
        Assert.AreEqual(1, await db.WorkflowDefinitions.CountAsync());
    }

    [TestMethod]
    public async Task Update_ExistingId_UpdatesAndReturnsOk()
    {
        using var db = CreateDbContext();
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-1", "Old Name"));
        await db.SaveChangesAsync();

        var emptyDef = JsonDocument.Parse("{}").RootElement;
        var dto = new UpdateWorkflowDto("New Name", "Updated", emptyDef);

        var result = await WorkflowEndpoints.Update("def-1", dto, db);

        var okResult = result as Ok<WorkflowDefinitionDto>;
        Assert.IsNotNull(okResult);
        Assert.AreEqual("New Name", okResult.Value!.Name);
        Assert.AreEqual(2, okResult.Value.Version);
    }

    [TestMethod]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var emptyDef = JsonDocument.Parse("{}").RootElement;
        var dto = new UpdateWorkflowDto("Name", null, emptyDef);

        var result = await WorkflowEndpoints.Update("non-existent", dto, db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        using var db = CreateDbContext();
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-1"));
        await db.SaveChangesAsync();

        var result = await WorkflowEndpoints.Delete("def-1", db);

        Assert.IsInstanceOfType<NoContent>(result);
        Assert.AreEqual(0, await db.WorkflowDefinitions.CountAsync());
    }

    [TestMethod]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await WorkflowEndpoints.Delete("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }

    [TestMethod]
    public async Task Publish_ExistingId_SetsIsPublished()
    {
        using var db = CreateDbContext();
        db.WorkflowDefinitions.Add(CreateSampleEntity("def-1"));
        await db.SaveChangesAsync();

        var result = await WorkflowEndpoints.Publish("def-1", db);

        var okResult = result as Ok<WorkflowDefinitionDto>;
        Assert.IsNotNull(okResult);
        Assert.IsTrue(okResult.Value!.IsPublished);
    }

    [TestMethod]
    public async Task Publish_NonExistentId_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var result = await WorkflowEndpoints.Publish("non-existent", db);

        Assert.IsInstanceOfType<NotFound>(result);
    }
}
