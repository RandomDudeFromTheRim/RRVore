using RimWorld;
using Verse;

namespace RRVore
{
    /// <summary>
    /// RRVore's own defs, looked up lazily rather than through a [DefOf] class: RRVore is a
    /// separate assembly that loads after RimRound, and a stale or unbindable DefOf field is
    /// a silent null at the worst moment.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class VoreDefOf
    {
        public static readonly HediffDef VoreLoaded;
        public static readonly JobDef SwallowTarget;

        static VoreDefOf()
        {
            VoreLoaded = DefDatabase<HediffDef>.GetNamed("RR_VoreLoaded");
            SwallowTarget = DefDatabase<JobDef>.GetNamed("RR_SwallowTarget");
        }
    }
}
