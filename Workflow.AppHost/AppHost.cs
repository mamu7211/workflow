var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastruktur ---

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("workflowdb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var mailhog = builder.AddContainer("mailhog", "docker.io/mailhog/mailhog", "latest")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "mailhog-ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "mailhog-smtp");

// --- Services ---

var apiService = builder.AddProject<Projects.Workflow_ApiService>("apiservice")
    .WithHttpEndpoint(port: 5452, name: "api-http")
    .WithHttpsEndpoint(port: 7311, name: "api-https")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithEnvironment("Smtp__Host", mailhog.GetEndpoint("mailhog-smtp"))
    .WithHttpHealthCheck("/health")
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.Workflow_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
