using Azure.Connectors.Sdk.Teams.Models;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunctionConnectorsDotNet;

public class OnNewTeamsMessage
{
    private readonly ILogger<OnNewTeamsMessage> _logger;

    public OnNewTeamsMessage(ILogger<OnNewTeamsMessage> logger)
    {
        _logger = logger;
    }


    [Function("OnNewChannelMessage")]
    public string OnNewChannelMessage(
        [ConnectorTrigger] TeamsOnNewChannelMessageTriggerPayload payload)
    {
        var messages = payload?.Body?.Value ?? [];
        _logger.LogInformation("Received OnNewChannelMessage trigger ({Count} messages)", messages.Count);

        var serializedPayload = System.Text.Json.JsonSerializer.Serialize(payload);

        try
        {
            foreach (var message in messages)
            {
                if (message?.Body != null && message.Body.HasValue)
                {
                    var bodyElement = message.Body.Value;

                    // Extract the content
                    var content = bodyElement.TryGetProperty("content", out var contentProp)
                        ? contentProp.GetString() ?? string.Empty
                        : string.Empty;

                    if (!string.IsNullOrEmpty(content))
                    {
                        _logger.LogInformation("Teams message content: {Content}", content);
                    }
                    else
                    {
                        _logger.LogWarning("Could not extract content from message");
                    }
                }

                _logger.LogWarning("Could not extract content from message");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process teams message");
        }

        return serializedPayload;
    }
}
