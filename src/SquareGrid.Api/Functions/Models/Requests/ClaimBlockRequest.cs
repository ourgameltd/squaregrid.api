using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Api.Functions.Models.Requests
{
    public class ClaimBlockRequest
    {
        [Required]
        public required string? ClaimedBy { get; set; }
    }
}
