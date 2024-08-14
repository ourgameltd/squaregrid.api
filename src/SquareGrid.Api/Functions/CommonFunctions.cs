using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SquareGrid.Common.Exceptions;
using SquareGrid.Common.Services.Tables.Models;
using SquareGrid.Api.Utils;

namespace SquareGrid.Api.Functions
{
    public abstract class CommonFunctions
    {
        private readonly TableManager tableManager;
        private readonly ILogger logger;

        public CommonFunctions(TableManager tableManager, ILogger logger)
        {
            this.tableManager = tableManager;
            this.logger = logger;
        }

        /// <summary>
        /// Gets a agame from the request and validates it belongs to a user
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="gameId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="SquareGridException"></exception>
        protected async Task<SquareGridGame> GetGameByUser(FunctionContext ctx, string gameId, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                var user = ctx.GetUser();
                userId = user.ObjectId;
            }

            var game = await tableManager.GetAsync<SquareGridGame>(userId, gameId);

            if (game == null)
            {
                throw new SquareGridException("Game not found for user and game.");
            }

            var blocks = await tableManager.GetAllAsync<SquareGridBlock>(game.RowKey);
            game.SetBlocks(blocks);

            return game;
        }
    }
}
