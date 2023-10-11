using Newtonsoft.Json;
using Zengenti.Async;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Security;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

internal class ContensisClassicSecurityTokenProvider : ISecurityTokenProvider
{
    private readonly SecurityTokenParams _securityTokenParams;

    public ContensisClassicSecurityTokenProvider(SecurityTokenParams securityTokenParams)
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
                new KeyValuePair<string, string>("grant_type", "contensis_classic"),
                new KeyValuePair<string, string>("username", _securityTokenParams.Username!),
                new KeyValuePair<string, string>("password", _securityTokenParams.Password!),
                new KeyValuePair<string, string>("scope",
                    "openid offline_access Security_Administrator ContentType_Delete ContentType_Read ContentType_Write Entry_Delete Entry_Read Entry_Write Project_Read Project_Write Project_Delete DiagnosticsAllUsers DiagnosticsAdministrator Workflow_Administrator"),
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
        return result!;
    }

    public void ClearToken()
    {
    }
}