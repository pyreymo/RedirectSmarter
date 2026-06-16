# Redirect Smarter

Redirect Smarter is a [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV Online.

The plugin lets you set priority-based action targets, place supported ground-targeted actions at the cursor, and queue supported macro or ground actions without clipping the GCD.

![Redirect Smarter preview](preview.png)

### How do I install it?

This plugin is currently available through the Dalamud plugin installer.

### Commands

This plugin has a single command, `/rs`, that opens the configuration.

### Options menu

These options let you control how Redirect Smarter handles target changing:

* `Ignore range and target type errors`: Skips invalid targets instead of stopping the action with an error.
* `Place all ground targets at the cursor`: Instantly places supported ground-targeted actions at the mouse cursor.

These options allow additional things to enter the combat queue, avoiding "clipping" the GCD:

* `Ground targeted actions`: Lets you queue ground actions while casting. This must be used with cursor placement or an explicit target option.
* `Actions from macros`: Prevents GCD clipping from macro actions.

## FAQ

### How do I configure an action redirect?

Open the plugin configuration and select the job you are interested in. Scroll through the action list, or use the search field, to locate the action you wish to modify. If you cannot find the ability, it is not currently supported.

Once you have located the action, click the + button next to it and choose the targets you want to try. The final target is selected by priority from left to right.

### What target options are available?

The following are currently supported options:

* `Cursor`: Places the action at the mouse cursor location.
* `Self`: The player.
* `Target`: Your current target.
* `Focus`: Your current focus target.
* `Target of Target`: Your target's target.
* `Soft Target`: Your current soft target.
* `Chocobo`: Your chocobo companion.
* `<2>` through `<8>`: Party member 2-8.

### Why can I add more than one target option to a single action?

The final target is selected based on a priority system from top to bottom. Once a match is made, that target will be used and anything below it will be ignored. If no match is made, the default target for the action will be attempted.

### Why are lower level versions of spells listed? Can you combine them?

This is primarily due to the way the action bar handles upgrading spells automatically for synced content. While it is technically possible to combine them, there may be situations where this behavior is undesirable and will be left as is for now.

### About macro queueing

This plugin allows you to "queue" actions using macros as you normally would be able to via the action bar. This does not bypass the game's queue system or allow you to queue multiple things at the same time. It does, however, allow you to create priority-based macros or macros that use custom targeting without worrying about clipping.

For example, you can create a Raise macro that will always try to use Swiftcast and then Raise your focus target:

```
/macroicon Raise
/ac Swiftcast
/ac Raise <f>
```

Normally, if you try to use this macro while casting, nothing will happen. With macro queueing enabled, it will try to queue Swiftcast, and if it isn't available, it will try to queue Raise.

Note that if you also have custom action targeting enabled in the configuration, it will override your macro's intended target. However, this system allows you to avoid the configuration step altogether and simply play using normal ingame macros that now work as though they were action bar abilities.

**Notice**: This is not setup to allow you to create one-button macros that will play the game for you, and actually explicitly prevents it. If you use a macro that has multiple actions that can succeed while you are not casting, it will use the first one immediately *and* queue the second one. This is the intended behavior.

### I have a different problem / I want to suggest something!

Please create an issue if one doesn't exist already. Keep in mind that requests aren't guaranteed to be fulfilled.
