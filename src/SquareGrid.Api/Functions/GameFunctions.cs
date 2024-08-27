using Azure;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Models;
using SquareGrid.Common.Services.Tables.Models;
using SquareGrid.Common.Utils;
using System.Net;

namespace SquareGrid.Api.Functions
{
    public class GameFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public GameFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger<GameFunctions> logger) : base(tableManager, mediaManager, logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(GetGame), tags: ["game"], Summary = "Get a game by its user and id.", Description = "Get a game by its user and id.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Game), Description = "The square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound)]
        [Function(nameof(GetGame))]
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            Game game;

            try
            {
                game = await GetGameByUserOrThrow(req, gameId);
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Game[]), Description = "An array of square grid game models.")]
        [Function(nameof(GetGames))]
        public async Task<HttpResponseData> GetGames(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = req.GetUser(logger);
            var gameEntitites = await tableManager.GetAllAsync<SquareGridGame>(user.ObjectId);
            var games = gameEntitites.Select(i => i.ToGame()).ToList();

            // This is a bit shit for now to get the blocks associated with each game, wont take long, but you get what I mean
            foreach (var game in games)
            {
                var blockEntities = await tableManager.GetAllAsync<SquareGridBlock>(game.RowKey);
                var blocks = blockEntities.Select(i => i.ToBlock()).ToList();
                game.SetBlocks(blocks);
            }

            games = games.OrderBy(i => i.IsWon).ThenByDescending(i => i.Timestamp).ToList();
            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(games);
            return okResponse;
        }

        [OpenApiOperation(operationId: nameof(PutGame), tags: ["game"], Summary = "Create a new game for a logged in user.", Description = "Create a new game for a logged in user.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PutGame))]
        public async Task<HttpResponseData> PutGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            // Get the user
            var user = req.GetUser(logger);

            var gameEntitites = await tableManager.GetAllAsync<SquareGridGame>(user.ObjectId);
            var games = gameEntitites.Select(i => i.ToGame()).ToList();

            // This is a bit shit for now to get the blocks associated with each game, wont take long, but you get what I mean
            foreach (var game in games)
            {
                var blockEntities = await tableManager.GetAllAsync<SquareGridBlock>(game.RowKey);
                var blocks = blockEntities.Select(i => i.ToBlock()).ToList();
                game.SetBlocks(blocks);
            }

            if (games.Any(i => i.IsWon == false))
            {
                // You can only have 1 game at a time for now when creating manually
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Update the record and create the game
            var newGameEntity = new SquareGridGame()
            {
                Description = "My new game description.",
                PartitionKey = user.ObjectId,
                RowKey = Guid.NewGuid().ToString().ToLower(),
                Title = "My new game!"
            };

            await tableManager.Insert(newGameEntity);
            var newGame = newGameEntity.ToGame();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(newGame);
            return response;
        }

        [OpenApiOperation(operationId: nameof(PostGame), tags: ["game"], Summary = "Updates a game for a user.", Description = "Updates a game for a user.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Game), Description = "A square grid game model.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(PostGame))]
        public async Task<HttpResponseData> PostGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "games/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = req.GetUser();

            var parser = await MultipartFormDataParser.ParseAsync(req.Body);
            var image = await UploadImageIfPopulated($"images/games/{gameId}", req, ctx, parser);
            var data = await req.GetFromStringValidated<Game>(parser);

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            var gameEntity = await tableManager.GetAsync<SquareGridGame>(user.ObjectId, gameId);

            if (gameEntity == null)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            gameEntity.RowKey = gameId;
            gameEntity.Title = data.Body!.Title;
            gameEntity.Description = data.Body!.Description;
            gameEntity.GroupName = data.Body!.GroupName.GenerateSlug();
            gameEntity.ShortName = data.Body!.ShortName.GenerateSlug();
            gameEntity.DisplayAsGrid = data.Body!.DisplayAsGrid;    
            gameEntity.ConfirmedWinnersOnly = data.Body!.ConfirmedWinnersOnly;

            if (!string.IsNullOrWhiteSpace(image))
            {
               gameEntity.Image = image;
            }

            if (!string.IsNullOrWhiteSpace(gameEntity.GroupName) && !string.IsNullOrWhiteSpace(gameEntity.ShortName))
            {
                var lookup = await tableManager.GetAsync<SquareGridLookup>(gameEntity.GroupName, gameEntity.ShortName);

                if (lookup == null)
                {
                    await tableManager.Insert(new SquareGridLookup()
                    {
                        GameId = gameId,
                        UserId = user.ObjectId,
                        PartitionKey = gameEntity.GroupName,
                        RowKey = gameEntity.ShortName
                    });
                }
                else
                {
                    if (lookup.UserId != user.ObjectId)
                    {
                        return req.CreateResponse(HttpStatusCode.Forbidden);
                    }

                    lookup.GameId = gameId;

                    await tableManager.Update(lookup, Azure.Data.Tables.TableUpdateMode.Merge);
                }
            }

            var gameBlocks = await tableManager.GetAllAsync<SquareGridBlock>(gameId);

            if (gameBlocks.Any(i => i.PartitionKey != gameId))
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            foreach (var block in gameBlocks)
            {
                block.PartitionKey = gameId;
            }

            var blocksDeleted = gameBlocks.Where(i => data.Body.Blocks.All(b => b.RowKey != i.RowKey)).ToList();
            var blocksMatched = gameBlocks.Where(i => data.Body.Blocks.Any(b => b.RowKey == i.RowKey)).ToList();
            var blocksAdded = data.Body.Blocks.Where(i => gameBlocks.All(b => b.RowKey != i.RowKey)).ToList();

            try
            {
                foreach (var block in blocksDeleted)
                {
                    await tableManager.DeleteAsync<SquareGridBlock>(block.PartitionKey, block.RowKey);
                }

                foreach (var block in blocksMatched)
                {
                    var blockData = data.Body.Blocks.First(i => i.RowKey == block.RowKey);

                    // If the ETag is different, then the block has been updated since the user last saw it

                    if (blockData.ETag != block.ETag.ToString())
                    {
                        var response = req.CreateResponse(HttpStatusCode.Conflict);
                        await response.WriteStringAsync("Some blocks have ben updated since your edits, please refresh and try again.");
                        return response;
                    }

                    // if the new block is the same as the old block, then we can skip it

                    if (block.Equals(blockData))
                    {
                        continue;
                    }

                    block.ClaimedByFriendlyName = blockData.ClaimedByFriendlyName;
                    block.ClaimedByUserId = blockData.ClaimedByUserId;
                    block.DateClaimed = blockData.DateClaimed;
                    block.DateConfirmed = blockData.DateConfirmed;
                    block.Title = blockData.Title;
                    await tableManager.Update(block);
                }

                foreach (var block in blocksAdded)
                {
                    var newBlock = block.ToBlock();
                    await tableManager.Insert(newBlock);
                }       
                
                await tableManager.Update(gameEntity);
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
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [Function(nameof(DrawWinner))]
        public async Task<HttpResponseData> DrawWinner(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "games/{gameId}/winner")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            bool confirmedWinnerOnly = false;

            if (bool.TryParse(req.Query["confirmedWinnerOnly"], out bool parsedIsChampion))
            {
                confirmedWinnerOnly = parsedIsChampion;
            }

            var user = req.GetUser();

            Game game;

            try
            {
                game = await GetGameByUserOrThrow(req, gameId);
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

            if (game.Blocks.All(i => i.IsClaimed == false))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            if (confirmedWinnerOnly && game.Blocks.All(i => i.IsConfirmed == false))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var winningBlock = game.PickAWinner(confirmedWinnerOnly);

            if (winningBlock == null)
            {
               return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            SquareGridBlock? blockEntity = await tableManager.GetAsync<SquareGridBlock>(gameId, winningBlock.RowKey);
            blockEntity!.IsWinner = true;

            await tableManager.Update(blockEntity);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(game);
            return response;
        }
    }
}
