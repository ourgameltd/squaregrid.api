using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Api.Functions.Models.Requests
{
    internal class ClaimBlockRequest
    {
        [Required]
        public required string? ClaimedBy { get; set; }
    }
}
