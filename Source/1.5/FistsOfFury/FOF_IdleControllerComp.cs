using System.Collections.Generic;
using System.Linq;
using AM.Idle;
using AM.Reqs;
using AM.Tweaks;
using JetBrains.Annotations;
using Verse;

namespace AM.FoF;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public sealed class FOF_IdleControllerComp : IdleControllerComp
{
    private static AnimDef[][] attackAnimationsCached;
    private static AnimDef[] flavourAnimsCached;
    
    private static IReadOnlyList<AnimDef> GetFistAttackAnimations(Rot4 direction)
    {
        if (attackAnimationsCached != null)
            return attackAnimationsCached[direction.AsInt];

        var reqArgs = new ReqInput { IsFists = true};
        attackAnimationsCached = new AnimDef[4][];

        // Horizontal.
        var hor = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackHorizontal && d.Allows(reqArgs)).ToArray();
        attackAnimationsCached[Rot4.EastInt] = hor;
        attackAnimationsCached[Rot4.WestInt] = hor;

        // Vertical.
        attackAnimationsCached[Rot4.NorthInt] = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackNorth && d.Allows(reqArgs)).ToArray();
        attackAnimationsCached[Rot4.SouthInt] = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackSouth && d.Allows(reqArgs)).ToArray();

        return attackAnimationsCached[direction.AsInt];
    }

    private static IReadOnlyList<AnimDef> GetFistFlavourAnimations()
    {
        var fistsReq = new ReqInput {IsFists = true};
        flavourAnimsCached ??= AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.Flavour && d.Allows(fistsReq)).ToArray();
        return flavourAnimsCached;
    }
    
    private bool isInFistMode;

    public FOF_IdleControllerComp()
    {
        IsFistsOfFuryComp = true;
    }
    
    protected override bool ShouldBeActive(out Thing weapon)
    {
        bool baseWantsToBeActive = base.ShouldBeActive(out weapon);
        if (baseWantsToBeActive)
        {
            isInFistMode = false;
            return true;
        }

        if (!SimpleShouldBeActiveChecks(out var pawn) || !AdditionalShouldBeActiveChecks())
        {
            isInFistMode = false;
            return false;
        }
        
        // Drafted?
        bool fistsActive = pawn.def.race.Humanlike && (pawn.Drafted || pawn.IsInActiveMeleeCombat());
        if (fistsActive)
        {
            isInFistMode = true;
            return true;
        }

        isInFistMode = false;
        return false;
    }

    protected override AnimDef GetMovementAnimation(ItemTweakData tweak, bool horizontal)
    {
        if (!isInFistMode)
        {
            return base.GetMovementAnimation(tweak, horizontal);
        }
        
        return horizontal ? FOF_DefOf.AM_FOF_Idle_MoveHor : FOF_DefOf.AM_FOF_Idle_MoveVert;
    }

    protected override IReadOnlyList<AnimDef> GetFlavourAnimations(ItemTweakData tweakData)
    {
        if (!isInFistMode)
        {
            return base.GetFlavourAnimations(tweakData);
        }

        return GetFistFlavourAnimations();
    }

    protected override void UpdateAttackAnimation()
    {
        if (!isInFistMode)
        {
            base.UpdateAttackAnimation();
            return;
        }
        
        // TODO update here if necessary.
    }

    protected override IReadOnlyList<AnimDef> GetAttackAnimationsFor(Pawn pawn, Thing weapon, out bool allowPauseEver)
    {
        if (!isInFistMode)
        {
            return base.GetAttackAnimationsFor(pawn, weapon, out allowPauseEver);
        }
        
        allowPauseEver = false;
        return GetFistAttackAnimations(pawn.Rotation);
    }

    /// <inheritdoc />
    protected override void EnsureFacingOrIdle(Pawn pawn, ItemTweakData tweak)
    {
        if (!isInFistMode)
        {
            base.EnsureFacingOrIdle(pawn, tweak);
            return;
        }

        var rot = pawn.Rotation;
        bool facingSouth = rot == Rot4.South;
        bool isBusyStance = pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true };
        var targetAnimation = (isBusyStance || !facingSouth) ? rot.IsHorizontal ? FOF_DefOf.AM_FOF_Idle_FightingHor : FOF_DefOf.AM_FOF_Idle_FightingSouth : FOF_DefOf.AM_FOF_Idle_NeutralIdle;
        
        bool startNew = CurrentAnimation is not {IsDestroyed: false} || CurrentAnimation.Def != targetAnimation;
        if (startNew)
        {
            StartAnim(targetAnimation);
        }
    }

    protected override float GetPointAtTargetLerp()
    {
        if (!isInFistMode)
        {
            return base.GetPointAtTargetLerp();
        }

        // Always point at target.
        return 0;
    }
}
