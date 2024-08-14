using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Functions;
using SquareGrid.Api.Models.Requests;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Services.Tables.Models;
using System.Net;

namespace SquareGrid.Api
{
    public class BlockFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public BlockFunctions(TableManager tableManager, ILogger<GameFunctions> logger) : base(tableManager, logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(PutBlock), tags: ["block"], Summary = "Add a block for a game for the logged in user card.", Description = "Add a block for a game for the logged in user card.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PutBlockRequest), Description = "A request model to add a block.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PutBlock))]
        [Authorize]
        public async Task<HttpResponseData> PutBlock(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games/{gameId}/block")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = ctx.GetUser();
            var data = await req.GetFromBodyValidated<PutBlockRequest>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            SquareGridGame game;

            try
            {
                game = await GetGameByUser(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            var newBlock = new SquareGridBlock
            {
                Index = game.GetNextAvailableBlockIndex(),
                Title = data.Body!.Title,
                ClaimedByFriendlyName = data.Body?.ClaimedBy,
                DateClaimed = data.Body?.ClaimedBy != null ? DateTime.UtcNow : null,
                ConfirmedByOwner = data.Body!.Confirmed,
                DateConfirmed = data.Body!.Confirmed ? DateTime.UtcNow : null,
                PartitionKey = game.RowKey,
                RowKey = Guid.NewGuid().ToString()
            };

            await tableManager.Insert(newBlock);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
