namespace Api.Models;

public class QueryRequest
{
    public string? Filter { get; set; } = string.Empty;
    public string? Query { get; set; }
    public int KNearestNeighborsCount { get; set; } = 5;
}
