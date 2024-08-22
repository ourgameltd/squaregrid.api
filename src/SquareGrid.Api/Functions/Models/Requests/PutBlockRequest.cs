using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Api.Functions.Models.Requests
{
    internal class PutBlockRequest
    {
        [Required]
        public required string Title { get; set; }

        public required string? ClaimedBy { get; set; }

        public required bool Confirmed { get; set; } = false;
    }
}
