using ChatApp.Server.Models;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace ChatApp.Server.Plugins;

public class QueryFhirPlugin
{
    private static string? _bearerToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private readonly HttpClient _client;
    private readonly FhirOptions _fhirOptions;

    public QueryFhirPlugin(HttpClient client, FhirOptions fhirOptions)
    {
        _client = client;
        _fhirOptions = fhirOptions;
    }

    [KernelFunction("query_fhir")]
    [Description("makes an HTTP call to a FHIR endpoint to get clinical data.")]
    [return: Description("JSON string of clinical data specific to the query sent")]
    public async Task<string> QueryFhir(string query)
    {
        var bearerToken = await GetBearerToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _client.GetAsync(_fhirOptions.FHIRServerUrl + query);

        if (response.IsSuccessStatusCode)
            return response.Content.ReadAsStringAsync().Result;
        else
            return response.StatusCode.ToString();
    }

    private async Task<string> GetBearerToken()
    {
        // Check if the token is already available and not expired
        if (_bearerToken != null && DateTime.UtcNow < _tokenExpiry)
            return _bearerToken;

        var url = $"https://login.microsoftonline.com/{_fhirOptions.FHIRAuthTenantId}/oauth2/token";

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _fhirOptions.FHIRAuthClientId,
            ["client_secret"] = _fhirOptions.FHIRAuthClientSecret,
            ["resource"] = _fhirOptions.FHIRAuthResource
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = await _client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to get bearer token");

        var str = await response.Content.ReadAsStringAsync() ?? string.Empty;

        var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

        if (!responseObject?.TryGetValue("access_token", out _bearerToken) ?? false)
            throw new Exception("Failed to get bearer token");

        _bearerToken = responseObject["access_token"];

        int expiresIn = int.Parse(responseObject["expires_in"]);

        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 300); // Subtract 5 minutes for safety

        return _bearerToken;
    }
}
