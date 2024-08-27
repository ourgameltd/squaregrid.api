using Azure;
using Azure.Data.Tables;
using SquareGrid.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Common.Services.Tables.Models
{
    public class SquareGridBlock : ITableEntity, IBlock
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
        /// Date the block was confirmed
        /// </summary>
        public DateTime? DateConfirmed { get; set; }

        /// <summary>
        /// Is winner
        /// </summary>
        public bool IsWinner { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        /// <summary>
        /// Convert to game model
        /// </summary>
        /// <returns></returns>
        public Block ToBlock()
        {
            return new Block()
            {
                PartitionKey = PartitionKey,
                ETag = ETag.ToString(),
                Timestamp = Timestamp,
                ClaimedByFriendlyName = ClaimedByFriendlyName,
                ClaimedByUserId = ClaimedByUserId,
                DateClaimed = DateClaimed,
                DateConfirmed = DateConfirmed,
                Index = Index,
                IsWinner = IsWinner,
                RowKey = RowKey,
                Title = Title
            };
        }

        /// <summary>
        /// Check if its changed
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IBlock other)
        {
            return other != null &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey &&
                   Title == other.Title &&
                   Index == other.Index &&
                   ClaimedByUserId == other.ClaimedByUserId &&
                   ClaimedByFriendlyName == other.ClaimedByFriendlyName &&
                   DateClaimed == other.DateClaimed &&
                   DateConfirmed == other.DateConfirmed &&
                   IsWinner == other.IsWinner;
        }
    }
}
