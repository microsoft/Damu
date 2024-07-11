namespace Api.Models;

public class QueryRequest
{
    public string? Query { get; set; }
    public int? KNearestNeighborsCount { get; set; }
    public string? Filter { get; set; }
}