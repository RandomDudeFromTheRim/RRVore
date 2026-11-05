using HarmonyLib;
using UnityEngine;
using Verse;

namespace RRVore
{
    /// <summary>
    /// Bootstrap for the RimRound vore addon, and its settings page.
    ///
    /// The settings live here rather than being patched into RimRound's settings window:
    /// two mods drawing into one window means either side can break the other, and RRVore
    /// has to keep working against whatever RimRound's window looks like next patch. This
    /// is the standard Mod settings page, so it shows up under Options -> Mod settings.
    /// </summary>
    public class RRVoreMod : Mod
    {
        public RRVoreMod(ModContentPack content) : base(content)
        {
            new Harmony("RandomDudeFromTheRim.RimRound.Vore").PatchAll();
        }

        public override string SettingsCategory() => "RimRound: Vore";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height));

            list.CheckboxLabeled("Vore enabled",
                ref VoreSettings.Enabled,
                "Off: nobody can swallow anyone. Nothing already swallowed is lost.");

            list.CheckboxLabeled("Swallow downed enemies unprompted",
                ref VoreSettings.HuntEnabled,
                "Friendly pawns only, and only those who like being big. Raiders and prisoners are never taken on their own.");

            list.CheckboxLabeled("Prey can struggle free",
                ref VoreSettings.Struggle,
                "Melee DPS against how big the predator is, how each feels about weight, and how close the predator is to bursting.");

            list.GapLine();

            list.Label($"Share of a meal the predator keeps: {VoreSettings.DigestEfficiency:0.00}");
            VoreSettings.DigestEfficiency = list.Slider(VoreSettings.DigestEfficiency,
                VoreSettings.MinEfficiency, VoreSettings.MaxEfficiency);

            list.Label($"Share that lands at once: {VoreSettings.BolusFraction:0.00}");
            VoreSettings.BolusFraction = list.Slider(VoreSettings.BolusFraction,
                VoreSettings.MinBolus, VoreSettings.MaxBolus);

            list.Label($"In-game hours per kilo digested: {VoreSettings.HoursPerKg:0.00}");
            VoreSettings.HoursPerKg = list.Slider(VoreSettings.HoursPerKg,
                VoreSettings.MinHours, 4f);

            list.GapLine();

            list.Label("Swallowed prey become stomach content, and RimRound's own digestion turns that into weight - " +
                       "a bolus when they go down, then a steady trickle.");
            list.Label("A full stomach only empties as fast as the predator digests, which is what sets the real pace.");

            list.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // the values live on the save (WorldComponent_Vore); nudge nothing else
        }
    }
}
