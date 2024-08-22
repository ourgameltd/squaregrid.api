using Azure;
using System.ComponentModel.DataAnnotations;

namespace SquareGrid.Common.Models
{
    public class Game : IGame
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
        /// The group name for the card
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// The shortname for the card
        /// </summary>
        public string? ShortName { get; set; }

        /// <summary>
        /// A description for the game
        /// </summary>
        [Required]
        public required string Description { get; set; }

        /// <summary>
        /// Contains all blocks for this Game
        /// </summary>
        public List<Block> Blocks { get; private set; } = new List<Block>();

        /// <summary>
        /// The amount of blocks
        /// </summary>
        public int BlockCount => Blocks.Count();

        /// <summary>
        /// Are are blocks now claimed
        /// </summary>
        public bool IsClaimed => Blocks.Count() == 0 ? false : Blocks.All(b => b.IsClaimed);

        /// <summary>
        /// The amount of blocks
        /// </summary>
        public int ClaimedBlockCount => Blocks.Count() == 0 ? 0 : Blocks.Where(i => i.IsClaimed).Count();

        /// <summary>
        /// The amount of blocks
        /// </summary>
        public int PercentageClaimed
        {
            get
            {
                if (ClaimedBlockCount == 0 || BlockCount == 0)
                {
                    return 0;
                }

                int percentage = (int)Math.Round(((double)ClaimedBlockCount / BlockCount) * 100, 1);
                return Math.Min(percentage, 100);
            }
        }

        /// <summary>
        /// Are all blocks claimed and confirmed
        /// </summary>
        public bool IsCompleted => Blocks.Count() == 0 ? false : IsClaimed && Blocks.All(b => b.IsConfirmed);

        /// <summary>
        /// Is this game won
        /// </summary>
        public bool IsWon => Blocks.Count() == 0 ? false : Blocks.Any(b => b.IsWinner);

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
        public void SetBlocks(IEnumerable<Block>? blocks)
        {
            Blocks = (blocks ?? new List<Block>()).ToList();
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
        public Block? PickAWinner()
        {
            var confirmedBlocks = Blocks.Where(o => o.IsClaimed).ToList();

            if (confirmedBlocks.Count == 0)
            {
                return null;
            }

            Random random = new Random();

            int randomIndex = random.Next(confirmedBlocks.Count);
            var block = confirmedBlocks[randomIndex];

            foreach (var b in Blocks)
            {
                b.IsWinner = b.Index == block.Index;
            }

            return block;
        }

        public DateTimeOffset? Timestamp { get; set; }

        public string? ETag { get; set; }

        /// <summary>
        /// Display on UI as a grid
        /// </summary>
        public bool DisplayAsGrid { get; set; }
    }
}
