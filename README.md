# Project Momo Elementals

RimWorld 1.6 mod adding **wild elementals** for Project Momo: spirit-folk born of
earth, fire, wind and water who wander the wild places of the rim. They spawn only in
the wild — like wild men — and like wild men they can be tamed or captured and
convinced to join.

Eight elementals ship today. Seven wander in through their own gated event. The genie
is found instead.

## Requirements

- RimWorld **1.6**
- **Biotech** expansion (the elemental xenotypes need it)
- **Odyssey** expansion (required: the Ignis gate knows the `LavaField` biome)
- **Harmony** (the spawn-gate and wild-man code is a Harmony-patched assembly)
- **Big and Small - Framework** (the elemental races use the Big & Small race pattern)
- **Project Momo** (elemental xenotypes carry the Momo gene)

## The eight elementals

| Elemental | Element | Appears only where |
|---|---|---|
| **Gnome** | Earth | Tiles with caves |
| **Ignis** | Fire | Desert, Extreme Desert or Lava Field biomes, or any tile averaging over 30 °C |
| **Sylph** | Wind | Tiles over 1000 m elevation |
| **Undine** | Water | Coastal tiles, river tiles, or the Lake biome |
| **Dorome** | Clay | Tiles with caves, or swamps with 1000 mm or more rainfall |
| **Dryad** | Wood | Forest biomes, while the outdoor temperature sits in the tree growth range |
| **Apsara** | Water | Coastal tiles, river tiles, or the Lake biome |
| **Genie** | — | Found, not wandering: a lamp drops in desert biomes, and cleaning it frees her |

Each wander-in elemental arrives factionless, pinned 100% to her xenotype, and is
tamed through the vanilla wild-man flow (Animals → Tame). Every xenotype carries the
Momo gene, a full thematic gene kit, and her own skin colour gene.

Signature powers: the ignis scorches what stands near her. The gnome heals on soil and
bare rock. The sylph swings between giddy and foul moods. The undine moves faster in
the wet and cleans filth quickly. The dorome merges with the ground. The dryad feeds
on sunlight and works plants. The apsara's dance of love lifts everyone near her.

## Contents

| File | What it defines |
|---|---|
| `About/About.xml` | Mod metadata and dependencies |
| `Defs/XenotypeDefs/Xenotype_Elemental.xml` | The eight elemental xenotypes |
| `Defs/ThingDefs/Race_ElementalMomo.xml` | The eight elemental races (Human-derived) |
| `Defs/ThingDefs/Item_GenieLamp.xml` | The Old Dusty Lamp |
| `Defs/ThingDefs/Filth_Mud.xml` | The dorome's mud filth |
| `Defs/GeneDefs/Genes_ElementalSkin.xml` | The per-element skin colour genes |
| `Defs/PawnKindDefs/PawnKinds_Elemental.xml` | Factionless wild pawn kinds, one per element |
| `Defs/BackstoryDefs/Backstories_Elemental.xml` | Shared elemental childhood + per-element adulthoods |
| `Defs/IncidentDefs/Incidents_Elemental.xml` | Seven gated wander-in events + the lamp event |
| `Defs/AbilityDefs/` | The dorome mud-merge and the genie wish |
| `Defs/HediffDefs/` | Power markers, the apsara's allure, the genie's bond |
| `Defs/ThinkTreeDefs/ThinkTrees_Elemental.xml` | Wild-elemental think-tree branches |
| `Defs/RulePackDefs/RulePacks_Namers_Elemental.xml` | Elemental person name generator |
| `Source/Elementals/` | Harmony entry point, spawn gates, physiology, per-element powers |
| `Assemblies/PMM_Elementals.dll` | Compiled assembly |

## Build & release

- `./build.sh` — runs `dotnet build` on `PMM.Elementals.csproj`. The project reference
  builds Project Momo first.
- `./release.sh vX.Y.Z "notes"` — builds, tags, zips the Workshop layout and creates
  a GitHub release on `Gaph0/PMM---Elementals`.

Lore reference: the Monster Girl Encyclopedia wiki snapshots in the `MGEWiki` folder of
this workspace, outside this repo.
