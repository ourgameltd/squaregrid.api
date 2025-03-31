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
        var blobContent = Layouts.Redirect.Replace("{{url}}", model.Url);
        blobContent = blobContent.Replace("{{title}}", model.Title);
        blobContent = blobContent.Replace("{{description}}", model.Description);
        blobContent = blobContent.Replace("{{image}}", model.Image);

        var path = $"{model.GroupName.GenerateSlug()}/{model.CardName.GenerateSlug()}/index.html";
        var blobClient = containerClient.GetBlobClient(path.ToLower());
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
    public required string GroupName { get; set; }
    public required string CardName { get; set; }
    public string FullUrl => string.Join("/", new[] { "https://blobstoragecontainer.com", GroupName.GenerateSlug(), CardName.GenerateSlug(), "index.html" });
}
