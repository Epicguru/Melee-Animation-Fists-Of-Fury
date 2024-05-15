using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using UnityEngine;
using Verse;

namespace AM.FoF.Patches;

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldownTicks))]
public static class Patch_VerbProps_AdjustedCooldownTicks
{
    [TweakValue("Fists of Fury", 0, 10), UsedImplicitly]
    private static float cooldownFactor = 0.2f;
    private static readonly Dictionary<Pawn, float> pawnToAdjustment = new Dictionary<Pawn, float>();

    [UsedImplicitly]
    [DebugAction("Fists of Fury", "Adjust pawn melee cooldown", actionType = DebugActionType.ToolMapForPawns)]
    private static void AddPawn(Pawn pawn)
    {
        pawnToAdjustment[pawn] = cooldownFactor;
    }
    
    [UsedImplicitly]
    public static void Postfix(Verb ownerVerb, Pawn attacker, ref int __result)
    {
        if (ownerVerb.IsMeleeAttack && pawnToAdjustment.TryGetValue(attacker, out var adjustment))
        {
            __result = (int)Mathf.Max(1, adjustment * __result);
        }
    }
}