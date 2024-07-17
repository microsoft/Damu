using Azure.Storage.Blobs;
using ChatApp.Server.Models;
using Newtonsoft.Json;
using System.Text;

namespace ChatApp.Server.Services;

public class NoteService
{
    private readonly ILogger<NoteService> _logger;
    private readonly BlobContainerClient _blobContainerClient;

    public NoteService(ILogger<NoteService> logger, BlobContainerClient blobContainerClient)
    {
        _logger = logger;
        _blobContainerClient = blobContainerClient;
    }

    // other CRUD operations not in scope eg, CreateNoteAsync, UpdateNoteAsync, DeleteNoteAsync, GetNotesAsync

    public async Task<Note?> GetNoteAsync(long noteId)
    {
        var blobClient = _blobContainerClient.GetBlobClient($"{noteId}.json");

        if(await blobClient.ExistsAsync() == false)
            return null;

        var blobDownloadResult = await blobClient.DownloadContentAsync();

        if (blobDownloadResult.Value == null)
            throw new Exception($"Failed to download blob {noteId}.json");

        var binaryData = blobDownloadResult.Value.Content;
        
        var json = Encoding.Default.GetString(binaryData);

        var note = JsonConvert.DeserializeObject<Note>(json);

        return note;
    }
}
