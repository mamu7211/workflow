using Workflow.ApiService.Data;
using Workflow.ApiService.Endpoints;
using Workflow.ApiService.Services;
using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// CORS - allow cross-origin requests from Blazor WASM frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization");
    });
});

// OpenAPI
builder.Services.AddOpenApi();

// Engine
builder.Services.AddSingleton<ActivityRegistry>(sp =>
{
    var registry = new ActivityRegistry();
    registry.AddBuiltInActivities();
    return registry;
});
builder.Services.AddScoped<IExpressionEvaluator, SimpleExpressionEvaluator>();
builder.Services.AddScoped<WorkflowExecutionEngine>();
builder.Services.AddScoped<IWorkflowInstanceStore, EfWorkflowInstanceStore>();

// HTTP client factory for HttpRequestActivity
builder.Services.AddHttpClient();

// Database
builder.AddNpgsqlDbContext<WorkflowDbContext>("workflowdb");

// RabbitMQ
builder.AddRabbitMQClient("messaging");

// Background Services
builder.Services.AddHostedService<DelayResumeService>();
builder.Services.AddHostedService<RabbitMqListenerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Map endpoints
app.MapWorkflowEndpoints();
app.MapInstanceEndpoints();
app.MapUserTaskEndpoints();
app.MapWebhookEndpoints();
app.MapActivityMetadataEndpoints();

app.MapGet("/", () => "Workflow API Service is running.");

app.MapDefaultEndpoints();

app.Run();
