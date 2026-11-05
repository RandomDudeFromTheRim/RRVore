using RRVore;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RRVore
{
    /// <summary>Walk to a helpless pawn and swallow them whole.</summary>
    public class JobDriver_RRSwallow : JobDriver
    {
        const int SwallowTicks = 60;

        Pawn Target => job.GetTarget(TargetIndex.A).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Hediff_RRVoreHold.CanSwallow(pawn, Target, out _));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil swallow = ToilMaker.MakeToil("RRSwallow");
            swallow.defaultDuration = SwallowTicks;
            swallow.defaultCompleteMode = ToilCompleteMode.Delay;
            swallow.handlingFacing = true;
            swallow.AddFinishAction(() => Hediff_RRVoreHold.Swallow(pawn, Target));
            yield return swallow;
        }
    }
}
