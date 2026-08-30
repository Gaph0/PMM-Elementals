# Changelog
- 2026-08-30: chore: phase 0 scaffold (About metadata, build/release scripts, README;
  plan D1-D8 locked: custom elemental races, minimal momo+skin xenotypes,
  Undine water = coastal/river/lake)
- 2026-08-30: feat: phase 1 defs - four elemental xenotypes (momo+skin minimal per
  locked D4/D5), custom Human-derived races (D2), wild pawn kinds (gnome/ignis/sylph/
  undine, undine with Water speed 2.5), shared elemental childhood + four adulthoods,
  four gated wander-in incidents (no allowedBiomes; gates live in C#), elemental
  name generator
- 2026-08-30: feat: custom undine/sylph skin genes - PMM_Skin_UndineBlue (watery
  blues) and PMM_Skin_SylphGreen (light greens), parented on Biotech
  GeneSkinColorOverride so randomBrightnessFactor varies the shade per pawn
- 2026-08-30: feat: gnome/ignis custom skin genes - PMM_Skin_GnomeBrown (dirt
  browns) and PMM_Skin_IgnisOrange (flame oranges); all four elementals now use
  custom PMM_Skin_* genes with per-pawn shade variation
- 2026-08-30: feat: phase 2 assembly - four gated wander-in workers (gnome caves,
  ignis hot biome/>30C avg, sylph >1000m, undine coast/river/lake), wild-man +
  reach-outside + per-element backstory pins, race-swap postfix. Handles 1.6 API
  (PlanetTile, SurfaceTile.Rivers, avg temp = (Min+Max)/2)
- 2026-08-30: feat: elemental wild-man think trees (one branch per kind so wild
  elementals wander instead of JobGiver_ExitMapBest off the map) + sync.sh to
  rsync the mod into RimWorld/Mods for testing
- 2026-08-30: fix: gnome gate false-positived on caveless maps - the slime-style
  roof-grid sampler counted incidental overhead-mountain rock (~500 cells = a small
  hill) as "caves". Replaced with vanilla World.HasCaves(map.Tile), which checks the
  tile's cave TileMutator directly and cannot false-positive on rock
- 2026-08-30: feat: full thematic gene kits per user spec (amends locked D4/D5 minimal
  ruling). Gnome dead-calm/cave-dweller/robust/plants+construction; Ignis naked-speed/
  fire-immune/great-cook/cold-weak/quick; Sylph happy/kind/low-sleep/delicate/attractive/
  quick-study/social+artistic; Undine happy/kind/plants+medical/poor-mining/nearsighted/
  cold-weak/slow. Aptitudes via auto-generated Aptitude{Level}_{Skill} template genes
- 2026-08-30: fix: gene-kit XML malformed - Ignis/Sylph kit genes orphaned after
  description and Undine kit duplicated outside the def (loader dropped them with
  "doesn't correspond to any field" errors). Kits now correctly inside each genes block
- 2026-08-30: feat: ignis heat super-tolerance (MaxTemp_LargeIncrease) - pairs with
  her cold weakness (MinTemp_SmallIncrease); a fire elemental shrugs heat, suffers cold
- 2026-08-30: balance: sylph very-happy -> happy (Mood_Sanguine -> Mood_Optimist),
  great-artistic -> strong-artistic (AptitudeRemarkable_Artistic -> AptitudeStrong_Artistic)
- 2026-08-30: feat: phase 3 signature powers - Ignis flame aura (humans get extra
  tease never fire, momos/animals/insects/mechs ignite), Gnome living-stone healing
  (rough ground/under mountain), Sylph caprice mood whims (thought-based, foul/giddy),
  Undine water affinity (wet-weather speed hediff via map component, Filth_Water at 1/3
  rate instead of trash, CleaningSpeed 1.5). Caravan-speed investigated but deferred
  (no per-pawn hook; needs bespoke GetTicksPerMove patch)
- 2026-08-30: fix: phase-3 powers never fired - a pawn's RACE-def <comps> are never
  instantiated (InitializeComps only runs for HediffWithComps/WorldObject, never a
  Pawn's race def), so race comps were inert. Ported to: gnome/sylph HediffComps on a
  hidden marker hediff granted at generation (the slime jelly-ooze mechanism), ignis
  melee patch, undine map component + filth patch. Undine CleaningSpeed stat kept (a
  race stat, not a comp). Removed inert race <comps>
- 2026-08-30: fix: gnome living-stone missed rough rock - OnStone only checked the
  Diggable affordance (soil/gravel), not rough stone. Added SmoothableStone affordance
  (how the smooth-floor designator detects rough Granite/Slate), so gnomes now regen on
  bare mountain rock as well as soil and under mountain roof
