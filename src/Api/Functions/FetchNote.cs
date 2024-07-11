using Api.Models;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Api.Functions;

public class FetchNote
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<FetchNote> _logger;

    public FetchNote(FunctionSettings functionSettings, ILogger<FetchNote> logger)
    {
        _functionSettings = functionSettings;
        _logger = logger;
    }

    [Function("FetchNote")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "note/{noteId}")] HttpRequest req)
    {
        var noteIdStr = req.RouteValues["noteId"];

        if (!int.TryParse(noteIdStr?.ToString(), out var noteId))
            return new BadRequestObjectResult($"The supplied {nameof(noteId)} of {noteIdStr} is invalid.");

        var blobServiceClient = new BlobServiceClient(_functionSettings.BlobStorageConnStr); //, BlobClientOptions options = default

        var containerClient = blobServiceClient.GetBlobContainerClient("notes");

        var blobClient = containerClient.GetBlobClient(_functionSettings.NoteJsonFileName);

        var blobDownloadResult = await blobClient.DownloadContentAsync();

        if (!blobClient.Exists())
            throw new Exception($"Unable to find source file {_functionSettings.NoteJsonFileName} in storage.");

        if (blobDownloadResult.Value.Content == null)
            throw new Exception($"Unable to retrieve valid content from {_functionSettings.NoteJsonFileName} in storage.");

       //List<SourceNoteRecord> notes = [];

        var contentStream = blobDownloadResult.Value.Content.ToStream();

        var notes = DeserializeNdJson<SourceNoteRecord>(contentStream) ?? [];

        return notes.ToList().Find(note => note.NoteId == noteId) switch
        {
            null => new NotFoundObjectResult($"Note with id {noteId} not found."),
            var note => new OkObjectResult(note)
        };
    }

    private static IEnumerable<T> DeserializeNdJson<T>(Stream stream)
    {
        using var textReader = new StreamReader(stream, new UTF8Encoding(false, true), true, 1024, true);

        List<T> results = [];

        while (textReader.Peek() >= 0)
    {
            var line = textReader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var obj = JsonConvert.DeserializeObject<T>(line);

            if (obj == null)
                continue;

            results.Add(obj);
        }

        return results;
    }
}
