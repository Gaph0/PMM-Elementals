# Project Momo Elementals

RimWorld 1.6 mod adding **wild elementals** for Project Momo: spirit-folk born of
earth, fire, wind and water who wander the wild places of the rim. They spawn only in
the wild — like wild men — and like wild men they can be tamed or captured and
convinced to join.

> **Status: early development (phase 0 scaffold).** The plan is locked in
> `Elementals_Plan.md`; defs and C# land in phases 1–2.

## Requirements

- RimWorld **1.6**
- **Biotech** expansion (the elemental xenotypes need it)
- **Odyssey** expansion (the Ignis gate knows the `LavaField` biome)
- **Harmony** (the spawn-gate and wild-man code is a Harmony-patched assembly)
- **Project Momo** (elemental xenotypes carry the Momo gene)

## The four elementals

| Elemental | Element | Appears only where |
|---|---|---|
| **Gnome** | Earth | Maps with caves (overhead mountain) |
| **Ignis** | Fire | Hot lands: Desert, Extreme Desert or Lava Field biomes, or any tile averaging over 30 °C |
| **Sylph** | Wind | Tiles over 1000 m elevation |
| **Undine** | Water | Coastal tiles, river tiles, or the Lake biome |

Each elemental wanders in through her own gated incident (`baseChance` 1.0),
factionless and pinned to her xenotype, and is tamed through the vanilla wild-man
flow (Animals → Tame). All four carry the Momo gene; v1 xenotypes are deliberately
minimal (Momo gene + one skin colour each) — thematic gene kits are a phase-3 option
(see `Elementals_Plan.md` §6).

## Contents (planned — phases 1–2)

| File | What it defines |
|---|---|
| `About/About.xml` | Mod metadata, Harmony + Biotech + Odyssey + Project Momo dependencies |
| `Defs/XenotypeDefs/Xenotype_Elemental.xml` | `PMM_ElementalGnome` / `PMM_ElementalIgnis` / `PMM_ElementalSylph` / `PMM_ElementalUndine` xenotypes |
| `Defs/ThingDefs/Race_ElementalMomo.xml` | The four custom elemental races (Human-derived) |
| `Defs/PawnKindDefs/PawnKinds_Elemental.xml` | Factionless wild pawn kinds, one per element |
| `Defs/BackstoryDefs/Backstories_Elemental.xml` | Shared elemental childhood + per-element adulthoods |
| `Defs/IncidentDefs/Incidents_Elemental.xml` | The four gated wander-in events |
| `Defs/RulePackDefs/RulePacks_Namers_Elemental.xml` | Elemental person name generator |
| `Source/Elementals/ElementalMod.cs` | Harmony entry point, DefOfs |
| `Source/Elementals/ElementalWandersIn.cs` | Wander-in workers + the four spawn gates |
| `Source/Elementals/ElementalPhysiology.cs` | Wild-man, backstory and race-swap patches |
| `Assemblies/PMM_Elementals.dll` | Compiled assembly |

## Build & release

- `./build.sh` — compiles the assembly with `csc` against the local RimWorld +
  Harmony + Project Momo DLLs (auto-builds Project Momo first if needed).
- `./release.sh vX.Y.Z "notes"` — builds, tags, zips the Workshop layout and creates
  a GitHub release on `Gaph0/PMM---Elementals`.

Lore reference: `Wiki/Elementals/` (Monster Girl Encyclopedia wiki snapshots for the
four elementals).
