# Vore: design notes (fatal slice first)

Status: **design agreed in outline, fatal mode in progress.** This file is the owner
document for vore; the incubate ("slime preds") mode is deliberately out of scope for
this slice and written down at the end so it is not lost.

## Why this exists

RimRound gained a RimVore-2 integration in an earlier pass — six reflection postfixes
that scale RV2's vore rolls, capacity, digestion weight and struggles by RimRound
weight. It only works when RV2 is installed, it sits on constantly-called paths, and it
is being removed (see "RV2, and why it goes"). Vore becomes RimRound's own feature,
which is a better fit: the whole point is that mass moves.

## Prior art in this repo — what to reuse, not rewrite

| Piece | Where | Reuse |
|---|---|---|
| Burst point per pawn (body type + perks + survived stretch) | `RimRound\Utilities\BurstPoint.cs` | The capacity model. Extracted from the constrictor so there is one owner. |
| Load in kg, pump rate, strain past the point | `RimRound\Hediffs\Hediff_RRConstricted.cs` | The whole "how much can this body take" behaviour, already tuned |
| Temporary weight with a duration | `RimRound\Hediffs\Hediff_SuddenWeightGain.cs` | Belly load shows as weight and settles back |
| Struggle free of something holding you | `AI\JobDriver_RRTearOffConstrictor.cs`, `FloatMenuOptionProvider_RRTearOffConstrictor.cs` | Struggle job shape |
| Opportunistic hunter AI | `Comps\Pawns\Comp_RRConstrictorHunt.cs` | Vore hunt comp shape (hostiles, downed, 40 cells) |
| Pawn container across save/load | `RimWorld.CompDevourer` (Anomaly) | The *pattern* only: `IThingHolder` + `ThingOwner` + saved records |
| Slime coat, for later | `SweetSlime\SlimeCoating.cs` | Incubate mode's visual |

Anomaly's `CompDevourer` is the mechanical template but is **not** used directly: it is
single-slot, hard-coded to acid damage and a devourer animation, and its
`StartDigesting` else-branch dereferences a `ConsumeLeap_Devourer` ability a colonist
does not have.

## The model

One rule everywhere: **a pawn bursts on outgrowing `BurstPoint.BodyType(p)`**, and
`BurstPoint.KilosRemaining(p)` is how much more mass they can take. The constrictor
pumps kilos *into* a victim; vore loads kilos *into the predator*. Same limit, same
`GlobalSettings.burstingEnabled` switch, same ghoul/void-touched exemption
(`BurstPoint.CannotBurst`).

## Fatal vore, v1

**Comp.** `Comp_RRVore : ThingComp, IThingHolder` on the predator, holding a
`ThingOwner<Thing>` (the swallowed) plus a scribed `List<VoreRecord>` — one record per
prey: `pawn` (reference), `massKg`, `ticksInside`, `startTick`. Swallow despawns the
prey into the container.

**Capacity.** `Spare = BurstPoint.KilosRemaining(pred)`. Player-issued swallows are
refused above it with a reason; AI swallows are not, and overloading bursts the
predator through the existing strain path.

**While inside.** Predator carries `massKg` as temporary weight (`Hediff_SuddenWeightGain`,
refreshed), so the body sprite grows by exactly what is being carried and settles when
the prey leaves. The prey is despawned, unseen, and cannot act.

**Digest.** Every `ticksPerCheck`, prey mass converts to predator weight at
`digestEfficiency` and the prey's own weight drops by the same amount. Fatal by
default: at the end the prey is destroyed (no corpse) and the remainder goes to the
predator.

**Struggle.** The prey (if player-controlled) gets "Struggle free". Chance scales with
relative mass and melee skill; success regurgitates them alive. Predator may also
"Regurgitate" voluntarily.

**Endings:** digested (prey gone), struggled free, regurgitated, predator died (prey
dumped out, damaged), predator burst (prey dumped out, predator dies).

**Entry points.** Float menu on a downed or willing humanlike → "Swallow"; gizmo on the
predator → "Regurgitate". AI: a hunt comp, opt-in per race via `ModExtension` so nothing
in vanilla starts eating people by itself.

**Settings.** New `GlobalSettings` fields (auto-saved by `WorldComp_SaveValues`
reflection): `voreEnabled`, `voreDigestEfficiency`, `voreDigestHoursPerKg`,
`voreStruggleEnabled`, `voreStruggleSkillFactor`.

**Defaults to start from, all tunable:** digest efficiency 0.7; 2 in-game hours per kg;
struggle chance `0.15 + 0.35 * preyKg/predKg + 0.02 * meleeSkill`, clamped 0.05–0.9.

**Save/load is the risk.** Pawns inside pawns across save/load is where this codebase
has already been burned once (stale constrictor state). Acceptance requires a save and
reload mid-digest.

## Not in this slice

- Incubate mode (slime preds: prey fattens inside, predator fattens, spat out at the
  held-size limit instead of digesting). Same container, different `VoreMode` — the
  container must not preclude it.
- Multi-prey is allowed by the container but not balanced or exposed in v1 beyond
  whatever the capacity permits.
- Apparel handling: dropped on the ground at the swallow point, like
  `NephilaTransformUtility` does. Not carried inside.
- RV2 compatibility of any kind.

## Test plan

1. Dev-spawn a colonist and a prisoner; swallow; confirm the predator's body and weight
   hud change and the prey disappears from the map.
2. **Save, reload, confirm the prey is still inside and the digest continues.**
3. Struggle out; regurgitate; let a digest finish and confirm the predator keeps the mass.
4. Overfill a small predator with a big prey: confirm the burst point is respected and
   `burstingEnabled` off means nobody bursts.
5. Predator killed mid-digest: prey is dumped out.
6. Full log: zero errors, patch count unchanged.

## RV2, and why it goes

The integration is `Comps\Abilities\Comp_RV2Integration.cs` (197 lines, 6 reflection
postfixes), one `PatchAll` call in `HarmonyPatchMain`, a `TryStartRV2OralHold` branch in
`Hediff_VoidEchoVigor`, the SpeakUp patch `1.6\SpeakUp\Patches\RimRound_RimVore2_SpeakUpPatch.xml`,
four `RR_Vored*` thoughts, and two metadata lines. RV2's own direction
(`RV2_RimRound_Integration.dll`) only uses `FullnessAndDietStats_ThingComp.CurrentFullness`,
which stays, so removing RimRound's side breaks nothing on RV2's side.
