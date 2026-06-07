using Azure.Connectors.Sdk.OneDriveForBusiness.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;

namespace FunctionConnectorsDotNet;

public class OnNewOneDriveFile
{
    private readonly ILogger<Function1> _logger;

    public OnNewOneDriveFile(ILogger<Function1> logger)
    {
        _logger = logger;
    }


    [Function("OnNewOneDriveFile")]
    public string Run([ConnectorTrigger()] OneDriveForBusinessOnNewFilesTriggerPayload payload)
    {
        try
        {
            var fileContents = System.Text.Json.JsonSerializer.Serialize(payload);

            _logger.LogInformation(
            "File contents: {fileContents }",
            fileContents);

            _logger.LogInformation(
                    "Received file {Name} with size '{Size}'.",
                    payload?.Body?.Value?[0]?.Name, payload?.Body?.Value?[0]?.Size);

            return fileContents;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing the file: {ex.ToString()}", ex.ToString());
            return "err";
        }
    }
}