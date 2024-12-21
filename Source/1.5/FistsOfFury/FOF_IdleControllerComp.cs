using System.Collections.Generic;
using System.Linq;
using AM.Idle;
using AM.Reqs;
using AM.Tweaks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace AM.FoF;

/// <summary>
/// The override version of the idle controller component for Fists of Fury.
/// This additionally handles melee combat with fists and fist-like weapons.
/// </summary>
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
        var horizontalAttacks = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackHorizontal && d.Allows(reqArgs)).ToArray();
        attackAnimationsCached[Rot4.EastInt] = horizontalAttacks;
        attackAnimationsCached[Rot4.WestInt] = horizontalAttacks;

        // Vertical.
        attackAnimationsCached[Rot4.NorthInt] = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackNorth && d.Allows(reqArgs)).ToArray();
        attackAnimationsCached[Rot4.SouthInt] = AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.AttackSouth && d.Allows(reqArgs)).ToArray();

        return attackAnimationsCached[direction.AsInt];
    }

    private static IReadOnlyList<AnimDef> GetFistFlavourAnimations()
    {
        var fistsReq = new ReqInput { IsFists = true };
        flavourAnimsCached ??= AnimDef.GetDefsOfType(AnimType.Idle).Where(d => d.idleType == IdleType.Flavour && d.Allows(fistsReq)).ToArray();
        return flavourAnimsCached;
    }
    
    private bool isInFistMode;
    private float dodgeRotationOffset;
    private float dodgeRotationVelocity;
    private Vector2 dodgePositionOffset;
    private Vector2 dodgePositionVelocity;

    public FOF_IdleControllerComp()
    {
        IsFistsOfFuryComp = true;
    }

    public override void CompTick()
    {
        base.CompTick();

        dodgePositionOffset += dodgePositionVelocity;
        dodgePositionVelocity *= 0.9f;
        dodgePositionOffset *= 0.9f;
        dodgeRotationOffset += dodgeRotationVelocity;
        dodgeRotationVelocity *= 0.9f;
        dodgeRotationOffset *= 0.9f;
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
        return !isInFistMode ? base.GetFlavourAnimations(tweakData) : GetFistFlavourAnimations();
    }

    protected override void UpdateAttackAnimation()
    {
        // No updating is needed in fist mode.
        // The base component does stuff like hit pauses, which aren't supported by fists.
        if (isInFistMode)
            return;
        
        base.UpdateAttackAnimation();
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
        
        bool startNew = CurrentAnimation is not { IsDestroyed: false } || CurrentAnimation.Def != targetAnimation;
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

    public void AddBodyDrawOffset(ref PawnRenderer.PreRenderResults pawnDrawArgs)
    {
        if (!isInFistMode)
            return;

        if (Mathf.Abs(dodgeRotationOffset) > 0.5f || dodgePositionOffset.sqrMagnitude > 0.02f)
        {
            pawnDrawArgs.useCached = false;
            pawnDrawArgs.bodyPos += dodgePositionOffset.ToVector3();
            pawnDrawArgs.bodyAngle += dodgeRotationOffset;
        }

        Core.Log($"Get offset for {parent}:");
        
        // Get the body part.
        // Anim.GetPawnBody can't be used because the pawn is not registered to the animator.
        // Find it manually.
        var bodyA = CurrentAnimation?.GetPart("BodyA");
        if (bodyA == null)
            return;

        // Don't add offset if the idle type is moving.
        if (CurrentAnimation.Def.idleType is (IdleType.MoveHorizontal or IdleType.MoveVertical))
            return;

        ref readonly var bodySnapshot = ref CurrentAnimation.GetSnapshot(bodyA);
        pawnDrawArgs.useCached = true;
        pawnDrawArgs.bodyPos += bodySnapshot.LocalPosition;
        pawnDrawArgs.bodyAngle += bodySnapshot.LocalRotation.y;
        
        // TODO implement.
        //pawnDrawArgs.useCached = true;
        //pawnDrawArgs.bodyPos.x += Mathf.Sin(Time.time * 5f);
    }

    /// <summary>
    /// Called when this pawn dodges a melee attack.
    /// </summary>
    public void OnMeleeDodge(Pawn attackedBy)
    {
        var directionFromAttacker = parent.DrawPos - attackedBy.DrawPos;
        var directionFromAttackerFlat = directionFromAttacker.ToFlat().normalized;
        
        // Add dodge offset.
        dodgePositionVelocity += directionFromAttackerFlat * 0.13f;
        
        bool isToRight = parent.DrawPos.x > attackedBy.DrawPos.x;
        dodgeRotationVelocity += isToRight ? 7f : -7f;
    }
}
