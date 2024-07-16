namespace IndexOrchestration.Functions;

public partial class SearchAsync
{
    public class ApiSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
        public object Object { get; set; } = string.Empty;
        public List<ApiSearchResultChoice> Choices { get; set; } = [];
        public object HistoryMetadata { get; set; } = string.Empty;
        public string ApimRequestId { get; set; } = string.Empty;
    }
}
