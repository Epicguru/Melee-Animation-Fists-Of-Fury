using System;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AM.FoF.Patches;

[HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.CreateCombatLog))]
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public static class Patch_Verb_MeleeAttack_CreateCombatLog
{
    [HarmonyPriority(Priority.First)]
    public static void Prefix(Verb_MeleeAttack __instance, Func<ManeuverDef, RulePackDef> rulePackGetter)
    {
        if (__instance.maneuver == null || rulePackGetter == null)
            return;

        if (__instance.CurrentTarget.Thing is not Pawn targetPawn)
            return;
        
        var packDef = rulePackGetter(__instance.maneuver);
        if (packDef != __instance.maneuver.combatLogRulesDodge)
            return;
        
        var meleeComp = targetPawn.TryGetComp<FOF_IdleControllerComp>();
        var attacker = __instance.CasterPawn;
        meleeComp?.OnMeleeDodge(attacker);
    }
}
