using RimWorld;
using Verse;
using Verse.AI;

namespace RRVore
{
    /// <summary>Right-click a downed pawn, a sleeper or a prisoner: "Swallow X whole".</summary>
    public class FloatMenuOptionProvider_RRVore : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn predator = context.FirstSelectedPawn;
            if (clickedPawn == predator || !clickedPawn.RaceProps.Humanlike)
                return null;
            if (!clickedPawn.Downed && !clickedPawn.IsPrisonerOfColony && clickedPawn.Awake())
                return null;

            string label = $"Swallow {clickedPawn.LabelShort} whole";

            string refusal = Hediff_RRVoreHold.Refusal(predator, clickedPawn);
            if (refusal != null)
                return new FloatMenuOption(label + ": " + refusal.CapitalizeFirst(), null);

            if (!predator.CanReserveAndReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);

            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
            {
                Job job = JobMaker.MakeJob(VoreDefOf.SwallowTarget, clickedPawn);
                predator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }, MenuOptionPriority.RescueOrCapture), predator, clickedPawn);
        }
    }
}
