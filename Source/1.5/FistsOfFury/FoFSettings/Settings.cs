using UnityEngine;

namespace AM.FoF.FoFSettings;

public class Settings : SimpleSettingsBase
{
    [Header("Visuals")]
    [Label("Character Body Motion Scale")]
    [Description("This determines how much the pawn's body moves during melee fist fighting. 0% is no motion, 100% is full motion.\n" +
                 "This is a purely cosmetic setting.")]
    [Percentage]
    [Range(0f, 1f)]
    public float FightingBodyMotionScale = 1f;
}
