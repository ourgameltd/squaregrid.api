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
        /// Contains all blocks for this Game
        /// </summary>
        public List<SquareGridBlock> Blocks { get; private set; } = new List<SquareGridBlock>();

        /// <summary>
        /// Are are blocks now claimed
        /// </summary>
        public bool IsClaimed => Blocks.All(b => b.IsClaimed);

        /// <summary>
        /// Are all blocks claimed and confirmed
        /// </summary>
        public bool IsCompleted => IsClaimed && Blocks.All(b => b.IsConfirmed);

        /// <summary>
        /// Is this game won
        /// </summary>
        public bool IsWon => Blocks.Any(b => b.IsWinner);

        /// <summary>
        /// Won by user id
        /// </summary>
        public Guid? WonById => Blocks.FirstOrDefault(b => b.IsWinner)?.ClaimedByUserId;

        /// <summary>
        /// Won by display name
        /// </summary>
        public string? WonByName => Blocks.FirstOrDefault(b => b.IsWinner)?.ClaimedByFriendlyName;

        /// <summary>
        /// Won by date and time
        /// </summary>
        public DateTime? WonByDate => Blocks.FirstOrDefault(b => b.IsWinner)?.DateClaimed;

        /// <summary>
        /// Set the blocks for this card
        /// </summary>
        /// <param name="blocks"></param>
        public void SetBlocks(IEnumerable<SquareGridBlock>? blocks)
        {
            Blocks = (blocks ?? new List<SquareGridBlock>()).ToList();
        }

        /// <summary>
        /// Set the blocks for this card
        /// </summary>
        /// <param name="blocks"></param>
        public int GetNextAvailableBlockIndex()
        {
            HashSet<int> valuesSet = new HashSet<int>();

            foreach (var block in Blocks)
            {
                valuesSet.Add(block.Index);
            }

            int expectedValue = 1;
            while (true)
            {
                if (!valuesSet.Contains(expectedValue))
                {
                    return expectedValue;
                }
                expectedValue++;
            }
        }

        /// <summary>
        /// Pick a winner for this game
        /// </summary>
        /// <returns></returns>
        public SquareGridBlock? PickAWinner()
        {
            var confirmedBlocks = Blocks.Where(o => o.IsConfirmed).ToList();

            if (confirmedBlocks.Count == 0)
            {
                return null;
            }

            Random random = new Random();

            int randomIndex = random.Next(confirmedBlocks.Count);
            return confirmedBlocks[randomIndex];
        }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
