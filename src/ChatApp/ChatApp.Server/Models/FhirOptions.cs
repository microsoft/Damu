namespace ChatApp.Server.Models;

public class FhirOptions
{
    public string FHIRServerUrl { get; set; } = string.Empty;
    public string FHIRAuthTenantId { get; set; } = string.Empty;
    public string FHIRAuthClientId { get; set; } = string.Empty;
    public string FHIRAuthClientSecret { get; set; } = string.Empty;
    public string FHIRAuthResource { get; set; } = string.Empty;
}
