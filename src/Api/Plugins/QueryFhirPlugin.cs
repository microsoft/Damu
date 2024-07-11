using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace API
{
    public class QueryFhirPlugin
    {
        private static string? _bearerToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private readonly HttpClient _client;

        public QueryFhirPlugin(HttpClient client)
        {
            _client = client;
        }

        [KernelFunction("query_fhir")]
        [Description("makes an HTTP call to a FHIR endpoint to get clinical data.")]
        [return: Description("JSON string of clinical data specific to the query sent")]
        public async Task<string> QueryFhir(string query)
        {

            try
            {
                //get FHIR server URL from settings
                var fhirServerUrl = Environment.GetEnvironmentVariable("FHIR_SERVER_URL");

                string bearerToken = await GetBearerToken();
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                var response = await _client.GetAsync(fhirServerUrl + query);
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Console.WriteLine("response.StatusCode: " + response.StatusCode);
                    return response.StatusCode.ToString();
                }
            }
            catch
            {

                throw;
            }

        }

        private async Task<string> GetBearerToken()
        {
            try
            {
                // Check if the token is already available and not expired
                if (_bearerToken != null && DateTime.UtcNow < _tokenExpiry)
                {
                    return _bearerToken;
                }

                var tenantId = Environment.GetEnvironmentVariable("TenantId");
                var clientId = Environment.GetEnvironmentVariable("ClientId");
                var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
                var resource = Environment.GetEnvironmentVariable("Resource");

                var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/token";


                var requestBody = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId!,
                    ["client_secret"] = clientSecret!,
                    ["resource"] = resource!
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await _client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonSerializer.Deserialize<Dictionary<string, string>>(responseString);
                    if (responseObject != null && responseObject.ContainsKey("access_token"))
                    {
                        _bearerToken = responseObject["access_token"];
                        int expiresIn = int.Parse(responseObject["expires_in"]);
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 300); // Subtract 5 minutes for safety
                        return _bearerToken;
                    }
                    else
                    {
                        throw new Exception("Failed to get bearer token");
                    }
                }
                else
                {
                    throw new Exception("Failed to get bearer token");
                }
            }
            catch
            {

                throw new Exception("Failed to get bearer token");
            }

        }

    }
}
