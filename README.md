<p align="center">
  <img src="./images/icon.png" alt="Redirect Smarter icon" width="128">
</p>

<h1 align="center">Redirect Smarter</h1>

<p align="center">
  Send each action where it should go.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Dalamud%20API-15-blue" alt="Dalamud API 15">
</p>

## Overview

**Redirect Smarter** is a [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV Online.

It redirects supported actions to explicitly configured targets. For each action, you can define a priority list using ordinary game targets such as your current target, focus target, yourself, soft target, party slots, or a dynamic target selector.

When the action is used, Redirect Smarter tries those targets from left to right. The first resolved target that passes validation is used. If no configured target succeeds, the action falls back to the game's original target unless that action has `Prevent default` enabled.

The current goal is simple: make action targeting more predictable without turning the plugin into rotation automation.

## Current Scope

Redirect Smarter currently provides:

- Per-action target priority lists.
- Basic target validation for range, line of sight, and target type.
- Optional skipping of invalid configured targets.
- Per-action `Prevent default` behavior when no configured target is usable.
- A global redirect enable/disable switch.
- A dynamic lowest-HP party member selector.
- Optional macro action queueing.

Redirect Smarter does not currently provide:

- Mouseover targeting.
- Ground-targeted action placement.
- Automatic rotation logic.
- Additional smart target selectors such as dispellable status or enemy-density targeting.

## Installation

This plugin is currently available through the Dalamud plugin installer.

## Command

Use `/rs` to open or close the main window.

Use `/rs toggle` to enable or disable redirects globally. The main window title also shows the current redirect state.

## Configuration

The main window has two tabs:

- `Actions`: Configure per-action redirect behavior.
- `Settings`: Configure global plugin behavior.

The `Actions` tab lists supported actions by job. Select a job or role actions, find an action, then press the add button to create target priority entries for that action.

Targets are tried from left to right. The first resolved target that passes range, line-of-sight, and target-type checks is used. If no configured target succeeds, the action continues with the game's original target unless `Prevent default` is enabled for that action.

Each action row also has a `Prevent default` checkbox. When enabled, the action is blocked if none of its configured redirect targets are available.

The `Settings` tab contains:

- `Enable redirects`: Turns all redirect behavior on or off.
- `Ignore range and target type errors`: When enabled, invalid configured targets are skipped and the plugin tries the next target in the priority list. When disabled, the first invalid resolved target stops the action and shows an error.
- `Actions from macros`: Allows eligible macro actions to enter the game's normal action queue path more like actions used from the hotbar.

## Target Options

The following explicit target options are currently supported:

- `Target`: Your current target.
- `Focus`: Your current focus target.
- `Target of Target`: Your target's target.
- `Self`: The player.
- `Soft Target`: Your current soft target.
- `<2>` through `<8>`: Party member 2-8.
- `Lowest HP Party Member`: The living party member with the lowest HP percentage, only if that member is below the configured HP threshold. If no target qualifies, this selector returns no target unless its self option can select the damaged local player.

## Custom Macro Placeholders

`Lowest HP Party Member` is also registered as the custom placeholder `<lowhp>`.
`<lowhp>` is equivalent to `<lowhp:100>` and only chooses targets below full HP.

The placeholder also supports a small parameter syntax:

- `<lowhp:80>`: Shorthand for `<lowhp:below=80>`.
- `<lowhp:below=80>`: Only chooses targets below 80% HP.
- `<lowhp:80:self=false>`: Only chooses targets below 80% HP and does not target the local player.

Only the first HP threshold may be positional. Use named arguments for anything else; `<lowhp:80:false>` is intentionally not supported.

Custom placeholder support is experimental and only applies to game paths that call the placeholder resolver directly. For action redirection, prefer choosing `Lowest HP Party Member` from the Redirect Smarter target list, where the same threshold and self-target behavior can be configured in the main UI.

## Macro Queueing

Macro queueing only changes how eligible macro actions enter the game's existing action flow. It does not bypass the game's queue system, choose a rotation for you, or execute a full sequence automatically.

For example:

```text
/macroicon Raise
/ac Swiftcast
/ac Raise <f>
```

With macro queueing enabled, actions from a macro can behave more like hotbar actions for queue timing. If an action also has a Redirect Smarter priority list, that configured target list can override the target written in the macro line.

## Development Direction

The current implementation supports both explicit target names and target providers that compute a target dynamically.

Likely future target providers include:

- Party member with a dispellable status.
- Party member missing a specific buff.
- Best enemy target by nearby enemy density.
- Best ground position derived from enemy clustering, if ground-targeted action support is reintroduced later.

The important boundary is that these should be target-selection strategies, not action-rotation automation. The plugin should decide where an action goes after the user chooses to use that action.

## Notes For Maintainers

The current code is split around those boundaries:

- `ActionCatalog` builds the list of configurable actions.
- `ActionExtensions` owns action capability helpers and action allowlist checks.
- `Targeting/Selectors` contains concrete target-selection strategies and the selector interface.
- `Targeting/Parameters` contains the shared parameter schema and runtime selection context.
- `Targeting/MacroPlaceholders` parses custom placeholder arguments into the same parameter shape used by the UI.
- `Targeting/Validation` owns target type, range, and line-of-sight validation.
- `RedirectTargetCatalog` defines available targets, display names, legal persisted target ids, custom macro placeholders, and target-specific parameter schemas.
- `TargetResolver` maps target ids and custom macro placeholders to target selectors, passing target parameters through a shared selection context.
- Target selectors with options should expose parameter schemas with `TargetParameter.Int` / `TargetParameter.Bool`, then read values from `TargetSelectionContext`.
- `ActionRedirector` owns configured target priority application and prevent-default behavior.
- `GameHooks` owns the action-use hook, macro-origin normalization, and original action invocation.
- `MacroPlaceholderHook` owns custom placeholder resolution.
- `Configuration` stores user settings, prunes unsupported target names from older configs, and applies UI redirection edits.
