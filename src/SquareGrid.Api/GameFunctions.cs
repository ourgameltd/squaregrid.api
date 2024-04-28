using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Services.Tables.Models;
using System.Net;

namespace SquareGrid.Api
{
    public class GameFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public GameFunctions(TableManager tableManager, ILogger<GameFunctions> logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(GetGame), tags: ["game"], Summary = "Get a game by its user and id.", Description = "Get a game by its user and id.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGame))]
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "game/{userId}/{gameId}")] HttpRequestData req,
            string userId,
            string gameId)
        {
            var game = await tableManager.GetAsync<SquareGridGame>(userId, gameId);

            if (game == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(game);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(GetGames), tags: ["game"], Summary = "Get all games for a userId.", Description = "Get all games for a userId.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SquareGridGame[]), Description = "An array of square grid game models.")]
        [Function(nameof(GetGames))]
        public async Task<HttpResponseData> GetGames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "game/{userId}")] HttpRequestData req,
            string userId)
        {
            var games = await tableManager.GetAllAsync<SquareGridGame>(userId);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(games);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(PutGame), tags: ["game"], Summary = "Create a new game for a user.", Description = "Create a new game for a user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "A square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PutGame))]
        public async Task<HttpResponseData> PutGame(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "game")] HttpRequestData req)
        {
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            await tableManager.Insert(data.Body!);

            return req.CreateResponse(HttpStatusCode.Created);
        }

        [OpenApiOperation(operationId: nameof(PostGame), tags: ["game"], Summary = "Updates a game for a user.", Description = "Updates a game for a user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "A square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PostGame))]
        public async Task<HttpResponseData> PostGame(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "game")] HttpRequestData req)
        {
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            await tableManager.Update(data.Body!);

            return req.CreateResponse(HttpStatusCode.Created);
        }
    }
}
