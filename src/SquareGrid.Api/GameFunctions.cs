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
        public async Task<HttpResponseData> GetGame(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "game/{userId}/{gameId}")] HttpRequestData req,
            string userId,
            string gameId)
        {
            var game = await tableManager.GetAsync<SquareGridGame>(userId, gameId);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(game);
            return okResponse;
        }

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
