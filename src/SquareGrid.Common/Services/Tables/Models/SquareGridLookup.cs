using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Common.Services.Tables.Models
{
    public class SquareGridLookup : ITableEntity
    {
        /// <summary>
        /// A country code slug
        /// </summary>
        [Required]
        public required string PartitionKey { get; set; }

        /// <summary>
        /// The slug friendly name
        /// </summary>
        [Required]
        public required string RowKey { get; set; }

        /// <summary>
        /// The game id
        /// </summary>
        [Required]
        public required string GameId { get; set; }

        /// <summary>
        /// The user id
        /// </summary>
        [Required]
        public required string UserId { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
