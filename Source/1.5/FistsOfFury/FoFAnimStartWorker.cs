using System;
using AM.Tweaks;
using JetBrains.Annotations;

namespace AM.FoF;

[UsedImplicitly]
public sealed class FoFAnimStartWorker : IAnimStartWorker
{
    /// <summary>
    /// Called just before animations are started.
    /// Used to create custom tweak data for the hands to adjust trail length.
    /// </summary>
    public void OnAnimInit(AnimRenderer animRenderer)
    {
        // Check if the animation is from this mod.
        var animationModSource = animRenderer.Def?.modContentPack;
        if (animationModSource != Core.ModContentPack)
            return;

        const float HAND_RADIUS = 0.08f;
        ReadOnlySpan<string> handNames = ["HandA", "HandB", "HandA2", "HandB2"];
        
        // Try to find the corresponding animation part for the hands.
        int handsFound = 0;
        for (int i = 0; i < handNames.Length; i++)
        {
            string name = handNames[i];
            var part = animRenderer.GetPart(name);
            if (part == null)
                continue;
            
            handsFound++;
            // Modify tweak data to adjust trail length.
            var overrideData = animRenderer.GetOverride(part);
            overrideData.TweakData = new ItemTweakData
            {
                BladeStart = -HAND_RADIUS,
                BladeEnd = HAND_RADIUS,
            };
        }

        if (handsFound == 0)
        {
            Core.Warn($"No hands found in animation {animRenderer}!");
        }
    }
}