using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RRVore
{
    /// <summary>
    /// RRVore's own settings. Kept separate from RimRound's GlobalSettings because that one
    /// is saved by reflecting over RimRound's own fields and cannot see another assembly's.
    /// </summary>
    public static class VoreSettings
    {
        public static bool Enabled = true;
        public static bool HuntEnabled = true;
        public static bool Struggle = true;

        /// <summary>Share of a swallowed pawn's mass the predator keeps; the rest is lost.</summary>
        public static float DigestEfficiency = 0.7f;
        /// <summary>Share of the meal that lands the moment they go down.</summary>
        public static float BolusFraction = 0.3f;
        /// <summary>In-game hours per kilo transferred.</summary>
        public static float HoursPerKg = 0.15f;

        public const float MinEfficiency = 0f, MaxEfficiency = 1f;
        public const float MinBolus = 0f, MaxBolus = 1f;
        public const float MinHours = 0.01f, MaxHours = 24f;
    }

    /// <summary>Saves RRVore's settings with the game, the same way RimRound saves its own.</summary>
    public class WorldComponent_Vore : WorldComponent
    {
        public WorldComponent_Vore(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref VoreSettings.Enabled, "rrvoreEnabled", true);
            Scribe_Values.Look(ref VoreSettings.HuntEnabled, "rrvoreHunt", true);
            Scribe_Values.Look(ref VoreSettings.Struggle, "rrvoreStruggle", true);
            Scribe_Values.Look(ref VoreSettings.DigestEfficiency, "rrvoreEfficiency", 0.7f);
            Scribe_Values.Look(ref VoreSettings.BolusFraction, "rrvoreBolus", 0.3f);
            Scribe_Values.Look(ref VoreSettings.HoursPerKg, "rrvoreHoursPerKg", 0.15f);
        }
    }
}
