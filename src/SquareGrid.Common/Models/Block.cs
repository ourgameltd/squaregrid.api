using Azure;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace SquareGrid.Common.Models
{
    public class Block : IBlock
    {
        /// <summary>
        /// PartitionKey is the RowKey of the SquareGridGame
        /// </summary>
        [Required]
        public required string PartitionKey { get; set; }

        /// <summary>
        /// RowKey is the Id of the Block
        /// </summary>
        [Required]
        public required string RowKey { get; set; }

        /// <summary>
        /// The title of the block
        /// </summary>
        [Required]
        public required string Title { get; set; }

        /// <summary>
        /// The index of the block
        /// </summary>
        [Required]
        public required int Index { get; set; }

        /// <summary>
        /// An associated Id of the user who claimed it if they are registered
        /// </summary>
        public Guid? ClaimedByUserId { get; set; }

        /// <summary>
        /// String name of the person who claimed it
        /// </summary>
        public string? ClaimedByFriendlyName { get; set; }

        /// <summary>
        /// Date the block was claimed
        /// </summary>
        public DateTime? DateClaimed { get; set; }

        /// <summary>
        /// Is this claimed
        /// </summary>
        public bool IsClaimed => DateClaimed.HasValue;

        /// <summary>
        /// Date the block was confirmed
        /// </summary>
        public DateTime? DateConfirmed { get; set; }

        /// <summary>
        /// Is this claimed
        /// </summary>
        public bool IsConfirmed => DateConfirmed.HasValue;

        /// <summary>
        /// Is winner
        /// </summary>
        public bool IsWinner { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
