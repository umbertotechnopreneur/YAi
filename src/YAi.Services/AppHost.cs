var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.YAi_Services_Core>("yai-services-core");

builder.Build().Run();
