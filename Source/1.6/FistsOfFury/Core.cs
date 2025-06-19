using System;
using AM.FoF.FoFSettings;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using UnityEngine;
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
    
    [DebugOutput("Fists Of Fury", onlyWhenPlaying = false), UsedImplicitly]
    public static void OutputTranslationKeys()
    {
        Log(SimpleSettings.GenerateTranslationKeys(Settings));
    }
    
    public static Settings Settings { get; private set; }
    public static ModContentPack ModContentPack { get; private set; }
    
    public Core(ModContentPack content) : base(content)
    {
        Log("Hello, world!");

        ModContentPack = content;
        
        try
        {
            new Harmony(content.PackageId).PatchAll();
        }
        catch (Exception e)
        {
            Error($"Exception when Harmony patching one or more methods. Fists of Fury is probably non-functional because of this.", e);
        }
        
        // Initialize settings.
        Settings = GetSettings<Settings>();
    }

    public override string SettingsCategory() => Content.Name;
    
    public override void DoSettingsWindowContents(Rect inRect)
    {
        SimpleSettings.DrawWindow(Settings, inRect);
    }
}