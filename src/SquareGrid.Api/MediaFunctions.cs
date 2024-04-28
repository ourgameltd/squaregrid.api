using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SquareGrid.Api
{
    public class MediaFunctions
    {
        private readonly MediaBlobManager mediaManager;
        private readonly ILogger<MediaFunctions> logger;

        public MediaFunctions(MediaBlobManager mediaManager, ILogger<MediaFunctions> logger)
        {
            this.mediaManager = mediaManager;
            this.logger = logger;
        }

        [Function(nameof(UploadProfileImage))]
        public Task<IActionResult> UploadProfileImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "images/{userId}")] HttpRequest req,
            Guid userId) 
                => UploadImage($"images/{userId}", req);

        [Function(nameof(UploadGridImage))]
        public Task<IActionResult> UploadGridImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "images/{userId}/{gridId}")] HttpRequest req,
            Guid userId,
            Guid gridId) 
                => UploadImage($"images/{userId}/{gridId}", req);

        private async Task<IActionResult> UploadImage(string path, HttpRequest req)
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("The request must be multipart/form-data.");
            }

            var formdata = await req.ReadFormAsync();
            var file = req.Form.Files["file"];

            if (file != null)
            {
                var stream = file.OpenReadStream();
                await mediaManager.Upload(path, stream);
            }

            return new CreatedResult();
        }
    }
}
