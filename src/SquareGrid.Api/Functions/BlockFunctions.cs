using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SquareGrid.Api.Functions;
using SquareGrid.Api.Functions.Models.Requests;
using SquareGrid.Api.Utils;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Models;
using SquareGrid.Common.Services.Tables.Models;
using System.Net;

namespace SquareGrid.Api
{
    public class BlockFunctions : CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger<GameFunctions> logger;

        public BlockFunctions(TableManager tableManager, MediaBlobManager mediaManager, ILogger<GameFunctions> logger) : base(tableManager, mediaManager, logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: nameof(PutBlock), tags: ["block"], Summary = "Add a block for a game for a logged in user.", Description = "Add a block for a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PutBlockRequest), Description = "A request model to add a block.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created)]
        [Function(nameof(PutBlock))]
        public async Task<HttpResponseData> PutBlock(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "games/{gameId}/block")] HttpRequestData req, FunctionContext ctx,
            string gameId)
        {
            var user = await ctx.GetUser();
            var data = await req.GetFromBodyValidated<PutBlockRequest>();

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

            var newBlock = new SquareGridBlock
            {
                Index = game.GetNextAvailableBlockIndex(),
                Title = data.Body!.Title,
                ClaimedByFriendlyName = data.Body?.ClaimedBy,
                DateClaimed = data.Body?.ClaimedBy != null ? DateTime.UtcNow : null,
                DateConfirmed = data.Body!.Confirmed ? DateTime.UtcNow : null,
                PartitionKey = game.RowKey,
                RowKey = Guid.NewGuid().ToString()
            };

            await tableManager.Insert(newBlock);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(DeleteBlock), tags: ["block"], Summary = "Delete a block on a game for a logged in user.", Description = "Delete a block on a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(DeleteBlock))]
        public async Task<HttpResponseData> DeleteBlock(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "games/{gameId}/block/{blockId}")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string blockId)
        {
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

            await tableManager.DeleteAsync<SquareGridBlock>(gameId, blockId);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(ClaimBlock), tags: ["block"], Summary = "Claim a block on a game.", Description = "Claim a block on a game.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ClaimBlockRequest), Description = "A request model to claim a block thats free.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(ClaimBlock))]
        public async Task<HttpResponseData> ClaimBlock(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "games/{gameId}/block/{blockId}/claim")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string blockId)
        {
            User? user = await ctx.GetUserIfPopulated();
            var data = await req.GetFromBodyValidated<ClaimBlockRequest>();

            if (!data.IsValid)
            {
                return data.HttpResponseData!;
            }

            SquareGridBlock? blockEntity = await tableManager.GetAsync<SquareGridBlock>(gameId, blockId);
            
            if (blockEntity == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var block = blockEntity.ToBlock();
            if (block.IsClaimed)
            {
                return req.CreateResponse(HttpStatusCode.Conflict);
            }

            blockEntity.ClaimedByFriendlyName = data.Body?.ClaimedBy;
            blockEntity.DateClaimed = DateTime.UtcNow;

            if (Guid.TryParse(user?.ObjectId, out Guid guid))
            {
                blockEntity.ClaimedByUserId = guid;
            }

            await tableManager.Update(blockEntity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(RemoveClaimBlock), tags: ["block"], Summary = "Remove claim for block on a game for a logged in user.", Description = "Remove claim for block on a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(RemoveClaimBlock))]
        public async Task<HttpResponseData> RemoveClaimBlock(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "games/{gameId}/block/{blockId}/claim")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string blockId)
        {

            User? user = await ctx.GetUserIfPopulated();

            Game game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            SquareGridBlock? blockEntity = await tableManager.GetAsync<SquareGridBlock>(gameId, blockId);

            if (blockEntity == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            blockEntity.ClaimedByFriendlyName = null;
            blockEntity.DateClaimed = null;
            blockEntity.DateConfirmed = null;
            blockEntity.ClaimedByUserId = null;

            await tableManager.Update(blockEntity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [OpenApiOperation(operationId: nameof(ConfirmBlock), tags: ["block"], Summary = "Confirm a block on a game for a logged in user.", Description = "Conform a block on a game for a logged in user.")]
        [OpenApiSecurity("function_auth", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-functions-key")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent)]
        [Function(nameof(ConfirmBlock))]
        public async Task<HttpResponseData> ConfirmBlock(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "games/{gameId}/block/{blockId}/confirm")] HttpRequestData req, FunctionContext ctx,
            string gameId,
            string blockId)
        {
            User user = await ctx.GetUser();

            Game game;

            try
            {
                game = await GetGameByUserOrThrow(ctx, gameId);
            }
            catch (SquareGridException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            SquareGridBlock? blockEntity = await tableManager.GetAsync<SquareGridBlock>(game.RowKey, blockId);

            if (blockEntity == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var block = blockEntity.ToBlock();

            if (!block.IsClaimed)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            blockEntity.DateConfirmed = blockEntity.DateConfirmed.HasValue ? null : DateTime.UtcNow;

            await tableManager.Update(blockEntity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
