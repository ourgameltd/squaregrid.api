using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Common.Services.Tables.Models
{
    public class SquareGridGame : ITableEntity
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

        /// <summary>
        /// Options for the game
        /// </summary>
        [Required]
        public required string Options { get; set; }

        /// <summary>
        /// Decide if its public
        /// </summary>
        public required bool Published { get; set; }

        /// <summary>
        /// How many blocks does it include
        /// </summary>
        public required int Blocks { get; set; }

        /// <summary>
        /// How many blacks have been claimed by someone
        /// </summary>
        public required int BlocksClaimed { get; set; }

        /// <summary>
        /// How many blocks are left
        /// </summary>
        public int BlocksRemaining => Blocks - BlocksClaimed;

        /// <summary>
        /// Which block was the winner
        /// </summary>
        public string? WinnerBlock { get; set; }

        /// <summary>
        /// Friendly id of registered winner
        /// </summary>
        public string? WinnerFriendlyName { get; set; }

        /// <summary>
        /// User id of registered winner
        /// </summary>
        public Guid? WinnerUserId { get; set; }

        /// <summary>
        /// Is the game complete
        /// </summary>
        public bool IsCompleted => BlocksRemaining <= 0;

        /// <summary>
        /// Does the game have a winner picked
        /// </summary>
        public bool IsWon { get; set; } = false;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
