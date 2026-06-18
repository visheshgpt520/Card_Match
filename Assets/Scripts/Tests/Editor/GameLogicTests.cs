using NUnit.Framework;
using UnityEngine;

public class GameLogicTests
{
    [Test]
    public void TestDeterministicShuffle()
    {
        MatchGameModel model1 = new MatchGameModel();
        model1.Initialize(4, 4, 12345, 10);

        MatchGameModel model2 = new MatchGameModel();
        model2.Initialize(4, 4, 12345, 10);

        // Same seed must produce identical card layouts
        CollectionAssert.AreEqual(model1.CardSpriteIDs, model2.CardSpriteIDs, "Same seed must produce identical layouts");

        MatchGameModel model3 = new MatchGameModel();
        model3.Initialize(4, 4, 54321, 10);

        // Different seeds should produce different card layouts
        CollectionAssert.AreNotEqual(model1.CardSpriteIDs, model3.CardSpriteIDs, "Different seeds should produce different layouts");
    }

    [Test]
    public void TestMatchScoringAndCombo()
    {
        MatchGameModel model = new MatchGameModel();
        
        // Setup initial board state manually for testing
        int[] spriteIDs = { 0, 0, 1, 1 };
        bool[] matched = { false, false, false, false };
        model.RestoreState(2, 2, 12345, 0, 1, 0, 0f, spriteIDs, matched);

        Assert.AreEqual(0, model.Score);
        Assert.AreEqual(1, model.ComboMultiplier);

        // Match 1
        bool isMatch;
        model.CheckMatch(0, 1, out isMatch);
        Assert.IsTrue(isMatch);
        Assert.AreEqual(100, model.Score); // 100 * 1
        Assert.AreEqual(2, model.ComboMultiplier); // Combo increases to 2
        Assert.AreEqual(2, model.CardsRemaining);

        // Match 2 (with x2 combo multiplier)
        model.CheckMatch(2, 3, out isMatch);
        Assert.IsTrue(isMatch);
        Assert.AreEqual(300, model.Score); // 100 + (100 * 2) = 300
        Assert.AreEqual(3, model.ComboMultiplier); // Combo increases to 3
        Assert.AreEqual(0, model.CardsRemaining);
    }

    [Test]
    public void TestMismatchResetsComboAndAppliesPenalty()
    {
        MatchGameModel model = new MatchGameModel();
        
        int[] spriteIDs = { 0, 1, 0, 1 };
        bool[] matched = { false, false, false, false };
        model.RestoreState(2, 2, 12345, 100, 3, 0, 0f, spriteIDs, matched);

        Assert.AreEqual(100, model.Score);
        Assert.AreEqual(3, model.ComboMultiplier);

        // Click non-matching cards
        bool isMatch;
        model.CheckMatch(0, 1, out isMatch);
        Assert.IsFalse(isMatch);
        Assert.AreEqual(90, model.Score); // 100 - 10 penalty = 90
        Assert.AreEqual(1, model.ComboMultiplier); // Combo resets to 1
    }

    [Test]
    public void TestOddGridOmitCenter()
    {
        MatchGameModel model = new MatchGameModel();
        model.Initialize(3, 3, 12345, 10);

        // A 3x3 layout has 9 cells, but with center omitted, we must have 8 active cards
        Assert.AreEqual(8, model.TotalCards);
        Assert.AreEqual(8, model.CardsRemaining);
        Assert.AreEqual(-2, model.CardSpriteIDs[4], "Center card (index 4) should be omitted (-2)");
        Assert.IsTrue(model.CardMatchedStates[4], "Center card should be pre-marked as completed/matched");
    }
}
