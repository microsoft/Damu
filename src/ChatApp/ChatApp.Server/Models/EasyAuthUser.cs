using System.Text.Json.Serialization;

namespace ChatApp.Server.Models
{
    // todo: see what we can remove from this and maintain feature parity in the UI
    public class EasyAuthUser
    {
        [JsonPropertyName("user_principal_id")]
        public string UserPrincipalId { get; set; } = "";
        [JsonPropertyName("user_name")]
        public string Username { get; set; } = "testusername@constoso.com";
        [JsonPropertyName("auth_provider")]
        public string AuthProvider { get; set; } = "aad";
        [JsonPropertyName("auth_token")]
        public string AuthToken { get; set; } = "your_aad_id_token";
        [JsonPropertyName("client_principal_b64")]
        public string ClientPrincipalB64 { get; set; } = "your_base_64_encoded_token";
        [JsonPropertyName("aad_id_token")]
        public string AadIdToken { get; set; } = "your_aad_id_token";
    }

    public class EasyAuthUserHeaders
    {
        public string Accept { get; set; } = "*/*";
        public string AcceptEncoding { get; set; } = "gzip, deflate, br";
        public string AcceptLanguage { get; set; } = "en";
        public string ClientIp { get; set; } = "22.222.222.2222:64379";
        public string ContentLength { get; set; } = "192";
        public string ContentType { get; set; } = "application/json";
        public string Cookie { get; set; } = "AppServiceAuthSession=/AuR5ENU+pmpoN3jnymP8fzpmVBgphx9uPQrYLEWGcxjIITIeh8NZW7r3ePkG8yBcMaItlh1pX4nzg5TFD9o2mxC/5BNDRe/uuu0iDlLEdKecROZcVRY7QsFdHLjn9KB90Z3d9ZeLwfVIf0sZowWJt03BO5zKGB7vZgL+ofv3QY3AaYn1k1GtxSE9HQWJpWar7mOA64b7Lsy62eY3nxwg3AWDsP3/rAta+MnDCzpdlZMFXcJLj+rsCppW+w9OqGhKQ7uCs03BPeon3qZOdmE8cOJW3+i96iYlhneNQDItHyQqEi1CHbBTSkqwpeOwWP4vcwGM22ynxPp7YFyiRw/X361DGYy+YkgYBkXq1AEIDZ44BCBz9EEaEi0NU+m6yUOpNjEaUtrJKhQywcM2odojdT4XAY+HfTEfSqp0WiAkgAuE/ueCu2JDOfvxGjCgJ4DGWCoYdOdXAN1c+MenT4OSvkMO41YuPeah9qk9ixkJI5s80lv8rUu1J26QF6pstdDkYkAJAEra3RQiiO1eAH7UEb3xHXn0HW5lX8ZDX3LWiAFGOt5DIKxBKFymBKJGzbPFPYjfczegu0FD8/NQPLl2exAX3mI9oy/tFnATSyLO2E8DxwP5wnYVminZOQMjB/I4g3Go14betm0MlNXlUbU1fyS6Q6JxoCNLDZywCoU9Y65UzimWZbseKsXlOwYukCEpuQ5QPT55LuEAWhtYier8LSh+fvVUsrkqKS+bg0hzuoX53X6aqUr7YB31t0Z2zt5TT/V3qXpdyD8Xyd884PqysSkJYa553sYx93ETDKSsfDguanVfn2si9nvDpvUWf6/R02FmQgXiaaaykMgYyIuEmE77ptsivjH3hj/MN4VlePFWokcchF4ciqqzonmICmjEHEx5zpjU2Kwa+0y7J5ROzVVygcnO1jH6ZKDy9bGGYL547bXx/iiYBYqSIQzleOAkCeULrGN2KEHwckX5MpuRaqTpoxdZH9RJv0mIWxbDA0kwGsbMICQd0ZODBkPUnE84qhzvXInC+TL7MbutPEnGbzgxBAS1c2Ct4vxkkjykOeOxTPxqAhxoefwUfIwZZax6A9LbeYX2bsBpay0lScHcA==";
        public string DisguisedHost { get; set; } = "your_app_service.azurewebsites.net";
        public string Host { get; set; } = "your_app_service.azurewebsites.net";
        public string MaxForwards { get; set; } = "10";
        public string Origin { get; set; } = "https://your_app_service.azurewebsites.net";
        public string Referer { get; set; } = "https://your_app_service.azurewebsites.net/";
        public string SecChUa { get; set; } = "\"Microsoft Edge\";v=\"113\"; \"Chromium\";v=\"113\"; \"Not-A.Brand\";v=\"24\"";
        public string SecChUaMobile { get; set; } = "?0";
        public string SecChUaPlatform { get; set; } = "\"Windows\"";
        public string SecFetchDest { get; set; } = "empty";
        public string SecFetchMode { get; set; } = "cors";
        public string SecFetchSite { get; set; } = "same-origin";
        public string Traceparent { get; set; } = "00-24e9a8d1b06f233a3f1714845ef971a9-3fac69f81ca5175c-00";
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36 Edg/113.0.1774.42";
        public string WasDefaultHostname { get; set; } = "your_app_service.azurewebsites.net";
        public string XAppserviceProto { get; set; } = "https";
        public string XArrLogId { get; set; } = "4102b832-6c88-4c7c-8996-0edad9e4358f";
        public string XArrSsl { get; set; } = "2048|256|CN=Microsoft Azure TLS Issuing CA 02, O=Microsoft Corporation, C=US|CN=*.azurewebsites.net, O=Microsoft Corporation, L=Redmond, S=WA, C=US";
        public string XClientIp { get; set; } = "22.222.222.222";
        public string XClientPort { get; set; } = "64379";
        public string XForwardedFor { get; set; } = "22.222.222.22:64379";
        public string XForwardedProto { get; set; } = "https";
        public string XForwardedTlsversion { get; set; } = "1.2";
        public string XMsClientPrincipal { get; set; } = "your_base_64_encoded_token";
        public string XMsClientPrincipalId { get; set; } = "00000000-0000-0000-0000-000000000000";
        public string XMsClientPrincipalIdp { get; set; } = "aad";
        public string XMsClientPrincipalName { get; set; } = "testusername@oso.com";
        public string XMsTokenAadIdToken { get; set; } = "your_aad_id_token";
        public string XOriginalUrl { get; set; } = "/chatgpt";
        public string XSiteDeploymentId { get; set; } = "your_app_service";
        public string XWawsUnencodedUrl { get; set; } = "/chatgpt";
    }
}
