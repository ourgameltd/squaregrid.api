using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Services.Tables.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace SquareGrid.Api
{
    public class MediaFunctions
    {
        private readonly TableManager tableManager;
        private readonly MediaBlobManager mediaManager;
        private readonly ILogger<MediaFunctions> logger;

        public MediaFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger<MediaFunctions> logger)
        {
            this.tableManager = tableManager;
            this.mediaManager = mediaManager;
            this.logger = logger;
        }

        [Function(nameof(UploadProfileImage))]
        [Authorize]
        public async Task<HttpResponseData> UploadProfileImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "images/user")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            return await UploadImage($"images/{user.ObjectId}", req, ctx);
        }

        [Function(nameof(UploadGameImage))]
        [Authorize]
        public async Task<HttpResponseData> UploadGameImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "images/game/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = ctx.GetUser();

            var game = await tableManager.GetAsync<SquareGridGame>(user.ObjectId, gameId);

            if (game == null)
            {
                var response = req.CreateResponse(HttpStatusCode.Forbidden);
                return response;
            }

            return await UploadImage($"images/{gameId}", req, ctx);
        }

        private async Task<HttpResponseData> UploadImage(string path, HttpRequestData req, FunctionContext ctx)
        {
            var response = req.CreateResponse();

            if (req.Headers.Contains("content-type") && !req.Headers.GetValues("content-type").First().StartsWith("multipart/form-data"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("The request must be multipart/form-data.");
                return response;
            }

            var boundary = GetBoundary(req.Headers.GetValues("content-type").First());
            if (boundary == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Boundary not found.");
                return response;
            }

            var file = await ParseFormData(req.Body, boundary);
            if (file != null)
            {
                await mediaManager.Upload(path, file);
                response.StatusCode = HttpStatusCode.Created;
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("No file uploaded.");
            }

            return response;
        }

        private async Task<Stream> ParseFormData(Stream body, string boundary)
        {
            var reader = new MultipartReader(boundary, body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                if (section.Headers!.ContainsKey("Content-Disposition"))
                {
                    var contentDisposition = section.Headers["Content-Disposition"];

                    if (contentDisposition.Any(value => value!.Contains("filename=", StringComparison.OrdinalIgnoreCase)))
                    {
                        return section.Body; // Found the file stream
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }

            throw new ArgumentException("This request is not a file upload");
        }

        private string GetBoundary(string contentType)
        {
            var elements = contentType.Split(';');
            var element = elements.FirstOrDefault(entry => entry.Trim().StartsWith("boundary="));
            if (element != null)
            {
                var boundary = element.Trim().Substring("boundary=".Length);
                return boundary.Trim('"');
            }
            throw new ArgumentException("File boundary cannot be found.");
        }
    }
}
