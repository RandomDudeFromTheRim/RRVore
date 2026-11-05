using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace RRVore
{
    /// <summary>
    /// What the predator is carrying, and a way to let it go. The prey are held inside the
    /// predator (despawned into its container, exactly as RimVore-2 holds prey), so they
    /// cannot be clicked on the map - this is the window onto them instead.
    /// </summary>
    internal class ITab_RRVore : ITab
    {
        public ITab_RRVore()
        {
            labelKey = "RR_VoreTab";
            size = new Vector2(380f, 240f);
        }

        Hediff_RRVoreHold Hold => SelPawn == null ? null : Hediff_RRVoreHold.Of(SelPawn);

        public override bool IsVisible
        {
            get
            {
                Hediff_RRVoreHold hold = Hold;
                return hold != null && hold.Count > 0;
            }
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var list = new Listing_Standard();
            list.Begin(rect);

            Hediff_RRVoreHold hold = Hold;
            if (hold == null || hold.Count == 0)
            {
                list.Label("Nothing inside.");
                list.End();
                return;
            }

            list.Label($"Carrying {hold.Count}, {hold.RemainingKg:0} kg left to digest");
            list.Label($"Room for about {RimRound.Utilities.BurstPoint.KilosRemaining(SelPawn):0} kg more before {SelPawn.LabelShort} bursts");
            list.GapLine();

            foreach (VorePrey v in hold.prey.ToList())
            {
                if (v?.pawn == null)
                    continue;

                Pawn p = v.pawn;
                list.Label($"{p.LabelShort} - {v.remainingKg:0} of {v.massKg:0} kg left");
                if (list.ButtonText($"Spit out {p.LabelShort}"))
                    hold.SpitOut(v);
                list.Gap(6f);
            }

            list.End();
        }
    }
}
