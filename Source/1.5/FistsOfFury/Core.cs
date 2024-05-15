using System;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace AM.FoF;

[HotSwapAll]
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class Core : Mod
{
    internal const string DEBUG_ACTION_CATEGORY = "Melee Animation: Fists of Fury";
    
    internal static void Log(string msg)
    {
        Verse.Log.Message($"<color=cyan>[MA:FoF]</color> {msg}");
    }
    
    internal static void Warn(string msg)
    {
        Verse.Log.Warning($"<color=cyan>[MA:FoF]</color> {msg}");
    }
    
    internal static void Error(string msg, Exception e = null)
    {
        Verse.Log.Error($"<color=cyan>[MA:FoF]</color> {msg}");
        if (e != null)
            Verse.Log.Error(e.ToString());
    }
    
    public Core(ModContentPack content) : base(content)
    {
        Log("Hello, world!");

        try
        {
            new Harmony(content.PackageId).PatchAll();
        }
        catch (Exception e)
        {
            Error($"Exception when Harmony patching one or more methods. Fists of Fury is probably non-functional because of this.", e);
        }
    }
}