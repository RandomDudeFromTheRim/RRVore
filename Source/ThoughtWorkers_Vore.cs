using RimWorld;
using Verse;

namespace RRVore
{
    /// <summary>Predator: they are carrying someone, and they like it.</summary>
    public class ThoughtWorker_CarryingPrey : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Hediff_RRVoreHold hold = Hediff_RRVoreHold.Of(p);
            return hold != null && hold.Count > 0;
        }
    }

    /// <summary>Prey: they are inside somebody, and they are not enjoying it.</summary>
    public class ThoughtWorker_BeingSwallowed : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p?.ParentHolder is Hediff_RRVoreHold;
        }
    }
}
