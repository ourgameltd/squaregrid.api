using Azure;

namespace SquareGrid.Common.Models
{
    public interface IGame
    {
        string Description { get; set; }

        string? Image { get; set; }

        string? GroupName { get; set; }

        string? ShortName { get; set; }

        string PartitionKey { get; set; }

        string RowKey { get; set; }

        DateTimeOffset? Timestamp { get; set; }

        string Title { get; set; }

        bool DisplayAsGrid { get; set; }
    }
}