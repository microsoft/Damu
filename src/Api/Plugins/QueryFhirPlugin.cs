//add library to read from settings file
using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

public class QueryFhirPlugin {

    [KernelFunction("query_fhir")]
    [Description("makes an HTTP call to a FHIR endpoint to get clinical data.")]
    [return: Description("JSON string of clinical data specific to the query sent")]
    public async Task<string> QueryFhir(string query)
    {
        //get FHIR server URL from settings
        var fhirServerUrl = Environment.GetEnvironmentVariable("FHIR_SERVER_URL");
        using (var client = new HttpClient())
        {
            //var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(fhirServerUrl + query, null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);
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

