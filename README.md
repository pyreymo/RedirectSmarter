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

It redirects supported actions to explicitly configured targets. For each action, you can define a priority list using ordinary game targets such as your current target, focus target, yourself, your chocobo, or party slots.

When the action is used, Redirect Smarter tries those targets from left to right. The first resolved target that passes validation is used. If no configured target succeeds, the action falls back to the game's original target.

The current goal is simple: make action targeting more predictable without turning the plugin into rotation automation.

## Current Scope

Redirect Smarter currently provides:

- Per-action target priority lists.
- Basic target validation for range, line of sight, and target type.
- Optional skipping of invalid configured targets.
- Optional macro action queueing.

Redirect Smarter does not currently provide:

- Mouseover targeting.
- Ground-targeted action placement.
- Automatic rotation logic.
- Smart target selectors such as lowest HP, dispellable status, or enemy-density targeting.

## Installation

This plugin is currently available through the Dalamud plugin installer.

## Command

Use `/rs` to open the configuration window.

## Configuration

The main window lists supported actions by job. Select a job or role actions, find an action, then press the add button to create target priority entries for that action.

Targets are tried from left to right. The first resolved target that passes range, line-of-sight, and target-type checks is used. If no configured target succeeds, the action continues with the game's original target.

The settings window contains:

- `Ignore range and target type errors`: When enabled, invalid configured targets are skipped and the plugin tries the next target in the priority list. When disabled, the first invalid resolved target stops the action and shows an error.
- `Actions from macros`: Allows eligible macro actions to enter the game's normal action queue path more like actions used from the hotbar.

## Target Options

The following explicit target options are currently supported:

- `Target`: Your current target.
- `Focus`: Your current focus target.
- `Target of Target`: Your target's target.
- `Self`: The player.
- `Soft Target`: Your current soft target.
- `Chocobo`: Your chocobo companion.
- `<2>` through `<8>`: Party member 2-8.

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

The current implementation deliberately keeps target resolution simple and explicit. That makes the next layer easier to add: target providers that compute a target dynamically.

Likely future target providers include:

- Lowest-health party member.
- Party member with a dispellable status.
- Party member missing a specific buff.
- Best enemy target by nearby enemy density.
- Best ground position derived from enemy clustering, if ground-targeted action support is reintroduced later.

The important boundary is that these should be target-selection strategies, not action-rotation automation. The plugin should decide where an action goes after the user chooses to use that action.

## Notes For Maintainers

The current code is split around those boundaries:

- `ActionCatalog` builds the list of configurable actions.
- `ActionExtensions` owns action capability and target validation helpers.
- `RedirectTargets` defines the currently supported explicit target names.
- `TargetResolver` maps target names to game objects.
- `GameHooks` owns the action-use hook, macro-origin normalization, and configured target application.
- `Configuration` stores user settings and prunes unsupported target names from older configs.
