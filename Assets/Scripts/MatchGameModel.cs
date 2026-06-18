using System;

public class MatchGameModel
{
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public int TotalCards { get; private set; }
    public int CardsRemaining { get; private set; }
    
    public int Score { get; private set; }
    public int ComboMultiplier { get; private set; }
    public int Moves { get; private set; }
    public float ElapsedTime { get; set; }
    public int Seed { get; private set; }

    public int[] CardSpriteIDs { get; private set; }
    public bool[] CardMatchedStates { get; private set; }

    public MatchGameModel()
    {
        Reset();
    }

    public void Reset()
    {
        Score = 0;
        ComboMultiplier = 1;
        Moves = 0;
        ElapsedTime = 0f;
    }

    public void Initialize(int rows, int columns, int seed, int availableSpritesCount)
    {
        Rows = rows;
        Columns = columns;
        Seed = seed;
        Reset();

        int totalCells = rows * columns;
        int centerIndex = -1;
        if (totalCells % 2 != 0)
        {
            centerIndex = totalCells / 2;
            TotalCards = totalCells - 1;
        }
        else
        {
            TotalCards = totalCells;
        }

        CardsRemaining = TotalCards;
        CardSpriteIDs = new int[totalCells];
        CardMatchedStates = new bool[totalCells];

        // Seeded random shuffle
        System.Random rand = new System.Random(seed);

        // Generate matching pairs
        int pairCount = TotalCards / 2;
        int[] selectedSprites = new int[pairCount];
        for (int i = 0; i < pairCount; i++)
        {
            selectedSprites[i] = rand.Next(0, availableSpritesCount);
        }

        // Initialize sprite IDs with -1 (unassigned)
        for (int i = 0; i < totalCells; i++)
        {
            CardSpriteIDs[i] = -1;
        }

        // Omit center index for odd grids (set to -2 to represent empty slot)
        if (centerIndex != -1)
        {
            CardMatchedStates[centerIndex] = true;
            CardSpriteIDs[centerIndex] = -2;
        }

        // Assign pairs to random empty positions
        for (int i = 0; i < pairCount; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                int attempts = 0;
                int randIndex = rand.Next(0, totalCells);
                while (CardSpriteIDs[randIndex] != -1 && attempts < 1000)
                {
                    randIndex = (randIndex + 1) % totalCells;
                    attempts++;
                }
                CardSpriteIDs[randIndex] = selectedSprites[i];
            }
        }
    }

    public void RestoreState(int rows, int columns, int seed, int score, int multiplier, int moves, float elapsedTime, int[] spriteIDs, bool[] matchedStates)
    {
        Rows = rows;
        Columns = columns;
        Seed = seed;
        Score = score;
        ComboMultiplier = multiplier;
        Moves = moves;
        ElapsedTime = elapsedTime;

        CardSpriteIDs = (int[])spriteIDs.Clone();
        CardMatchedStates = (bool[])matchedStates.Clone();

        int totalCells = rows * columns;
        int centerIndex = totalCells % 2 != 0 ? totalCells / 2 : -1;
        
        if (totalCells % 2 != 0)
        {
            TotalCards = totalCells - 1;
        }
        else
        {
            TotalCards = totalCells;
        }

        // Recompute remaining cards count
        int count = 0;
        for (int i = 0; i < totalCells; i++)
        {
            if (i == centerIndex) continue;
            if (!CardMatchedStates[i])
            {
                count++;
            }
        }
        CardsRemaining = count;
    }

    public bool CheckMatch(int indexA, int indexB, out bool isMatch)
    {
        Moves++;
        
        int spriteA = CardSpriteIDs[indexA];
        int spriteB = CardSpriteIDs[indexB];

        isMatch = (spriteA == spriteB) && (spriteA >= 0);

        if (isMatch)
        {
            CardMatchedStates[indexA] = true;
            CardMatchedStates[indexB] = true;
            CardsRemaining -= 2;

            // Increment score with combo multiplier
            Score += 100 * ComboMultiplier;
            ComboMultiplier++;
            return true;
        }
        else
        {
            // Reset combo multiplier and apply mismatch penalty
            Score = Math.Max(0, Score - 10);
            ComboMultiplier = 1;
            return false;
        }
    }
}