using System.Net.Http.Headers;
using System.Net.Http.Json;
using Holmes.App.Infrastructure.Identity.Models;
using Microsoft.Extensions.Options;

namespace Holmes.App.Infrastructure.Identity;

internal sealed class IdentityProvisioningClient(
    HttpClient httpClient,
    IOptions<IdentityProvisioningOptions> options
) : IIdentityProvisioningClient
{
    private readonly IdentityProvisioningOptions _options = options.Value;

    public async Task<ProvisionIdentityUserResponse> ProvisionUserAsync(
        ProvisionIdentityUserRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("IdentityProvisioning:BaseUrl must be configured.");
        }

        httpClient.BaseAddress ??= new Uri(_options.BaseUrl);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/provision/users");
        message.Content = JsonContent.Create(new
        {
            holmesUserId = request.HolmesUserId,
            email = request.Email,
            displayName = request.DisplayName,
            confirmationReturnUrl = _options.ConfirmationReturnUrl
        });

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProvisionIdentityUserResponse>(
            cancellationToken);

        if (payload is null)
        {
            throw new InvalidOperationException("Identity provisioning response was empty.");
        }

        return payload;
    }
}