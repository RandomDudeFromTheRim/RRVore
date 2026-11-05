using RimRound.Utilities;
using HediffUtility = RimRound.Utilities.HediffUtility;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RRVore
{
    /// <summary>One swallowed pawn, and how much of them is left to digest.</summary>
    public class VorePrey : IExposable
    {
        public Pawn pawn;
        /// <summary>What they weighed going in, kilos.</summary>
        public float massKg;
        /// <summary>Kilos not yet transferred to the predator. Reaches 0 and they are gone.</summary>
        public float remainingKg;
        public int ticksInside;
        /// <summary>How far this prey has fought their way out. See RequiredStruggleWork.</summary>
        public float struggleWork;
        /// <summary>Stomach content that came from this prey and has not been digested yet.</summary>
        public float deliveredLitres;

        public float DigestedKg => Mathf.Max(0f, massKg - remainingKg);

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref massKg, "massKg", 0f);
            Scribe_Values.Look(ref remainingKg, "remainingKg", 0f);
            Scribe_Values.Look(ref ticksInside, "ticksInside", 0);
            Scribe_Values.Look(ref struggleWork, "struggleWork", 0f);
            Scribe_Values.Look(ref deliveredLitres, "deliveredLitres", 0f);
        }
    }

    /// <summary>
    /// Someone is carrying swallowed pawns. They are held inside the predator the way a
    /// devourer holds its meal — in a <see cref="ThingOwner"/> on this hediff — so a swallowed
    /// colonist keeps their faction, their name and their place in the colony throughout.
    ///
    /// The prey is worth <see cref="MassOf"/> kilos. That mass is fed into the predator's
    /// stomach a dose at a time, the way a food vat tops up a pawn: it is stomach content,
    /// and RimRound's own digestion turns it into weight. Nothing here calls the sudden
    /// weight gain path, so the curve is the same smooth trickle as being fed from a vat.
    ///
    /// Fatal by default: left alone, the prey ends up fully transferred and ceases to exist.
    /// A prey big enough relative to its predator may struggle free, and the predator can
    /// always let them go. How much can be swallowed at once is RimRound's BurstPoint.
    /// </summary>
    public class Hediff_RRVoreHold : Hediff, IThingHolder
    {
        public List<VorePrey> prey = new List<VorePrey>();

        ThingOwner<Thing> innerContainer;

        const int CheckInterval = 250;
        const int StruggleInterval = 2500;
        const float KilosWhenFinished = 0.05f;
        /// <summary>How full the stomach is kept between mouthfuls, as a share of the soft limit.</summary>
        const float StomachTargetFraction = 0.8f;
        /// <summary>Damage per kilo transferred, spread over the prey's body.</summary>
        const float DamagePerKilo = 0.6f;
        const float StruggleWorkBase = 30f;
        const float StruggleWorkPerDPS = 2f;

        public Hediff_RRVoreHold()
        {
            innerContainer = new ThingOwner<Thing>(this, LookMode.Deep, removeContentsIfDestroyed: false);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        /// <summary>A hediff is not a Thing, so the holder chain stops here at the predator.</summary>
        public IThingHolder ParentHolder => pawn;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public int Count => prey?.Count ?? 0;
        public float RemainingKg => prey == null ? 0f : prey.Sum(p => p.remainingKg);

        public static Hediff_RRVoreHold Of(Pawn p) => p?.health?.hediffSet?.GetFirstHediff<Hediff_RRVoreHold>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "swallowed", this);
            Scribe_Collections.Look(ref prey, "swallowedState", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (innerContainer == null)
                    innerContainer = new ThingOwner<Thing>(this, LookMode.Deep, removeContentsIfDestroyed: false);
                if (prey == null)
                    prey = new List<VorePrey>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit && prey != null)
                prey.RemoveAll(v => v == null || v.pawn == null);
        }

        public override string TipStringExtra =>
            Count == 0 ? null : $"Carrying {Count}: {RemainingKg:0} kg left to digest";

        // ---------------------------------------------------------------- weight of a meal

        /// <summary>
        /// What this pawn is worth as a meal, in kilos. RimRound's own weight when they have
        /// it; a race's own mass comp next (the Nephila keep theirs elsewhere); otherwise the
        /// pawn's actual mass, so nothing is free food.
        /// </summary>
        public static float MassOf(Pawn p)
        {
            if (p == null)
                return 0f;

            Hediff weight = p.WeightHediff();
            float kilos = weight != null ? HediffUtility.SeverityToKilosWithBaseWeight(weight.Severity) : 0f;
            if (kilos > 0.1f)
                return kilos;

            float racial = RacialMass(p);
            if (racial > 0.1f)
                return racial;

            return Mathf.Max(1f, p.GetStatValue(StatDefOf.Mass));
        }

        static System.Reflection.PropertyInfo nephilaKilos;
        static bool nephilaLookedUp;

        /// <summary>Kilos from a race's own mass comp (RimRoundNephila's CompNephilaMass), if it has one.</summary>
        static float RacialMass(Pawn p)
        {
            if (!nephilaLookedUp)
            {
                nephilaLookedUp = true;
                Type compType = GenTypes.GetTypeInAnyAssembly("Nephila.CompNephilaMass");
                nephilaKilos = compType?.GetProperty("Kilos");
            }

            if (nephilaKilos == null || p.AllComps == null)
                return 0f;

            foreach (ThingComp comp in p.AllComps)
                if (nephilaKilos.DeclaringType.IsInstanceOfType(comp))
                    return (float)nephilaKilos.GetValue(comp);

            return 0f;
        }

        // ---------------------------------------------------------------- swallowing

        /// <summary>Why this pawn can't be swallowed right now, or null if they can.</summary>
        public static string Refusal(Pawn pred, Pawn target)
        {
            if (!VoreSettings.Enabled)
                return "vore is switched off in the settings";
            if (pred == null || target == null || pred == target)
                return "not a valid meal";
            if (!pred.RaceProps.Humanlike || !target.RaceProps.Humanlike)
                return "only people can do this";
            if (pred.WeightHediff() == null)
                return $"{pred.LabelShort} does not put on weight";
            if (target.Dead || !target.Spawned)
                return $"{target.LabelShort} is not here";
            if (Of(target) != null)
                return $"{target.LabelShort} is already inside someone";
            if (Of(pred)?.prey.Any(v => v.pawn == target) == true)
                return $"{target.LabelShort} is already inside {pred.LabelShort}";
            if (!target.Downed && !target.IsPrisonerOfColony && target.Awake())
                return $"{target.LabelShort} is awake and free - down them, or make them a prisoner";
            if (!pred.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                return $"{pred.LabelShort} cannot reach {target.LabelShort}";

            float load = MassOf(target) * VoreSettings.DigestEfficiency;
            if (!BurstPoint.CannotBurst(pred) && load > BurstPoint.KilosRemaining(pred))
                return $"{target.LabelShort} is too much for {pred.LabelShort} - they would burst";
            return null;
        }

        public static bool CanSwallow(Pawn pred, Pawn target, out string reason)
        {
            reason = Refusal(pred, target);
            return reason == null;
        }

        /// <summary>Swallow a pawn whole, and start feeding them into the stomach.</summary>
        public static void Swallow(Pawn pred, Pawn target)
        {
            string reason = Refusal(pred, target);
            if (reason != null)
            {
                Messages.Message(reason, new LookTargets(pred), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Hediff hold = pred.health.hediffSet.GetFirstHediff<Hediff_RRVoreHold>()
                          ?? HediffUtility.AddHediffOfDefTo(VoreDefOf.VoreLoaded, pred);
            if (!(hold is Hediff_RRVoreHold comp))
                return;

            target.DeSpawn();
            if (!comp.innerContainer.TryAdd(target))
            {
                GenSpawn.Spawn(target, pred.Position, pred.Map);
                return;
            }

            float kilos = MassOf(target);
            var record = new VorePrey
            {
                pawn = target,
                massKg = kilos,
                remainingKg = kilos,
                ticksInside = 0,
            };
            comp.prey.Add(record);

            // first mouthful now, so the stomach and the bar move immediately
            comp.TransferToStomach(record, float.MaxValue);

            Messages.Message(
                $"{pred.LabelShort} swallows {target.LabelShort} whole.",
                new LookTargets(pred),
                MessageTypeDefOf.NeutralEvent);
        }

        // ---------------------------------------------------------------- digesting

        public override void Tick()
        {
            base.Tick();

            if (Count == 0 || pawn == null || pawn.Dead || !pawn.Spawned)
                return;
            if (!pawn.IsHashIntervalTick(CheckInterval))
                return;

            Digest(CheckInterval);

            if (VoreSettings.Struggle && pawn.IsHashIntervalTick(StruggleInterval))
                TryStruggle();
        }

        /// <summary>
        /// Keep the stomach topped up from what is inside, and let RimRound digest it into
        /// weight on its own. The prey only loses what actually made it into the stomach,
        /// so a slow digester simply keeps them in there longer.
        /// </summary>
        void Digest(int ticks)
        {
            float kgPerTick = 1f / (VoreSettings.HoursPerKg * 2500f);
            float allowance = kgPerTick * ticks;

            for (int i = prey.Count - 1; i >= 0; i--)
            {
                VorePrey v = prey[i];
                if (v?.pawn == null)
                {
                    prey.RemoveAt(i);
                    continue;
                }

                if (v.pawn.Dead || v.pawn.Destroyed)
                {
                    Consume(i);
                    continue;
                }

                float moved = TransferToStomach(v, allowance);
                if (moved > 0f)
                {
                    v.ticksInside += ticks;

                    if (v.pawn.WeightHediff() != null)
                        HediffUtility.AddHediffSeverity(RimRound.Defs.HediffDefOf.RimRound_Weight, v.pawn,
                            -HediffUtility.KilosToSeverityWithoutBaseWeight(moved), false, false);

                    Burn(v.pawn, moved * DamagePerKilo);
                }

                if (v.remainingKg <= KilosWhenFinished)
                    Consume(i);
            }

            if (Count == 0)
                CleanUp();
        }

        /// <summary>
        /// Move up to <paramref name="maxKg"/> of this prey into the predator's stomach, as
        /// much as fits below the target fullness. Returns how many kilos actually went in.
        /// </summary>
        float TransferToStomach(VorePrey v, float maxKg)
        {
            var fnd = pawn.TryGetComp<RimRound.Comps.FullnessAndDietStats_ThingComp>();
            if (fnd == null || v == null)
                return 0f;

            float target = fnd.SoftLimit * StomachTargetFraction;
            float room = target - fnd.CurrentFullness;
            if (room <= 0.01f || maxKg <= 0f)
                return 0f;

            float efficiency = VoreSettings.DigestEfficiency;
            if (efficiency <= 0.0001f)
                return 0f;

            // one litre of fullness becomes about one kilo of weight, and the prey pays for
            // that at the efficiency rate: 70% means they lose ~1.43 kg per litre delivered
            float kgPerLitre = 1f / efficiency;
            float litres = Mathf.Min(room, maxKg / kgPerLitre);
            if (litres <= 0.001f)
                return 0f;

            // exactly what the feeding tube does to a pawn
            fnd.UpdateRatio(litres, fnd.CurrentFullnessToNutritionRatio);
            fnd.CurrentFullness += litres;
            v.deliveredLitres += litres;

            float kg = litres * kgPerLitre;
            v.remainingKg = Mathf.Max(0f, v.remainingKg - kg);
            return kg;
        }

        /// <summary>Stomach acid doing its work on the way through.</summary>
        void Burn(Pawn victim, float damage)
        {
            if (damage <= 0.01f || victim.health?.hediffSet == null)
                return;

            BodyPartRecord part = victim.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.AcidBurn);
            victim.TakeDamage(new DamageInfo(DamageDefOf.AcidBurn, damage, 0f, -1f, pawn, part));
        }

        /// <summary>Nothing left inside: the hediff has no reason to exist.</summary>
        void CleanUp()
        {
            if (pawn != null && !pawn.Dead && pawn.health?.hediffSet?.HasHediff(def) == true)
                pawn.health.RemoveHediff(this);
        }

        /// <summary>
        /// Ticked from the predator's own Tick (see Pawn_Tick_TickContainedPrey), the way
        /// RimVore-2 passes a predator's tick down to its prey: a contained pawn gets no
        /// ticks of their own, so their needs, hediffs and injuries would otherwise freeze.
        /// </summary>
        public void TickContained()
        {
            innerContainer?.DoTick();
        }

        void Consume(int index)
        {
            VorePrey v = prey[index];
            prey.RemoveAt(index);

            Pawn victim = v.pawn;
            if (victim != null)
            {
                innerContainer.Remove(victim);
                victim.Destroy(DestroyMode.Vanish);
            }

            Messages.Message(
                $"{pawn.LabelShort} finishes digesting {(victim == null ? "a meal" : victim.LabelShort)}.",
                new LookTargets(pawn),
                MessageTypeDefOf.NegativeEvent);
        }

        // ---------------------------------------------------------------- struggling

        void TryStruggle()
        {
            for (int i = prey.Count - 1; i >= 0; i--)
            {
                VorePrey v = prey[i];
                if (v?.pawn == null || v.pawn.Dead)
                    continue;

                float required = RequiredStruggleWork(v);
                if (float.IsPositiveInfinity(required))
                    continue;   // they are not trying to leave

                float dps = Mathf.Max(0.1f, v.pawn.GetStatValue(StatDefOf.MeleeDPS));
                v.struggleWork += dps * StruggleWorkPerDPS;

                if (v.struggleWork >= required)
                {
                    Release(i, struggled: true);
                    if (Count == 0)
                        CleanUp();
                    return;
                }
            }
        }

        /// <summary>
        /// How hard this prey has to work to get out, in the same units as
        /// <see cref="VorePrey.struggleWork"/>: how much bigger the predator is, how each
        /// feels about weight, how close the predator is to their burst point, and the one
        /// perk that reads as a grip.
        /// </summary>
        float RequiredStruggleWork(VorePrey v)
        {
            WeightOpinion preyOpinion = Opinion(v.pawn);
            if (preyOpinion >= WeightOpinion.Fanatical)
                return float.PositiveInfinity;   // enjoying it far too much to struggle

            float predKg = Mathf.Max(1f, MassOf(pawn));
            float preyKg = Mathf.Max(1f, v.massKg);
            float sizeFactor = Mathf.Clamp(predKg / preyKg, 0.5f, 4f);

            float work = StruggleWorkBase * sizeFactor * GripFactor(Opinion(pawn)) * FightFactor(preyOpinion);

            float spare = BurstPoint.KilosRemaining(pawn);
            float closeness = v.remainingKg / Mathf.Max(1f, v.remainingKg + spare);
            work *= Mathf.Lerp(1.3f, 0.7f, Mathf.Clamp01(closeness));

            if (PerkLevel(pawn, "RR_InescapableInfinity_Title") > 0)
                work *= 1.5f;

            return work;
        }

        static WeightOpinion Opinion(Pawn p) =>
            p?.TryGetComp<RimRound.Comps.ThingComp_PawnAttitude>()?.weightOpinion ?? WeightOpinion.Neutral;

        static int PerkLevel(Pawn p, string perkName)
        {
            var levels = p?.TryGetComp<RimRound.Comps.FullnessAndDietStats_ThingComp>()?.perkLevels?.PerkToLevels;
            return levels != null && levels.TryGetValue(perkName, out int level) ? level : 0;
        }

        /// <summary>How hard the predator holds on. A weight-lover clamps down.</summary>
        static float GripFactor(WeightOpinion o)
        {
            switch (o)
            {
                case WeightOpinion.Hate: return 0.7f;
                case WeightOpinion.Dislike: return 0.85f;
                case WeightOpinion.NeutralMinus: return 0.95f;
                case WeightOpinion.NeutralPlus: return 1.05f;
                case WeightOpinion.Like: return 1.15f;
                case WeightOpinion.Love: return 1.35f;
                case WeightOpinion.Fanatical: return 1.6f;
                default: return 1f;
            }
        }

        /// <summary>How hard the prey fights. A weight-hater fights like hell.</summary>
        static float FightFactor(WeightOpinion o)
        {
            switch (o)
            {
                case WeightOpinion.Hate: return 0.6f;
                case WeightOpinion.Dislike: return 0.8f;
                case WeightOpinion.NeutralMinus: return 0.9f;
                case WeightOpinion.NeutralPlus: return 1.1f;
                case WeightOpinion.Like: return 1.25f;
                case WeightOpinion.Love: return 1.45f;
                default: return 1f;
            }
        }

        // ---------------------------------------------------------------- letting go

        /// <summary>Spat everyone back out.</summary>
        public void Regurgitate()
        {
            for (int i = prey.Count - 1; i >= 0; i--)
                Release(i, struggled: false);
            if (Count == 0)
                CleanUp();
        }

        /// <summary>Let one particular prey go.</summary>
        public void SpitOut(VorePrey v)
        {
            int index = prey.IndexOf(v);
            if (index < 0)
                return;
            Release(index, struggled: false);
            if (Count == 0)
                CleanUp();
        }

        void Release(int index, bool struggled)
        {
            if (index < 0 || index >= prey.Count)
                return;
            VorePrey v = prey[index];
            prey.RemoveAt(index);

            Pawn victim = v?.pawn;
            if (victim == null)
                return;

            Map map = pawn.MapHeld;
            IntVec3 cell = map == null
                ? IntVec3.Invalid
                : CellFinder.RandomClosewalkCellNear(pawn.PositionHeld, map, 2);

            bool placed = false;
            if (map != null)
                placed = innerContainer.TryDrop(victim, cell, map, ThingPlaceMode.Near, out _);

            if (!placed)
            {
                // nowhere to put them: keep holding rather than destroy them
                innerContainer.TryAdd(victim);
                prey.Insert(index, v);
                return;
            }

            RestoreAfterRelease(victim, v, struggled);
        }

        /// <summary>Put a released pawn back together: whatever they brought back up, their graphics, their stun.</summary>
        void RestoreAfterRelease(Pawn victim, VorePrey v, bool struggled)
        {
            // what is still sloshing in the stomach came back up with them; weight already
            // digested stays where it is
            var fnd = pawn.TryGetComp<RimRound.Comps.FullnessAndDietStats_ThingComp>();
            if (fnd != null && v.deliveredLitres > 0f)
                fnd.CurrentFullness = Mathf.Max(0f, fnd.CurrentFullness - v.deliveredLitres);

            victim.stances?.stunner?.StunFor(300, victim, addBattleLog: false, showMote: true);

            // held pawns come back with a stale render tree: refresh the body and the head
            if (victim.TryGetComp<RimRound.Comps.PawnBodyType_ThingComp>() is RimRound.Comps.PawnBodyType_ThingComp pbt)
                RimRound.Utilities.BodyTypeUtility.UpdatePawnSprite(victim, pbt.PersonallyExempt, pbt.CategoricallyExempt);
            victim.Drawer?.renderer?.SetAllGraphicsDirty();

            Messages.Message(
                struggled
                    ? $"{victim.LabelShort} struggles free of {pawn.LabelShort}."
                    : $"{pawn.LabelShort} spits {victim.LabelShort} back out.",
                new LookTargets(victim),
                MessageTypeDefOf.NeutralEvent);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Regurgitate();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            // removed some other way (surgery, dev tools): nobody gets stranded in there
            if (Count > 0)
                Regurgitate();
        }
    }
}
