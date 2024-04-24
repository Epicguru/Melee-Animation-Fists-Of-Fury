using JetBrains.Annotations;
using Verse;

namespace AM.FoF;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class Core : Mod
{
    internal static void Log(string msg)
    {
        Verse.Log.Message($"<color=cyan>[MA:FoF]</color> {msg}");
    }
    
    public Core(ModContentPack content) : base(content)
    {
        Log("Hello, world!");
    }
}