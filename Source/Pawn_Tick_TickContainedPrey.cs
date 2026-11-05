using HarmonyLib;
using Verse;

namespace RRVore
{
    /// <summary>
    /// A swallowed pawn is despawned, so the map never ticks them: their needs, hediffs and
    /// wounds would freeze until they were let out. Pass the predator's tick down to
    /// everything they are carrying, the way RimVore-2 does.
    /// </summary>
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("Tick")]
    internal class Pawn_Tick_TickContainedPrey
    {
        static void Postfix(Pawn __instance)
        {
            Hediff_RRVoreHold hold = Hediff_RRVoreHold.Of(__instance);
            if (hold == null || hold.Count == 0)
                return;
            hold.TickContained();
        }
    }
}
