// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace ChatApp.Server.Models;

public record SupportingContentRecord(string Title, string Content, string Url, string Filepath, string ChunkId, string AdditionalContent);

public record ToolContentResponse(List<SupportingContentRecord> Citations, List<string> Intent);

public record ResponseMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("totalTokens")] int TotalTokens);
