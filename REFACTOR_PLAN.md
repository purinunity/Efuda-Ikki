# Refactor Plan

Updated: 2026-05-10

## Current Understanding

- `GameManager` owns game startup, stage/CPU setup, special-card loadouts, round flow, showdown resolution, damage application, and cut-in data creation.
- `GameState`/`PlayerState` hold mutable runtime state and card movement state is still stored on `Card` UI objects.
- `HandEvaluator` detects base roles; `SpecialCardResolver` applies special-card effects and resolves final score/winner.
- `HandRoleCatalog` is now the single source for role names, order, and score.
- `CPUController` owns exchange decisions and special-card decisions. Difficulty knobs are now grouped in `CpuDifficultySettings`.
- UI is split between general board UI (`UIManager`, `CardArea`) and the showdown result cut-in (`ShowdownCutInPopup`). `GameManager` now delegates cut-in data building and popup lookup/creation.

## Task List

- [x] Establish current architecture map and hot spots.
- [x] Centralize role score/order data in `HandRoleCatalog`.
- [x] Centralize CPU difficulty knobs in `CpuDifficultySettings`.
- [x] Move CPU character/stage tuning data out of `GameManager`.
- [x] Split `GameManager` responsibilities into smaller helpers.
  - [x] CPU setup and special-card loadout selection.
  - [x] Showdown cut-in data building.
  - [x] Showdown resolution, special-card consumption, damage, and game-over checks.
  - [x] Round initialization and exchange loop orchestration.
- [x] Reduce string-based role branching.
  - [x] Prefer `HandRank`/catalog lookups in CPU exchange decisions and result highlighting.
  - [x] Share card grouping/sequence utilities between evaluator, CPU, and result highlighting.
  - [x] Replace role-name-to-sprite lookup in `ShowdownCutInAssetSet` with a rank-based lookup.
- [x] Harden runtime state boundaries.
  - [x] Route gameplay selection reads/writes through `CardSelectionUtility` where practical.
  - [x] Replace `Console.WriteLine` in Unity runtime code with Unity logging or remove noisy logs.
- [x] Make inspector-facing settings easier to tune.
  - [x] Keep score and difficulty tables in one place.
  - [x] Add clear grouping headers for character-specific CPU settings.
- [x] Verification after each slice.
  - [x] Run `dotnet build Assembly-CSharp.csproj --no-restore`.
  - [x] Check Unity inspector migration risk for serialized fields.

## Current Slice

- [x] Extract CPU character settings from the top of `GameManager` into a dedicated serializable class.
- [x] Keep existing behavior and public serialized shape as close as possible.
- [x] Build after extraction.

## Next Slice

- [x] Add `ShowdownCutInDataBuilder`.
- [x] Delegate showdown cut-in data creation from `GameManager`.
- [x] Remove now-unused cut-in helper methods from `GameManager`.
- [x] Build after delegation.

## Upcoming Slice

- [x] Extract CPU setup and special-card loadout selection from `GameManager`.
- [x] Keep stage startup behavior unchanged.
- [x] Build after extraction.

## Completed Slice: Card Pattern Utilities

- [x] Reduce string-based role branching in CPU exchange decisions.
- [x] Move shared card grouping/sequence helpers into one utility.
- [x] Use the shared helper from `HandEvaluator`, `CPUController`, and `ShowdownCutInDataBuilder`.
- [x] Build after extraction.

## Completed Slice: Role Sprite Lookup

- [x] Replace `ShowdownCutInAssetSet` role-name switch with `HandRank` input.
- [x] Carry base/final/effect-step role ranks through `ShowdownCutInPopup.Data`.
- [x] Build after extraction.

## Completed Slice: Showdown Flow

- [x] Extract showdown resolution and post-cut-in result application into `ShowdownFlowService`.
- [x] Keep cut-in coroutine and end-screen transition inside `GameManager`.
- [x] Build after extraction.

## Completed Slice: Runtime Logging Cleanup

- [x] Replace `Console.WriteLine` in `GameState`/`PlayerState` with Unity warnings or remove noisy logs.
- [x] Build after cleanup.

## Completed Slice: Round Flow

- [x] Extract round initialization and exchange-loop orchestration into `RoundFlowService`.
- [x] Keep UI wait implementation in `GameManager` and inject it as a delegate.
- [x] Build after extraction.

## Completed Slice: Showdown Popup Provider

- [x] Extract showdown popup lookup/creation from `GameManager` into `ShowdownCutInPopupProvider`.
- [x] Keep the serialized popup reference synchronized after provider lookup/creation.
- [x] Build after extraction.

## Completed Slice: Inspector Settings

- [x] Add clearer inspector grouping and tooltips for character-specific CPU settings.
- [x] Build after inspector cleanup.

## Completed Slice: Selection Boundary

- [x] Add `CardSelectionUtility` as the shared boundary for selected-card reads/writes.
- [x] Use it from CPU special-card choice, special-card resolution, showdown cut-in data, and hand-selection reads.
- [x] Build after extraction.

## Remaining Larger Refactor

- [x] Fully separate card runtime model state from UI `Card` MonoBehaviours.
- [x] Build after extraction.
