using Azure;

namespace SquareGrid.Common.Models
{
    public interface IBlock
    {
        string? ClaimedByFriendlyName { get; set; }

        Guid? ClaimedByUserId { get; set; }

        DateTime? DateClaimed { get; set; }

        DateTime? DateConfirmed { get; set; }

        int Index { get; set; }

        bool IsWinner { get; set; }

        string PartitionKey { get; set; }

        string RowKey { get; set; }

        DateTimeOffset? Timestamp { get; set; }

        string Title { get; set; }
    }
}