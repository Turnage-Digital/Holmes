using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.Internal.Server.Controllers;

/// <summary>
/// Proxies SSE connections to the downstream API with proper authentication.
/// EventSource API cannot send custom headers, so the BFF's standard CSRF-protected
/// endpoints don't work for SSE. This controller handles SSE separately.
/// </summary>
[ApiController]
[Authorize]
[Route("api/orders/changes")]
public sealed class OrderChangesProxyController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OrderChangesProxyController> logger) : ControllerBase
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(30);

    [HttpGet]
    public async Task GetAsync(CancellationToken cancellationToken)
    {
        UserToken userToken;
        try
        {
            userToken = await HttpContext.GetUserAccessTokenAsync(new UserTokenRequestParameters(), cancellationToken).GetToken();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "No access token available for SSE proxy");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var apiBase = configuration["DownstreamApi:BaseUrl"] ?? "https://localhost:5001/api";
        var targetUrl = $"{apiBase}/orders/changes{Request.QueryString}";

        Response.Headers.CacheControl = "no-store";
        Response.Headers.ContentType = "text/event-stream";

        using var client = httpClientFactory.CreateClient();
        client.Timeout = RequestTimeout;

        var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        try
        {
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Downstream SSE endpoint returned {StatusCode}", response.StatusCode);
                Response.StatusCode = (int)response.StatusCode;
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is not null)
                {
                    await Response.WriteAsync(line + "\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is expected
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error proxying SSE connection");
        }
    }
}
