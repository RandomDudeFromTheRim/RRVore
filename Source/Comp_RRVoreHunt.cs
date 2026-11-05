using RimRound.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace RRVore
{
    public class CompProperties_RRVoreHunt : CompProperties
    {
        public CompProperties_RRVoreHunt()
        {
            compClass = typeof(Comp_RRVoreHunt);
        }
    }

    /// <summary>
    /// Opportunistic swallowing, friendly pawns only: a colonist who actually likes being
    /// big will eat a downed enemy left lying near them, without being told. Raiders and
    /// everyone else are left alone - what an enemy does with a downed colonist is vanilla's
    /// kidnapping business, not this.
    /// </summary>
    public class Comp_RRVoreHunt : ThingComp
    {
        const int CheckInterval = 600;
        const float Radius = 25f;

        public override void CompTick()
        {
            if (!VoreSettings.HuntEnabled)
                return;

            if (!(parent is Pawn pred) || !pred.Spawned || pred.Dead || pred.Downed)
                return;
            if (pred.Drafted || pred.InMentalState || pred.IsPrisoner)
                return;
            if (!pred.IsColonistPlayerControlled)
                return;
            if (!pred.IsHashIntervalTick(CheckInterval))
                return;

            // only someone who enjoys this does it unprompted
            if (Opinion(pred) < WeightOpinion.Like)
                return;

            if (pred.CurJobDef == VoreDefOf.SwallowTarget)
                return;

            Pawn target = FindTarget(pred);
            if (target == null)
                return;

            pred.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VoreDefOf.SwallowTarget, target), JobTag.Misc);
        }

        static WeightOpinion Opinion(Pawn p) =>
            p.TryGetComp<RimRound.Comps.ThingComp_PawnAttitude>()?.weightOpinion ?? WeightOpinion.Neutral;

        static Pawn FindTarget(Pawn pred)
        {
            Map map = pred.Map;
            if (map == null)
                return null;

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p == null || p == pred || p.Dead || !p.Downed || !p.RaceProps.Humanlike)
                    continue;
                // a downed enemy of the colony, not a prisoner, guest or our own
                if (p.Faction == null || !p.Faction.HostileTo(Faction.OfPlayer))
                    continue;
                if (p.Position.DistanceTo(pred.Position) > Radius)
                    continue;
                if (Hediff_RRVoreHold.Refusal(pred, p) != null)
                    continue;
                return p;
            }
            return null;
        }
    }
}
