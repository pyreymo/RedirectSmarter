using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace RedirectSmarter
{
    internal class GameHooks : IDisposable
    {
        private Configuration Configuration { get; } = null!;
        private Actions Actions { get; } = null!;
        private static ITargetManager TargetManager => Services.TargetManager;
        private static IToastGui ToastGui => Services.ToastGui;

        private unsafe delegate bool TryActionDelegate(
            IntPtr tp,
            ActionType t,
            uint id,
            ulong target,
            uint param,
            uint origin,
            uint unk,
            Vector3* l
        );
        private readonly Hook<TryActionDelegate> UseActionHook = null!;

        public GameHooks(Configuration config, Actions actions)
        {
            Configuration = config;
            Actions = actions;

            unsafe
            {
                UseActionHook = Services.InteropProvider.HookFromAddress<TryActionDelegate>(
                    (IntPtr)ActionManager.MemberFunctionPointers.UseAction,
                    UseActionCallback
                );
            }

            UseActionHook.Enable();
        }

        private unsafe IGameObject? ResolvePlaceholder(string ph)
        {
            try
            {
                var pm = PronounModule.Instance();
                var p = (IntPtr)pm->ResolvePlaceholder(ph, 0, 0);
                return Services.ObjectTable.CreateObjectReference(p);
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error($"Unable to resolve placeholder ({ph}): {ex.Message}");
                return null;
            }
        }

        public unsafe IGameObject? ResolveTarget(string target)
        {
            return target switch
            {
                RedirectTargets.Self => Services.ObjectTable.LocalPlayer,
                RedirectTargets.Target => TargetManager.Target,
                RedirectTargets.Focus => TargetManager.FocusTarget,
                RedirectTargets.TargetOfTarget => TargetManager.Target is { }
                    ? TargetManager.Target.TargetObject
                    : null,
                RedirectTargets.SoftTarget => TargetManager.SoftTarget,
                RedirectTargets.Chocobo => ResolvePlaceholder("<b>"),
                RedirectTargets.Party2 => ResolvePlaceholder(RedirectTargets.Party2),
                RedirectTargets.Party3 => ResolvePlaceholder(RedirectTargets.Party3),
                RedirectTargets.Party4 => ResolvePlaceholder(RedirectTargets.Party4),
                RedirectTargets.Party5 => ResolvePlaceholder(RedirectTargets.Party5),
                RedirectTargets.Party6 => ResolvePlaceholder(RedirectTargets.Party6),
                RedirectTargets.Party7 => ResolvePlaceholder(RedirectTargets.Party7),
                RedirectTargets.Party8 => ResolvePlaceholder(RedirectTargets.Party8),
                _ => null,
            };
        }

        private unsafe bool UseActionCallback(
            IntPtr actManager,
            ActionType type,
            uint id,
            ulong target,
            uint param,
            uint origin,
            uint unk,
            Vector3* location
        )
        {
            // This is NOT the same classification as the action's ActionCategory
            if (type != ActionType.Action)
            {
                return UseActionHook.Original(
                    actManager,
                    type,
                    id,
                    target,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            // The action row for the originating ID
            var ogRow = Actions.GetRow(id);

            if (ogRow.IsPvP)
            {
                return UseActionHook.Original(
                    actManager,
                    type,
                    id,
                    target,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            // Macro queueing
            // Known origins : 0 - bar, 1 - queue, 2 - macro
            origin = origin == 2 && Configuration.EnableMacroQueueing ? 0 : origin;

            // Actions placed on bars try to use their base action, so we need to get the upgraded version
            var adjustedId = ActionManager.MemberFunctionPointers.GetAdjustedActionId(
                (ActionManager*)actManager,
                id
            );

            // The action id to match against what's stored in the user config
            var configurationId = ogRow.RowId;

            // The actual action that will be used
            var adjustedRow = Actions.GetRow(adjustedId);

            if (!adjustedRow.HasOptionalTargeting())
            {
                return UseActionHook.Original(
                    actManager,
                    type,
                    id,
                    target,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            // Retain queued actions calculated target
            if (origin == 1)
            {
                return UseActionHook.Original(
                    actManager,
                    type,
                    id,
                    target,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            // Only actions where "IsPlayerAction" is true are allowed into the config
            if (adjustedRow.IsPlayerAction)
            {
                configurationId = adjustedRow!.RowId;
            }

            if (Configuration.Redirections.TryGetValue(configurationId, out Redirection? value))
            {
                foreach (var t in value.Priority)
                {
                    IGameObject? nt = ResolveTarget(t);
                    if (nt is not null)
                    {
                        bool rangeOk = adjustedRow.TargetInRangeAndLOS(nt, out var err);
                        bool typeOk = adjustedRow.TargetTypeValid(nt);
                        if (rangeOk && typeOk)
                        {
                            return UseActionHook.Original(
                                actManager,
                                type,
                                id,
                                nt.GameObjectId,
                                param,
                                origin,
                                unk,
                                location
                            );
                        }
                        else if (!Configuration.IgnoreErrors)
                        {
                            switch (err)
                            {
                                case 566:
                                    ToastGui.ShowError("Target not in line of sight.");
                                    break;
                                case 562:
                                    ToastGui.ShowError("Target is not in range.");
                                    break;
                                default:
                                    ToastGui.ShowError("Invalid target.");
                                    break;
                            }
                            return false;
                        }
                    }
                }

                return UseActionHook.Original(
                    actManager,
                    type,
                    id,
                    target,
                    param,
                    origin,
                    unk,
                    location
                );
            }

            return UseActionHook.Original(
                actManager,
                type,
                id,
                target,
                param,
                origin,
                unk,
                location
            );
        }

        public void Dispose()
        {
            UseActionHook?.Dispose();
        }
    }
}
