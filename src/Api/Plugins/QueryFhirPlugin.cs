using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace API
{
    public class QueryFhirPlugin
    {

        [KernelFunction("query_fhir")]
        [Description("makes an HTTP call to a FHIR endpoint to get clinical data.")]
        [return: Description("JSON string of clinical data specific to the query sent")]
        public async Task<string> QueryFhir(string query)
        {
            //get FHIR server URL from settings
            var fhirServerUrl = Environment.GetEnvironmentVariable("FHIR_SERVER_URL");
            Console.WriteLine("FHIR Query: ");
            Console.WriteLine(fhirServerUrl + query);

            using (var client = new HttpClient())
            {
                //var content = new StringContent(json, Encoding.UTF8, "application/json");
               
               // client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                var response = await client.GetAsync(fhirServerUrl + query);
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
        }
    }
}
