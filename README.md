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
- **Your capacity decides who is too much.** The swallow check compares the potential weight
  gain from the new prey and everyone already inside against RimRound's shared burst limit.
  Ghouls and the void-touched are exempt.
- **Fatal by default.** Prey are digested a bit at a time and eventually cease to exist, and
  what is left of them goes to the predator.
- **Prey can fight their way out.** Awake prey who are on their feet build escape progress
  based on their health and melee DPS. Relative size, weight opinions, and spare burst
  capacity affect how much effort escape takes.
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
| Initial bolus | 30% | Fraction of prey mass transferred at swallowing, up to available stomach room |
| In-game hours per kilo | 0.15 | How fast the prey is transferred |

Larger meals transfer faster and aim for a higher stomach fullness. At extreme prey-to-predator
mass ratios, the target can reach 150% of the hard limit with RimRound bursting enabled. RimRound
then handles strain and rupture. With bursting disabled, the target is capped at the hard limit.
The swallow menu warns about rupture risk, and opportunistic hunting skips meals with that warning.

## Compatibility

- Holds prey the way RimVore-2 does — despawned into the predator's container — so it does
  not need RV2 installed and does not conflict with it by itself.
- Reads the Nephila's own mass comp if RimRound: Nephila is loaded, so they are not free food.
- Expies get an extra burst tier from RimRound itself (fluffy bodies stretch).

## License

Unlicense, unless otherwise specified.
