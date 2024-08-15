using Azure;
using Azure.Data.Tables;
using SquareGrid.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace SquareGrid.Common.Services.Tables.Models
{
    public class SquareGridGame : ITableEntity, IGame
    {
        /// <summary>
        /// The UserId from B2C
        /// </summary>
        [Required]
        public required string PartitionKey { get; set; }

        /// <summary>
        /// The Id of the game
        /// </summary>
        [Required]
        public required string RowKey { get; set; }

        /// <summary>
        /// The title of the game
        /// </summary>
        [Required]
        public required string Title { get; set; }

        /// <summary>
        /// Optional image for the game
        /// </summary>
        public string? Image { get; set; }

        /// <summary>
        /// A description for the game
        /// </summary>
        [Required]
        public required string Description { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        /// <summary>
        /// Convert to game model
        /// </summary>
        /// <returns></returns>
        public Game ToGame()
        {
            return new Game()
            {
                ETag = ETag,
                Timestamp = Timestamp,
                PartitionKey = PartitionKey,
                RowKey = RowKey,
                Title = Title,
                Image = Image,
                Description = Description
            };
        }
    }
}
