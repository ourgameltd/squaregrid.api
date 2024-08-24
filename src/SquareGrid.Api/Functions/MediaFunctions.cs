using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Services.Tables.Models;
using System.Net;
using HttpMultipartParser;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Functions.Models.Response;

namespace SquareGrid.Api.Functions
{
    public class MediaFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly MediaBlobManager mediaManager;
        private readonly ILogger<MediaFunctions> logger;

        public MediaFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger<MediaFunctions> logger) : base(tableManager, mediaManager, logger)
        {
            this.tableManager = tableManager;
            this.mediaManager = mediaManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(UploadProfileImage), tags: ["media"], Summary = "Upload a profile image for a logged in user.", Description = "Upload a profile image for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(UploadImageResponse), Description = "Response detailing the saved image location.")]
        [OpenApiRequestBody("multipart/form-data", typeof(FilePart), Required = true, Description = "The image file to upload")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [Function(nameof(UploadProfileImage))]
        public async Task<HttpResponseData> UploadProfileImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "images/user")] HttpRequestData req, FunctionContext ctx)
        {
            var user = await ctx.GetUser();
            return await UploadImage($"images/users/{user.ObjectId}", req, ctx);
        }

        [OpenApiOperation(operationId: nameof(UploadGameImage), tags: ["media"], Summary = "Upload a game image for a logged in user.", Description = "Upload a game image for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(UploadImageResponse), Description = "Response detailing the saved image location.")]
        [OpenApiRequestBody("multipart/form-data", typeof(FilePart), Required = true, Description = "The image file to upload")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [Function(nameof(UploadGameImage))]
        public async Task<HttpResponseData> UploadGameImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "images/game/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = await ctx.GetUser();

            var game = await tableManager.GetAsync<SquareGridGame>(user.ObjectId, gameId);

            if (game == null)
            {
                var response = req.CreateResponse(HttpStatusCode.Forbidden);
                return response;
            }

            return await UploadImage($"images/games/{gameId}", req, ctx);
        }
    }
}
