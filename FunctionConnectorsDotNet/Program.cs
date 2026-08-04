using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Azure.Connectors.Sdk.Teams;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register Teams Connector client
builder.Services.AddSingleton<TeamsClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionRuntimeUrl = configuration["ConnectorsTeamsConnectionRuntimeUrl"];

    if (string.IsNullOrEmpty(connectionRuntimeUrl))
    {
        throw new InvalidOperationException("ConnectorsTeamsConnectionRuntimeUrl is required");
    }

    var credential = new DefaultAzureCredential();
    return new TeamsClient(new Uri(connectionRuntimeUrl), credential);
});

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();
