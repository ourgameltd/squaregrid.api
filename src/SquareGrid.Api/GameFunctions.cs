using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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

        [Function(nameof(GetGame))]
        [Authorize]
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games/{gameId}")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = ctx.GetUser();
            var game = await tableManager.GetAsync<SquareGridGame>(user.ObjectId, gameId);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(game);
            return okResponse;
        }

        [Function(nameof(GetGames))]
        [Authorize]
        public async Task<HttpResponseData> GetGames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            var games = await tableManager.GetAllAsync<SquareGridGame>(user.ObjectId);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(games);
            return okResponse;
        }

        [Function(nameof(PutGame))]
        [Authorize]
        public async Task<HttpResponseData> PutGame(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            data.Body!.PartitionKey = user.ObjectId;

            await tableManager.Insert(data.Body!);
            return req.CreateResponse(HttpStatusCode.Created);
        }

        [Function(nameof(PostGame))]
        [Authorize]
        public async Task<HttpResponseData> PostGame(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "games")] HttpRequestData req, FunctionContext ctx)
        {
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<SquareGridGame>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            data.Body!.PartitionKey = user.ObjectId;

            await tableManager.Update(data.Body!);
            return req.CreateResponse(HttpStatusCode.Created);
        }
    }
}
