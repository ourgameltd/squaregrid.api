using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Api.Models.Requests
{
    internal class PutBlockRequest
    {
        [Required]
        public required string Title { get; set; }

        public required string? ClaimedBy { get; set; }

        public required bool Confirmed { get; set; } = false;
    }
}
