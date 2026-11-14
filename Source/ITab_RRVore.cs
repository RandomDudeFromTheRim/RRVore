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
        const float PreyRowHeight = 54f;
        Vector2 scrollPosition;

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
            Hediff_RRVoreHold hold = Hold;
            if (hold == null || hold.Count == 0)
                return;

            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 26f),
                $"Carrying {hold.Count} ({hold.RemainingKg:0.0} kg left)");
            var viewport = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
            var entries = hold.prey.Where(v => v?.pawn != null).ToList();
            float contentHeight = entries.Count * PreyRowHeight;
            float contentWidth = viewport.width - (contentHeight > viewport.height ? 16f : 0f);
            Widgets.BeginScrollView(viewport, ref scrollPosition, new Rect(0f, 0f, contentWidth, contentHeight));
            for (int i = 0; i < entries.Count; i++)
            {
                VorePrey v = entries[i];
                float y = i * PreyRowHeight;
                float labelWidth = contentWidth - 96f;
                Widgets.Label(new Rect(0f, y, labelWidth, 24f), v.pawn.LabelShortCap.ToString().Truncate(labelWidth));
                Widgets.Label(new Rect(0f, y + 24f, labelWidth, 22f),
                    $"{v.DigestionProgress:P0} digested ({v.remainingKg:0.0} kg left)");
                if (Widgets.ButtonText(new Rect(labelWidth + 8f, y + 5f, 88f, 34f), "Spit out"))
                    hold.SpitOut(v);
            }
            Widgets.EndScrollView();
        }
    }
}
