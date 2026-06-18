# AI Disclosure

This document discloses the use of AI coding assistants during the development of this project.

## AI Usage Details

- **Tool Used**: Google Gemini 3.5 Flash / Antigravity AI Coding Assistant.
- **Where in the project**:
  - Assisted in restructuring the original codebase to extract state/scoring/shuffle logic into a decoupled C# model (`MatchGameModel.cs`).
  - Drafted the card state machine design (`CardState`) and continuous input handling logic in `_CardGameManager.cs`.
  - Assisted in setting up assembly definitions (`.asmdef` files) and EditMode unit tests (`GameLogicTests.cs`).
  - Assisted in writing the developer logs and architecture documentation files.
- **Roughly how much**: Approximately 40% of the architecture and code structure was discussed and generated via AI guidance, with manual refinement for Unity API bindings and UI layout adjustments.
