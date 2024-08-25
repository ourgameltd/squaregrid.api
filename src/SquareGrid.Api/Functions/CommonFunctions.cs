using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Services.Tables.Models;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Models;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker.Http;
using SquareGrid.Api.Functions.Models.Response;
using System.Net;

namespace SquareGrid.Api.Functions
{
    public abstract class CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly MediaBlobManager mediaManager;
        private readonly ILogger logger;

        public CommonFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger logger)
        {
            this.tableManager = tableManager;
            this.mediaManager = mediaManager;
            this.logger = logger;
        }

        /// <summary>
        /// Gets a agame from the request and validates it belongs to a user
        /// </summary>
        /// <param name="req"></param>
        /// <param name="gameId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="SquareGridException"></exception>
        protected async Task<Game> GetGameByUserOrThrow(HttpRequestData req, string gameId, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                var user = req.GetUser();
                userId = user.ObjectId;
            }

            var gameEntity = await tableManager.GetAsync<SquareGridGame>(userId, gameId);

            if (gameEntity == null)
            {
                throw new SquareGridException("Game not found for user and game.");
            }

            var game = gameEntity.ToGame();

            var blockEntities = await tableManager.GetAllAsync<SquareGridBlock>(gameEntity.RowKey);
            var blocks = blockEntities.Select(b => b.ToBlock()).ToList();
            game.SetBlocks(blocks);

            return game;
        }

        protected async Task<HttpResponseData> UploadImage(string path, HttpRequestData req, FunctionContext ctx)
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
                string extension = Path.GetExtension(file.FileName);
                string relativeFilePath = $"/{path}/{Guid.NewGuid().ToString().ToLower()}{extension}";
                await mediaManager.Upload(relativeFilePath, fileStream);
                var urlResponse = new UploadImageResponse()
                {
                    Url = relativeFilePath
                };
                await response.WriteAsJsonAsync(urlResponse);
                response.StatusCode = HttpStatusCode.Created;
            }

            return response;
        }

        protected async Task<string?> UploadImageIfPopulated(string path, HttpRequestData req, FunctionContext ctx, MultipartFormDataParser? parser = null)
        {
            var response = req.CreateResponse();

            parser = parser != null ? parser : await MultipartFormDataParser.ParseAsync(req.Body);

            if (parser.Files.Count == 0)
            {
                return null;
            }

            var file = parser.Files[0];

            // Validate the upload type
            _ = MimeTypeX.GetExtension(file.ContentType);

            using (var fileStream = file.Data)
            {
                string extension = Path.GetExtension(file.FileName);
                string relativeFilePath = $"/{path}/{Guid.NewGuid().ToString().ToLower()}{extension}";
                await mediaManager.Upload(relativeFilePath, fileStream);
                return relativeFilePath.TrimStart('/');
            }
        }
    }
}
