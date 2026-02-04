var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Hetzner_Container_Management_Api>("hetzner-container-management-api")
    .WithExternalHttpEndpoints();

builder.Build().Run();
