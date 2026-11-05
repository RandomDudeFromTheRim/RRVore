using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RRVore
{
    /// <summary>Let the player make a pawn spit out whoever it has swallowed.</summary>
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.GetGizmos))]
    internal class Pawn_GetGizmos_AddRRVoreRegurgitate
    {
        static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            Hediff_RRVoreHold hold = Hediff_RRVoreHold.Of(__instance);
            if (hold == null || hold.Count == 0 || !__instance.IsColonistPlayerControlled)
                return;

            var gizmos = __result.ToList();
            gizmos.Add(new Command_Action
            {
                defaultLabel = $"Spit out ({hold.Count})",
                defaultDesc = $"Let go of everyone {__instance.LabelShort} has swallowed.",
                action = hold.Regurgitate,
            });
            __result = gizmos;
        }
    }
}
