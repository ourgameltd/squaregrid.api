using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Services.Tables.Models;
using System.Net;

namespace SquareGrid.Api.Functions
{
    public class GameFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public GameFunctions(TableManager tableManager, ILogger<GameFunctions> logger) : base(tableManager, logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(LookupFriendlyName), tags: ["game"], Summary = "Check if a country code and friendly name exists.", Description = "Check if a country code and friendly name exists.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(LookupFriendlyName))]
        public async Task<HttpResponseData> LookupFriendlyName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{countryCode}/{friendlyName}/lookup")] HttpRequestData req, FunctionContext ctx,
            string countryCode,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(countryCode, friendlyName);

            if (lookup == null) 
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(GetGameByFriendlyName), tags: ["game"], Summary = "Get a game by its country and friendly name.", Description = "Get a game by its country and friendly name.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGameByFriendlyName))]
        public async Task<HttpResponseData> GetGameByFriendlyName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{countryCode}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
            string countryCode,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(countryCode, friendlyName);

            if (lookup == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            SquareGridGame game = await GetGameByUserOrThrow(ctx, lookup.GameId, lookup.UserId);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(game);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(AddFriendlyName), tags: ["game"], Summary = "Add friendly name on a  game for a logged in user.", Description = "Add friendly name on a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(AddFriendlyName))]
        [Authorize]
        public async Task<HttpResponseData> AddFriendlyName(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games/{gameId}/{countryCode}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string countryCode,
            string friendlyName)
        {
            // Get the user and request body and validate it
            var user = ctx.GetUser();

            SquareGridGame game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            await tableManager.Insert(new SquareGridLookup()
            { 
                PartitionKey = countryCode,
                RowKey = friendlyName,
                GameId = gameId,
                UserId = user.ObjectId
            });
            return req.CreateResponse(HttpStatusCode.Created);
        }

        [OpenApiOperation(operationId: nameof(GetGame), tags: ["game"], Summary = "Get a game by its user and id.", Description = "Get a game by its user and id.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGame))]
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{userId}/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string userId,
            string gameId)
        {
            SquareGridGame game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId, userId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(game);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(GetGames), tags: ["game"], Summary = "Get all games for the logged in user.", Description = "Get all games for the logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SquareGridGame[]), Description = "An array of square grid game models.")]
        [Function(nameof(GetGames))]
        [Authorize]
        public async Task<HttpResponseData> GetGames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            var games = await tableManager.GetAllAsync<SquareGridGame>(user.ObjectId);

            // This is a bit shit for now to get the blocks associated with each game, wont take long, but you get what I mean
            foreach (var game in games)
            {
                var blocks = await tableManager.GetAllAsync<SquareGridBlock>(game.RowKey);
                game.SetBlocks(blocks);
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(games);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(PutGame), tags: ["game"], Summary = "Create a new game for a logged in user.", Description = "Create a new game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "A square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PutGame))]
        [Authorize]
        public async Task<HttpResponseData> PutGame(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            // Get the user and request body and validate it
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            SquareGridGame game;

            try
            {
                // If the game exists, return a conflict
                game = await GetGameByUserOrThrow(ctx, data.Body!.RowKey);

                if (game != null)
                {
                    return req.CreateResponse(HttpStatusCode.Conflict);
                }
            }
            catch (SquareGridException)
            {
                logger.LogDebug("Game not found, creating new game.");
            }

            // Update the record and create the game
            data.Body!.PartitionKey = user.ObjectId;
            await tableManager.Insert(data.Body!);
            return req.CreateResponse(HttpStatusCode.Created);
        }

        [OpenApiOperation(operationId: nameof(PostGame), tags: ["game"], Summary = "Updates a game for a user.", Description = "Updates a game for a user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SquareGridGame), Description = "A square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(PostGame))]
        [Authorize]
        public async Task<HttpResponseData> PostGame(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "games/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            SquareGridGame game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            data.Body!.RowKey = gameId;

            await tableManager.Update(data.Body!);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(DrawWinner), tags: ["game"], Summary = "Draw winner for a game for a logged in user.", Description = "Draw winner for a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [Function(nameof(DrawWinner))]
        [Authorize]
        public async Task<HttpResponseData> DrawWinner(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "games/{gameId}/winner")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            SquareGridGame game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            if (game.IsWon)
            {
                return req.CreateResponse(HttpStatusCode.Conflict);
            }

            if (game.Blocks.Count <= 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            if (game.Blocks.All(i => i.IsConfirmed == false))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var winningBlock = game.PickAWinner();

            if (winningBlock == null)
            {
               return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            winningBlock.IsWinner = true;

            await tableManager.Update(winningBlock);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
