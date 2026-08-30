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
