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
using HttpMultipartParser;

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
            return await UploadImage($"images/user/{user.ObjectId}", req, ctx);
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

            return await UploadImage($"images/games/{gameId}", req, ctx);
        }

        private async Task<HttpResponseData> UploadImage(string path, HttpRequestData req, FunctionContext ctx)
        {
            var response = req.CreateResponse();

            var parser = await MultipartFormDataParser.ParseAsync(req.Body);

            if (parser.Files.Count == 0)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("No file uploaded.");
                return response;
            }

            var file = parser.Files[0];

            // Validate the upload type
            _ = MimeTypeX.GetExtension(file.ContentType);

            using (var fileStream = file.Data)
            {
                await mediaManager.Upload($"/{path}", fileStream);
                response.StatusCode = HttpStatusCode.Created;
            }

            return response;
        }
    }
}
