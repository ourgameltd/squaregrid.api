using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SquareGrid.Common.Resources;
using System.Text;

public class RedirectBlobManager
{
    private readonly BlobContainerClient containerClient;

    public RedirectBlobManager(BlobServiceClient blobServiceClient)
    {
        this.containerClient = blobServiceClient.GetBlobContainerClient("$web");
    }

    public async Task Upload(RedirectModel model)
    {
        var blobContent = Layouts.Redirect.Replace("url", model.Url);
        blobContent = blobContent.Replace("title", model.Title);
        blobContent = blobContent.Replace("description", model.Description);
        blobContent = blobContent.Replace("image", model.Image);

        var blobClient = containerClient.GetBlobClient(model.FriendlyUrl.ToLower());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "text/html" });
    }
}

public class RedirectModel
{
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Image { get; set; }
    public required string FriendlyUrl { get; set; }
    public required string Domain { get; set; }
    public string FullUrl => string.Join("/", new[] { Domain, FriendlyUrl });
}