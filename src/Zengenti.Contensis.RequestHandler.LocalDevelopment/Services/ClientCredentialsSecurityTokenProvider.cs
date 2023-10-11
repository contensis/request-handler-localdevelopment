using Newtonsoft.Json;
using Zengenti.Async;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Security;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

/// <summary>
/// For internal use only. Taken from Zengenti.Contensis.Security.Api.Console.Program
/// </summary>
internal class ClientCredentialsSecurityTokenProvider : ISecurityTokenProvider
{
    private readonly SecurityTokenParams _securityTokenParams;

    public ClientCredentialsSecurityTokenProvider(SecurityTokenParams securityTokenParams)
    {
        _securityTokenParams = securityTokenParams;
    }

    public SecurityTokenResult GetSecurityToken()
    {
        var httpClient = new HttpClient();

        httpClient.BaseAddress =
            new Uri($"https://cms-{_securityTokenParams.Alias}.cloud.contensis.com/authenticate/connect/token/");

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type",
            "application/x-www-form-urlencoded");

        FormUrlEncodedContent payload = new FormUrlEncodedContent
        (
            new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _securityTokenParams.ClientId),
                new KeyValuePair<string, string>("client_secret", _securityTokenParams.ClientSecret),
                new KeyValuePair<string, string>("scope",
                    "Security_Administrator ContentType_Delete ContentType_Read ContentType_Write Entry_Delete Entry_Read Entry_Write Project_Read Project_Write Project_Delete DiagnosticsAllUsers DiagnosticsAdministrator Workflow_Administrator"),
            }
        );

        try
        {
            var result = AsyncHelper.RunSync(() => GetSecurityToken(httpClient, payload));

            return new SecurityTokenResult
            {
                IsAdmin = true,
                IsValid = true,
                SecurityToken = result.access_token
            };
        }
        catch (Exception e)
        {
            return new SecurityTokenResult
            {
                ErrorMessage = e.Message,
                IsAdmin = false,
                IsValid = false,
                SecurityToken = string.Empty
            };
        }
    }

    private static async Task<dynamic> GetSecurityToken(HttpClient httpClient, FormUrlEncodedContent payload)
    {
        var response = await httpClient.PostAsync(string.Empty, payload);
        var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
        return result;
    }

    public void ClearToken()
    {
    }
}