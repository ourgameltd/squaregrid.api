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
    public class LookupFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public LookupFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger<GameFunctions> logger) : base(tableManager, mediaManager, logger)
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/{group}/{friendlyName}/lookup")] HttpRequestData req, FunctionContext ctx,
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/{group}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
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
        public async Task<HttpResponseData> AddFriendlyName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "games/{gameId}/{group}/{friendlyName}")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string group,
            string friendlyName)
        {
            var lookup = await tableManager.GetAsync<SquareGridLookup>(group, friendlyName);

            if (lookup != null)
            {
                return req.CreateResponse(HttpStatusCode.Conflict);
            }

            var user = await ctx.GetUser();

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
    }
}
