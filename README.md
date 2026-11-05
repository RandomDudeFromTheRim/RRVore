# RimRound: Vore

A lean vore addon for [RimRound](https://github.com/RandomDudeFromTheRim/RimRound).

Pawns swallow other pawns whole and carry them inside. What they swallow becomes stomach
content, and RimRound's own digestion turns it into weight — a bolus when the meal goes
down, then a steady trickle. No separate weight system, no second digestion clock: the
predator's own gut does the work.

It exists because RimVore-2 already does this, with an enormous amount of apparatus around
it. This is the part RimRound actually wants, and nothing else.

## What it does

- **Swallow anyone helpless** — downed, asleep or a prisoner — from the right-click menu.
  A friendly pawn who likes being big may also do it on their own.
- **Your stomach decides who is too much.** Swallowing somebody bigger than you can hold
  bursts you, using the same limit the gorge constrictor uses. A weight-lover can take more;
  ghouls and the void-touched can take anything.
- **Fatal by default.** Prey are digested a bit at a time and eventually cease to exist, and
  what is left of them goes to the predator.
- **Prey can fight their way out.** Their melee DPS against how big the predator is, how each
  of them feels about weight, and how close the predator is to bursting. A weight-hater
  fights like hell; a fanatic would rather not leave.
- **The "Swallowed" tab** on a predator lists who is inside, how much of them is left, and
  lets you spit them back out.
- **They stay people while they are inside.** Prey keep their faction, name and place in the
  colony, keep ticking (needs, wounds, hediffs), and survive a save and reload mid-digest.

## Requirements

- **RimRound** (and what RimRound needs)
- Harmony

Optional: Biotech (weight-opinion genes), Anomaly (ghouls and the void-touched can't be
burst).

## Installation

Drop the folder into RimWorld's `Mods` directory, or subscribe on the Workshop. It must load
**after** RimRound. Enable it in the mod list; it does nothing until it is ticked.

## Settings

Options → Mod settings → **RimRound: Vore**.

| Setting | Default | What it does |
|---|---|---|
| Vore enabled | on | Off: nobody swallows anyone; nothing already swallowed is lost |
| Swallow downed enemies unprompted | on | Friendly pawns who like being big eat downed raiders on their own |
| Prey can struggle free | on | Turn the escape attempts off for a purely one-way trip |
| Share of a meal the predator keeps | 0.70 | The rest is lost as digestion |
| Share that lands at once | 0.30 | The bolus; the remainder trickles in |
| In-game hours per kilo | 0.15 | How fast the prey is transferred |

A note on the last one: the stomach only holds so much, and only empties as fast as the
predator digests. Once it is full, the pace is set by their digestion, not by this number.

## Compatibility

- Holds prey the way RimVore-2 does — despawned into the predator's container — so it does
  not need RV2 installed and does not conflict with it by itself.
- Reads the Nephila's own mass comp if RimRound: Nephila is loaded, so they are not free food.
- Expies get an extra burst tier from RimRound itself (fluffy bodies stretch).

## License

Unlicense, unless otherwise specified.
