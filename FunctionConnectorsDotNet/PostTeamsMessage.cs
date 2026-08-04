using System.Net;
using System.Text.Json;
using Azure.Connectors.Sdk.Teams;
using Azure.Connectors.Sdk.Teams.Models;
using Azure.Connectors.Sdk;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionConnectorsDotNet;

public class PostTeamsMessage
{
    private readonly ILogger<PostTeamsMessage> _logger;
    private readonly TeamsClient _teamsClient;

    public PostTeamsMessage(ILogger<PostTeamsMessage> logger, TeamsClient teamsClient)
    {
        _logger = logger;
        _teamsClient = teamsClient;
    }

    [Function("PostTeamsMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Read the request body
            string body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<MessageRequest>(body,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || string.IsNullOrEmpty(data.Message) || 
                string.IsNullOrEmpty(data.TeamId) || string.IsNullOrEmpty(data.ChannelId))
            {
                var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new 
                { 
                    error = "Fields 'message', 'teamId', and 'channelId' are required." 
                });
                return badRequest;
            }

            _logger.LogInformation("Posting message to Teams: {Message}", data.Message);

            // Create message request using the SDK's dynamic schema model
            var messageRequest = new DynamicPostMessageRequest();

            messageRequest.AdditionalProperties["recipient"] = JsonSerializer.SerializeToElement(
                new
                {
                    groupId = data.TeamId,
                    channelId = data.ChannelId
                });

            messageRequest.AdditionalProperties["messageBody"] = JsonSerializer.SerializeToElement(
                $"<p>{WebUtility.HtmlEncode(data.Message)}</p>");

            // Post the message using the Teams connector client
            var result = await _teamsClient.PostMessageToConversationAsync(
                postAs: "Flow bot",
                postIn: "Channel",
                input: messageRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully posted message to Teams");

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new 
            { 
                success = true,
                message = "Hello from Connectors sdk!",
                messageId = result?.MessageId,
                messageLink = result?.MessageLink
            });

            return response;
        }
        catch (ConnectorException ex)
        {
            _logger.LogError(ex, "Teams connector error: {StatusCode}", ex.Status);

            var errorResponse = request.CreateResponse(HttpStatusCode.BadGateway);
            await errorResponse.WriteAsJsonAsync(new 
            { 
                error = "Failed to post message to Teams",
                details = ex.Message,
                statusCode = ex.Status
            });

            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post message to Teams");

            var errorResponse = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new 
            { 
                error = "Failed to post message to Teams",
                details = ex.Message
            });

            return errorResponse;
        }
    }
}

public class MessageRequest
{
    public string? Message { get; set; }
    public string? TeamId { get; set; }
    public string? ChannelId { get; set; }
}



