using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Models;
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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{group}/{friendlyName}/lookup")] HttpRequestData req, FunctionContext ctx,
            string group,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(group, friendlyName);

            if (lookup == null) 
            {
                return req.CreateResponse(HttpStatusCode.NoContent);
            }

            return req.CreateResponse(HttpStatusCode.Conflict);
        }

        [OpenApiOperation(operationId: nameof(GetGameByFriendlyName), tags: ["game"], Summary = "Get a game by its country and friendly name.", Description = "Get a game by its country and friendly name.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Game), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGameByFriendlyName))]
        public async Task<HttpResponseData> GetGameByFriendlyName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{group}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
            string group,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(group, friendlyName);

            if (lookup == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            Game game = await GetGameByUserOrThrow(ctx, lookup.GameId, lookup.UserId);

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
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games/{gameId}/{group}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string group,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(group, friendlyName);

            if (lookup != null)
            {
                return req.CreateResponse(HttpStatusCode.Conflict);
            }

            var user = ctx.GetUser();

            Game game;

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
                PartitionKey = group,
                RowKey = friendlyName,
                GameId = gameId,
                UserId = user.ObjectId
            });
            return req.CreateResponse(HttpStatusCode.Created);
        }

        [OpenApiOperation(operationId: nameof(GetGame), tags: ["game"], Summary = "Get a game by its user and id.", Description = "Get a game by its user and id.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Game), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGame))]
        [Authorize]
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            Game game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Game[]), Description = "An array of square grid game models.")]
        [Function(nameof(GetGames))]
        [Authorize]
        public async Task<HttpResponseData> GetGames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            var gameEntitites = await tableManager.GetAllAsync<SquareGridGame>(user.ObjectId);
            var games = gameEntitites.Select(i => i.ToGame()).ToList();

            // This is a bit shit for now to get the blocks associated with each game, wont take long, but you get what I mean
            foreach (var game in games)
            {
                var blockEntities = await tableManager.GetAllAsync<SquareGridBlock>(game.RowKey);
                var blocks = blockEntities.Select(i => i.ToBlock()).ToList();
                game.SetBlocks(blocks);
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(gameEntitites);
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

            Game game;

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

            Game game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            data.Body!.RowKey = gameId;

            try
            {                 
                await tableManager.Update(data.Body!);
            }
            catch (Exception e)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(e.Message);
                return response;
            }

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

            Game game;

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

            SquareGridBlock? blockEntity = await tableManager.GetAsync<SquareGridBlock>(gameId, winningBlock.RowKey);
            blockEntity!.IsWinner = true;

            await tableManager.Update(blockEntity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
