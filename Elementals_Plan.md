# Project Momo Elementals — Implementation Plan

> **Status: LOCKED (2026-08-30).** All decisions D1–D8 signed off — see §8. Phase 0
> scaffold done (About, build/release scripts, README, CHANGELOG, git repo). Phases
> 1–2 (defs, C#) remain.

Third mod in the Project Momo family (after `PMM.Core` and `PMM.SlimeFaction`). Adds the
four MGE elementals — **Gnome, Ignis, Sylph, Undine** — as wild momos who wander onto
the map exactly like wild slimes do, each gated to her own home terrain:

| Elemental | Element | Spawn gate (from audit brief) |
|---|---|---|
| Gnome | Earth | Only on maps **with caves** (overhead mountain) |
| Ignis | Fire | Only on **hot maps**: Desert, Extreme Desert, Lava Fields biomes, or any tile with average temperature **> 30 °C** |
| Sylph | Wind | Only on tiles with **elevation > 1000 m** |
| Undine | Water | Only on tiles **with water** (see **D1**) |

Foundation: the proven slime-faction pattern — `IncidentWorker_SlimeWandersIn` and its
bubble/taisui/sea subclasses (`Source/SlimeFaction/SlimeWandersIn.cs`), the wild-man
patch trio in `SlimePhysiology.cs`, and the xenotype/pawn-kind/backstory def layout.
**The elementals mod must NOT depend on the slime mod** — it copies the pattern, it does
not reference the DLL. Only dependency chain: Harmony → Biotech → Odyssey → PMM.Core.

Lore source: `Wiki/Elementals/*.html` (MGE wiki snapshots). Key lore → mechanics:

| Elemental | Wiki lore highlights | Mechanical translation |
|---|---|---|
| Gnome | Caves/mines; calm, gentle, quiet; voluptuous body "like the earth"; slow, long embraces | Cave gate; (tough/mining gene kit → phase 3) |
| Ignis | Volcanic/desert regions; strong-willed, passionate; body covered in flames that only burn when she wills it; violent like a raging flame | Heat gate; (fire gene kit + flame aura → phase 3) |
| Sylph | Flies hidden in the sky; capricious, free-spirited; collects rumors on the wind; curious | Elevation gate; (speed/cold gene kit → phase 3) |
| Undine | Lakes/springs; pure, kind, gentle; translucent water body; devoted to her covenanter | Water-tile gate; `Skin_SheerWhite`; fast in water (pawn-kind terrain factor) |

All four are "monsterized elementals" seeking a **covenanter** — in Project Momo terms
that is the existing Tsugai/bond system in `PMM.Core`; being Momo-gene carriers they are
bond-capable out of the box (**D6**). No new relationship system.

---

## 1. Requirements → mechanics mapping

| # | Requirement | Mechanism |
|---|---|---|
| 1 | Four new elementals spawn as wild momos | Four `XenotypeDef`s + four wild `PawnKindDef`s + four wander-in incidents (slime pattern) |
| 2 | Gnome: maps with caves only | `IncidentWorker_GnomeWandersIn.ClimateAcceptable` → `HasCaves(map)` roof-grid sample, copied verbatim from the slime mod (stride 8, ≥8 thick-roof hits) |
| 3 | Ignis: hot maps only | `IncidentWorker_IgnisWandersIn`: `biome.defName ∈ {Desert, ExtremeDesert, LavaField}` **OR** `GenTemperature.AverageTemperatureAtTile(map.Tile) > 30f` |
| 4 | Sylph: elevation > 1000 m | `IncidentWorker_SylphWandersIn`: `Find.WorldGrid[map.Tile].elevation > 1000f` |
| 5 | Undine: tiles with water | `IncidentWorker_UndineWandersIn`: `tile.IsCoastal || tile.Rivers?.Count > 0 || biome Lake` (**D1**) |
| 6 | Tameable/joinable like wild slimes | Wild-man patch trio keyed on the four elemental kinds (`IsWildMan`, `WildManShouldReachOutsideNow`, backstory pin) |
| 7 | They ARE momos | `ProjectMomo_Momo` gene in every xenotype (mana need, tease melee, bond-capable — all inherited from PMM.Core) |
| 8 | New standalone mod folder | Scaffold mirroring the slime faction layout (§2) |

**Critical XML detail (learned from the slime incidents):** none of the four
`IncidentDef`s may set `<allowedBiomes>`. That storyteller-level gate would hard-block
the temperature clause for Ignis (a 33 °C TropicalRainforest tile must still be
eligible) and the elevation/river clauses for Sylph/Undine. All gating lives in C#
`ClimateAcceptable`, like the bubble/taisui/sea slimes.

---

## 2. Project scaffold (Phase 0)

Mirror `Project Momo Slime Faction/` exactly:

```
Project Momo Elementals/
	Elementals_Plan.md          ← this file
	README.md                   ← player-facing, cloned style from slime README
	CHANGELOG.md                ← start at 0.1.0
	build.sh                    ← slime build.sh, names swapped (see below)
	release.sh                  ← slime release.sh; REPO name per D8
	About/
		About.xml                 ← PMM.Elementals (§3)
		Manifest.xml              ← copy slime pattern
		PublishedFileId.txt       ← empty until first Workshop upload
	Assemblies/                 ← build output (PMM_Elementals.dll)
	Defs/
		XenotypeDefs/Xenotype_Elemental.xml
		PawnKindDefs/PawnKinds_Elemental.xml
		ThingDefs/Race_ElementalMomo.xml      ← D2: custom races
		BackstoryDefs/Backstories_Elemental.xml
		IncidentDefs/Incidents_Elemental.xml
		RulePackDefs/RulePacks_Namers_Elemental.xml
	Languages/English/Keyed/    ← only if a letter needs keyed strings (slime pattern:
	                              letters inline in the IncidentDef; skip otherwise)
	Source/Elementals/
		ElementalMod.cs           ← Harmony entry + ElementalDefOf
		ElementalWandersIn.cs     ← base worker + 4 gated subclasses
		ElementalPhysiology.cs    ← ElementalKinds + wild-man patch trio
	Textures/                   ← phase 4 (custom xenotype icons; placeholders until then)
	Wiki/Elementals/            ← already present (lore source)
```

**Naming (locked, matching family conventions):**
- packageId `PMM.Elementals`; assembly `PMM_Elementals.dll`; namespace `PMM_Elementals`;
  Harmony id `PMM.Elementals`; source dir `Source/Elementals/`.
- `build.sh`: the slime one with `Source/SlimeFaction/*.cs` → `Source/Elementals/*.cs`
  and `-out:Assemblies/PMM_Elementals.dll`. Keep the ProjectMomo.dll auto-build and
  reference. VEF / IsekaiLeveling references dropped — v1 has no psycast or leveling
  integration; re-add from the slime build.sh if phase 3 needs them. No-sources guard:
  exits cleanly with a message until phase 2 adds `.cs` files.
- `release.sh`: identical, `MOD="PMM-Elementals"`, `REPO` per **D8**.

---

## 3. `About/About.xml`

Copy the slime faction's and change: name → `Project Momo Elementals`, packageId →
`PMM.Elementals`, description draft:

> Adds wild elementals for Project Momo: spirit-folk born of earth, fire, wind and
> water who wander the wild places of the rim.
>
> Elementals only appear in the wild, wandering onto your map like wild men — and like
> wild men they can be tamed or captured and convinced to join. Each seeks out her own
> element: gnomes surface from caves, ignis walk out of scorching deserts and lava
> fields, sylphs ride the mountain winds, and undines rise where rivers meet the sea.
>
> Requires the Biotech and Odyssey expansions, Harmony, and Project Momo.

Same `modDependencies` and `loadAfter` lists as the slime faction (Harmony, Biotech,
Odyssey, PMM.Core; `loadAfter` additionally lists `VanillaExpanded.VPsycastsE` — keep
for family parity).

---

## 4. Defs (Phase 1)

### 4.1 `Defs/XenotypeDefs/Xenotype_Elemental.xml`

Four xenotypes. Shared block: `<inheritable>true</inheritable>`,
`<factionlessGenerationWeight>0</factionlessGenerationWeight>` (never appear as
refugees/wanderers — incident-only, like slimes), shared name maker
(`NamerPersonElemental`, §4.4), `ProjectMomo_Momo` in every gene list.
Icons are vanilla placeholders (phase 4 replaces them, slime `Impid` precedent).

**Locked gene ruling (D4/D5):** v1 xenotypes carry `ProjectMomo_Momo` + one custom
`PMM_Skin_*` gene each (`Defs/GeneDefs/Genes_ElementalSkin.xml`): Gnome
`PMM_Skin_GnomeBrown` (dirt browns), Ignis `PMM_Skin_IgnisOrange` (flame oranges),
Sylph `PMM_Skin_SylphGreen` (light greens), Undine `PMM_Skin_UndineBlue` (watery
blues). Each is parented on Biotech's `GeneSkinColorOverride`, whose
`randomBrightnessFactor 0.18` yields varied shades per pawn. All defNames verified
against Biotech 1.6.

**AMENDED (2026-08-30, full thematic kits added per user spec):** on top of the skin +
Momo base, each xenotype now carries a full elemental kit. `MinTemp_SmallIncrease` =
cold weakness (raises comfy-temp minimum); `CaveDweller` = the "indoor dweller"
gnome; mood via the `Mood_*` spectrum; aptitudes via the auto-generated
`Aptitude{Level}_{Skill}` template genes (Terrible −8/Poor −4/Strong +4/Remarkable
+8, per-skill exclusion so different skills stack). All defNames verified vs Biotech 1.6.

| Elemental | Kit (beyond skin + Momo) |
|---|---|
| Gnome | `Aggression_DeadCalm`, `MoveSpeed_Slow`, `VerySleepy`, `CaveDweller`, `AptitudeTerrible_Cooking`, `AptitudeTerrible_Medicine`, `AptitudeRemarkable_Plants`, `AptitudeStrong_Construction`, `Robust`, `Nearsighted`, `AptitudeTerrible_Artistic` |
| Ignis | `NakedSpeed`, `FireResistant`, `AptitudeRemarkable_Cooking`, `AptitudeTerrible_Plants`, `AptitudeTerrible_Animals`, `MinTemp_SmallIncrease`, `Mood_Pessimist`, `MoveSpeed_Quick` |
| Sylph | `Mood_Sanguine`, `KindInstinct`, `LowSleep`, `Delicate`, `Beauty_Pretty`, `Learning_Fast`, `AptitudeTerrible_Construction`, `AptitudeTerrible_Mining`, `AptitudeStrong_Social`, `AptitudeRemarkable_Artistic` |
| Undine | `Mood_Sanguine`, `KindInstinct`, `AptitudeStrong_Plants`, `AptitudeStrong_Medicine`, `AptitudePoor_Mining`, `Nearsighted`, `MinTemp_SmallIncrease`, `MoveSpeed_Slow` |

Only if play-testing shows a gap, the remaining phase-3 options are: Ignis flame aura
(race comp), Gnome living-stone healing, Sylph caprice mood hediff, and elemental
butcher drops (`specificMeatDef` motes/cores). ~~Undine devotion/covenant~~ — cut per
locked D6.

**`PMM_ElementalGnome`** — earth. Label `gnome`. `combatPowerFactor 1.5`.
Icon placeholder `UI/Icons/Xenotypes/Dirtmole`.
Description: quiet, carefree cave-dweller with a full, earth-like body; seeks a man to
root herself to. Genes (locked minimal):
```
Skin_SlateGray   (rock; D3)
ProjectMomo_Momo
```

**`PMM_ElementalIgnis`** — fire. Label `ignis`. `combatPowerFactor 1.8`.
Icon placeholder `UI/Icons/Xenotypes/Impid`.
Description: passionate, strong-willed flame given a woman's form, wandering the hot
lands with lust smoldering within. Genes (locked minimal; fire kit → phase 3):
```
Skin_Orange   (flame; D3)
ProjectMomo_Momo
```

**`PMM_ElementalSylph`** — wind. Label `sylph`. `combatPowerFactor 1.2` (flighty, not a
fighter). Icon placeholder `UI/Icons/Xenotypes/Genie`.
Description: capricious, free-spirited wind elemental, ever curious about the love
between men and women. Genes (locked minimal; wind kit → phase 3):
```
Skin_LightGray   (wind-pale; D3)
ProjectMomo_Momo
```

**`PMM_ElementalUndine`** — water. Label `undine`. `combatPowerFactor 1.4`.
Icon placeholder `UI/Icons/Xenotypes/Highmate`.
Description: pure-hearted, gentle water elemental with a translucent body, dwelling by
lakes and springs, dreaming of a covenanter. Genes (locked minimal):
```
Skin_SheerWhite   (translucent water body — literally sheer; D3)
ProjectMomo_Momo
```
Her water speed lives on the **pawn kind**, not the genes (sea-slime precedent, §4.2).

### 4.2 `Defs/PawnKindDefs/PawnKinds_Elemental.xml`

Four kinds — `PMM_GnomeWild`, `PMM_IgnisWild`, `PMM_SylphWild`, `PMM_UndineWild` —
each a copy of the `PMM_SlimeWild` block with these deltas:
- `<race>` the matching custom race (`PMM_Race_GnomeMomo` / `PMM_Race_IgnisMomo` /
  `PMM_Race_SylphMomo` / `PMM_Race_UndineMomo`) — **D2 locked: custom races** (§4.2b).
- `<fixedGender>Female</fixedGender>` — elementals are always women (wiki lore; the
  Momo gene enforces it anyway, but pin it like the nureonago for clean generation).
- No weapons/apparel (same "gear generation only runs when tags are set" reasoning),
  `chemicalAddictionChance 0`, `apparelIgnorePollution true`, `combatPower 45`,
  `initialWillRange 0~4`, `initialResistanceRange 4~17`, `maxGenerationAge 60`.
- `backstoryFilters` per kind: `<categories><li>PMM_ElementalSpawn</li><li>PMM_GnomeSpawn</li></categories>`
  (etc.) — shared childhood category + per-element adulthood category, the exact
  sea-slime pattern (`PMM_SlimeSpawn` + `PMM_SlimeSeaSpawn`).
- `xenotypeSet` pinned: `<PMM_ElementalGnome>100</PMM_ElementalGnome>` (etc.). No colour
  mix — unlike slimes, each incident spawns exactly one element.
- `PMM_UndineWild` additionally gets the sea-slime water-speed block:
  `<moveSpeedFactorByTerrainTag><li><key>Water</key><value>2.5</value></li></moveSpeedFactorByTerrainTag>`.

### 4.2b `Defs/ThingDefs/Race_ElementalMomo.xml` (D2 locked: custom races)

Abstract `PMM_ElementalMomoRaceBase` (`ParentName="Human"`, deep merge of body/tools/
think tree/stats) + four concrete races: `PMM_Race_GnomeMomo`, `PMM_Race_IgnisMomo`,
`PMM_Race_SylphMomo`, `PMM_Race_UndineMomo` (slime `PMM_SlimeMomoRaceBase` pattern).
v1 keeps **human butcher yields** — no `specificMeatDef`, leather untouched — so no
C# race setup is needed (the slime `NullLeather`/`specificMeatDef` dance is
jelly-specific). The races exist for identity (race label on the info card), future
comps (phase-3 aura/stone-heal hooks) and slime-family parity. Elemental-flavoured
butcher drops ("motes/cores") stay a phase-3 option.

### 4.3 `Defs/BackstoryDefs/Backstories_Elemental.xml`

Mirror `Backstories_Slime.xml`: one shared childhood `PMM_ElementalChild`
("Born of the [element]…" — spirit-origin flavor, no skill gains, category
`PMM_ElementalSpawn`) and four adulthoods, each in its own category
(`PMM_GnomeSpawn`, `PMM_IgnisSpawn`, `PMM_SylphSpawn`, `PMM_UndineSpawn`) so the
physiology backstory patch can pin per-kind (§5.3):
- `PMM_GnomeAdult` — earth-tender; +Mining, +Plants (gardens stone and soil).
- `PMM_IgnisAdult` — flame-keeper; +Melee, +Cooking (fire's two faces).
- `PMM_SylphAdult` — sky-wanderer; +Intellectual, +Social (a collector of rumors).
- `PMM_UndineAdult` — spring-keeper; +Animals, +Medicine (gentle caretaker, fishing
  waters — sea-slime adulthood precedent).

### 4.4 `Defs/RulePackDefs/RulePacks_Namers_Elemental.xml`

One shared `NamerPersonElemental` rule pack (elemental-flavored name syllables), wired
into all four xenotypes (`nameMaker`/`nameMakerFemale` + `chanceToUseNameMaker 1`),
exactly mirroring `RulePacks_Namers_Slime.xml`. Per-element name packs are a phase-4
nice-to-have.

### 4.5 `Defs/IncidentDefs/Incidents_Elemental.xml`

Four incidents, all cloned from the slime wander-in skeleton: `category Misc`,
`targetTags Map_PlayerHome`, `populationEffect IncreaseMedium`, `letterDef NeutralEvent`,
`baseChance 1.0` (**D7**), **no `<allowedBiomes>`** (§1, critical detail).

| defName | workerClass | letterText draft |
|---|---|---|
| `PMM_GnomeWandersIn` | `IncidentWorker_GnomeWandersIn` | "A {0} has risen from the deep caves and wandered into the area, her earthen body leaving faint trails of loam. {PAWN_pronoun}'s called {PAWN_nameDef}.\n\nYou can attempt to tame {PAWN_objective}." |
| `PMM_IgnisWandersIn` | `IncidentWorker_IgnisWandersIn` | "A {0} has strode out of the shimmering heat haze into the area, flames licking harmlessly along her skin. {PAWN_pronoun}'s called {PAWN_nameDef}.\n\nYou can attempt to tame {PAWN_objective}." |
| `PMM_SylphWandersIn` | `IncidentWorker_SylphWandersIn` | "A {0} has descended from the high winds into the area, alighting as if on a whim. {PAWN_pronoun}'s called {PAWN_nameDef}.\n\nYou can attempt to tame {PAWN_objective}." |
| `PMM_UndineWandersIn` | `IncidentWorker_UndineWandersIn` | "A {0} has risen from the waters and wandered into the area, her translucent body glistening. {PAWN_pronoun}'s called {PAWN_nameDef}.\n\nYou can attempt to tame {PAWN_objective}." |

`letterLabel` `{0} wanders in` for all four (slime convention).

---

## 5. C# (Phase 2) — `Source/Elementals/`

### 5.1 `ElementalMod.cs`
- `[StaticConstructorOnStartup] static ElementalMod` → `new Harmony("PMM.Elementals").PatchAll(...)`.
- `[DefOf] ElementalDefOf`: the four pawn kinds + four xenotypes + four race ThingDefs
  (backstory/race patches need them without string lookups at call time; adulthood
  defs fetched by name per kind, slime pattern).

### 5.2 `ElementalWandersIn.cs`
Copy `SlimeWandersIn.cs` wholesale, then adapt:
- Base `IncidentWorker_ElementalWandersIn : IncidentWorker_WildManWandersIn` keeps
  everything verbatim: `BaseCanFireNowSub` (toxic fallout / noxious haze, no vanilla
  seasonal gate, no former-faction requirement), `TryExecuteWorker` (edge cell →
  `GeneratePawn` → clear faction → `GenSpawn.Spawn` →
  `WildManEverReachedOutside = true` → formatted letter), `TryFindEntryCell`,
  `TryFindFormerFaction`, and the `HasCaves(map)` roof sampler (stride 8, ≥8
  `RoofRockThick` hits) — gnome needs it.
- `PawnKindToSpawn` becomes abstract / virtual per subclass. Debug `Log.Message` lines
  use the `[PMM_Elementals]` prefix.

The four gates (`ClimateAcceptable` overrides, world-tile data via
`Find.WorldGrid[map.Tile]`):

```csharp
// Gnome — maps with caves
protected override bool ClimateAcceptable(Map map) => HasCaves(map);

// Ignis — hot maps only
private static readonly HashSet<string> HotBiomes = new() { "Desert", "ExtremeDesert", "LavaField" };
protected override bool ClimateAcceptable(Map map)
{
    var tile = Find.WorldGrid[map.Tile];
    if (HotBiomes.Contains(tile.PrimaryBiome.defName)) return true;
    return GenTemperature.AverageTemperatureAtTile(map.Tile) > 30f;
}

// Sylph — high country
protected override bool ClimateAcceptable(Map map)
    => Find.WorldGrid[map.Tile].elevation > 1000f;

// Undine — tiles with water (D1)
protected override bool ClimateAcceptable(Map map)
{
    var tile = Find.WorldGrid[map.Tile];
    return tile.IsCoastal
        || (tile.Rivers != null && tile.Rivers.Count > 0)
        || tile.PrimaryBiome.defName == "Lake";
}
```

Verified API surface: `GenTemperature.AverageTemperatureAtTile(int)` (same helper family
as the slime mod's `MinTemperatureAtTile`), `Tile.elevation` (float, meters),
`Tile.IsCoastal` (used by the sea slime), `Tile.Rivers` (`List<Tile.RiverLink>`),
biome defNames `Desert`/`ExtremeDesert` (Core) and `LavaField` (Odyssey) — all confirmed
against the installed 1.6 game data. `LavaField` needs no `MayRequire` in C#, but note
the string compare is Odyssey-only content; on a non-Odyssey game it simply never matches.

### 5.3 `ElementalPhysiology.cs`
Copy the three slime wild-man patches, keyed on a new `ElementalKinds.IsElementalKind`
(the four kind defs — a `HashSet<PawnKindDef>` built from `ElementalDefOf`):
1. **`Patch_ElementalIsWildMan`** — postfix `WildManUtility.IsWildMan`: elementals are
   wild men (drives taming via `WorkGiver_Tame`, arrest flow, wander behaviour).
2. **`Patch_ElementalShouldNotReachOutside`** — postfix
   `WildManUtility.WildManShouldReachOutsideNow`: return false for elemental kinds, so
   they keep wandering the map until tamed (the incident pre-sets
   `WildManEverReachedOutside`).
3. **`Patch_ElementalBackstory`** — postfix on pawn generation: pin
   `PMM_ElementalChild` + the per-kind adulthood via the `childhood`/`adulthood`
   `FieldRef`s, with a kind→adulthood-name map (`PMM_GnomeWild` → `PMM_GnomeAdult`, …),
   so even generation paths that bypass the pawn kind's backstory filter produce the
   right stories. Direct port of `Patch_SlimeBackstory`.
4. **`ElementalRaces` + `Patch_ElementalRaceByXenotype`** (D2 custom races): static
   `RaceFor(xenotypeDefName)` map (slime `SlimeRaces` pattern) and a generation postfix
   swapping `pawn.def` to the matching elemental race — port of the slime
   `Patch_SlimeRaceByXenotype`. 1:1 mapping (pinned xenotypeSets), so this is mostly
   belt-and-braces for callers that roll the xenotype directly.

No other physiology patches in v1: unlike slimes there is no gel gene, no filth, no
butcher override, no tattoo/hair patches.

---

## 6. Phase 3 (optional, post-v1) — signature powers

Only if v1 feels too plain. Each is independent; any subset can ship.
- **Thematic gene kits** (deferred by locked D4/D5; all defNames verified vs Biotech
  1.6). Ignis: `FireResistant`, `FireSpew`, `MaxTemp_LargeIncrease`,
  `MinTemp_SmallIncrease` (cold weakness), `MeleeDamage_Strong`. Gnome: `Robust`,
  `Pain_Reduced`, `Mood_Sanguine`, `DarkVision`, `MoveSpeed_Slow`,
  `AptitudeStrong_Mining`. Sylph: `MoveSpeed_VeryQuick`, `MinTemp_SmallDecrease`,
  `Learning_Fast`, `LowSleep`, `MeleeDamage_Weak`. Undine: `Mood_Optimist`, `Robust`,
  `Pain_Reduced`. (Check `FireSpew` exclusion tags when the Ignis kit lands.)
- **Ignis flame aura** — comp on the custom race: melee attackers that hit her take
  small fire burns (the "flames only burn when she wills it" clause). D2's custom
  races make this a clean `CompProperties` on `PMM_Race_IgnisMomo`.
- **Gnome living stone** — comp or hediff: slow passive healing while standing on
  rough stone / under mountain roof.
- **Sylph caprice** — mood swings: a hidden hediff cycling small ±mood offsets on a
  multi-day whim (the capricious disposition no vanilla gene models).
- **Elemental butcher drops** — `specificMeatDef` "elemental mote/core" per race
  instead of human meat (the custom races make this a two-line XML change per race).
- ~~Undine devotion / covenant thoughts~~ — **cut per locked D6** (no covenant
  mechanic). Revisit only if D6 is re-opened.

## 7. Phase 4 — art & release
- Custom xenotype icons in `Textures/UI/Icons/Xenotypes/` (4), replacing the vanilla
  placeholders; custom skin colours if **D3** goes custom (PMM_Skin_SeaFoam precedent).
- README screenshots on correctly-gated maps; CHANGELOG 1.0.0; `release.sh v1.0.0`;
  Steam Workshop first upload → fill `About/PublishedFileId.txt`.

---

## 8. Decisions — LOCKED (2026-08-30)

| # | Question | Locked outcome |
|---|---|---|
| **D1** | Undine "water" definition | **Coastal + river tiles + `Lake` biome** (option c). World-tile checks only — cheap and deterministic |
| **D2** | Race | **Custom elemental races** — one per element (`PMM_Race_*Momo`, §4.2b); v1 keeps human butcher yields, races give identity + future comp hooks |
| **D3** | Skin colours | **Reuse Biotech skin genes** (SlateGray / Orange / LightGray / SheerWhite); custom `PMM_Skin_*` colours in the phase 4 art pass |
| **D4** | Ignis genes | **Momo + skin only** — fire kit (FireResistant/FireSpew/etc.) deferred to phase 3 |
| **D5** | Sylph genes | **Momo + skin only** — wind kit deferred to phase 3. Ruling applied to all four xenotypes for consistency |
| **D6** | Covenant mechanic | **None** — the Momo gene stays (they are momos; bond-capable by inheritance), but no covenant-specific content is built |
| **D7** | Incident `baseChance` | **1.0 flat** for all four; tune after play-testing |
| **D8** | Repo/release identity | **`Gaph0/PMM---Elementals`**, set in `release.sh` |

---

## 9. Testing checklist (per build)

1. `build.sh` clean compile (core auto-builds if missing).
2. Startup: no def cross-ref errors; four xenotypes/kinds/incidents in dev-mode lists.
3. Gate negative tests (dev-mode "fire incident" on wrong maps): gnome blocked on
   caveless flat map; ignis blocked on boreal map; sylph blocked on lowland map
   (≤1000 m); undine blocked on dry inland tile. Each logs its `[PMM_Elementals]`
   reason.
4. Gate positive tests: gnome fires on mountain/cave map; ignis fires on Desert,
   ExtremeDesert, LavaField and a >30 °C non-desert tile; sylph fires on >1000 m tile;
   undine fires on coastal, river and Lake tiles.
5. Spawn sanity: correct xenotype, female, naked/unarmed, factionless, elemental
   backstories pinned, name from `NamerPersonElemental`.
6. Wild-man flow: tame gizmo present (not arrest-first), pawn keeps wandering (no
   edge-desawn), joins colony on tame; mana need present (Momo gene active).
7. Undine swims fast (Water terrain factor), ignis shrugs fire and heat but hates
   cold, sylph outruns everyone, gnome tanks hits.
8. Save/load round-trip on a map with all four elementals.

## 10. Explicitly out of scope

- No slime-mod dependency, no shared code — pattern copy only.
- No new faction, settlements, or world objects (the old slime-faction settlement code
  stays retired).
- No psycasts/VPE integration (slime dark-caster pattern) — elementals are physical
  spirits, not casters, in v1. Revisit in phase 3 if Ignis/Sylph feel under-powered.
- No "dark elemental" corruption variants from the wiki — lore notes only; would need
  a mamono-mana corruption system that doesn't exist yet.
