using Azure.Storage.Blobs;

public class MediaBlobManager
{
    private readonly BlobContainerClient containerClient;

    public MediaBlobManager(BlobServiceClient blobServiceClient)
    {
        this.containerClient = blobServiceClient.GetBlobContainerClient("media");
    }

    public async Task Upload(string blobName, Stream stream)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
