# Developer Log - Card Match Game

This log outlines key engineering choices, trade-offs, and abandoned approaches during development.

## Key Choices & Design Decisions

### 1. Separation of Concerns (Model-View-Controller)
Instead of coupling core gameplay data (matches, score, multipliers, shuffle sequences) with MonoBehaviours, I extracted all card state and game math into a pure C# model `MatchGameModel`. 
- **Why**: This decoupled logic is completely isolated from Unity's lifecycle, allowing it to be tested in milliseconds using EditMode tests without instantiating GameObjects or Canvas UI elements.

### 2. State Machine for Cards
I replaced boolean flags (`flipped`, `turning`) with an explicit enum state machine on the card component: `FaceDown`, `FlippingUp`, `FaceUp`, `FlippingDown`, `Matched`.
- **Why**: This completely eliminates continuous input glitches. By ignoring click events unless the card state is `FaceDown`, we naturally reject double-taps, mid-flip clicks, clicks on already face-up cards, and clicks on matched cards without complex state checking.

### 3. Dynamic UI Layout Handling
The slider in the menu is configured dynamically in code to represent 8 discrete grid options: `2x2, 2x3, 3x3, 4x3, 4x4, 5x3, 5x4, 5x5`.
- **Why**: It makes use of the existing scene UI component while scaling dynamically. For odd grids, the center cell of the layout is left empty, maintaining grid symmetry while keeping an even number of active cards that can be paired.

### 4. Overlapping Sounds via Spawned AudioSources
Instead of playing all audio clips on a single `AudioSource` on the main manager, I updated `AudioPlayer` to dynamically spawn short-lived AudioSources.
- **Why**: This permits playing overlapping pitch-shifted sound effects (such as a card flipping while a success fanfare or match pop is playing) without cutting off or distorting the other audio clips.

---

## Approaches Tried and Abandoned

### Abandoned: Board Lockout on Mismatch
I initially considered locking input on the entire card grid when two mismatched cards were flipped, waiting for them to flip back down before allowing the next turn.
- **Why Abandoned**: This violates the core requirement for "continuous input". Locking the board feels sluggish and unresponsive. By using a list of unpaired face-up card indices and resolving pairs asynchronously in independent coroutines, the player can flip cards as fast as they want while mismatched pairs wait 0.8 seconds and flip back down in the background.
