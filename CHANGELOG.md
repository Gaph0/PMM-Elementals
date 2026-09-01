# Changelog
- 2026-09-01: tweak: removed Beard_NoBeardOnly (beardless) from the genie xenotype (user request). Her kit is now the base-game Genie minus Hair_BaldOnly, ElongatedFingers and Beard_NoBeardOnly
- 2026-09-01: fix: freed genie no longer beelines for the map edge (user request). Root cause: PMM_GenieWild was missing from the PMM_ElementalWildBehavior think tree, so she fell through to the guest branch's JobGiver_ExitMapBest. Added her branches (wild-man core + idle wander) and a freedom timer: new PMM_Hediff_GenieFreedom marker (granted by Patch_GrantElementalMarkers) counts down 10000 ticks (4 in-game hours) from the moment she leaves the lamp, showing "(leaves in X)" in her health tab; when it runs out while she is still factionless, the new ThinkNode_ConditionalGenieFreedomSpent opens the vanilla JobGiver_ExitMapBest above her wild-man core and she walks off, with a departure letter (Patch_GenieDepartureMessage on Pawn.ExitMap). Welcoming her (Accept) drops the hediff; the node also ignores player-faction genies, so a colonist never walks off. Genies spawned by the previous build lack the hediff and simply wander forever - safe fallback. Offer letters now mention the few-hours window
- 2026-09-01: fix: genie bond gated behind the tsugai bond (user ruling) - women cleaning the lamp no longer become her master. The lamp-bond hediff is no longer granted (legacy def/comp kept so first-build saves load; inert - NOT auto-stripped because vanilla HealthTickInterval foreach-enumerates hediffs, so comp self-removal mid-tick would throw). BondedMaster now resolves live from the ProjectMomo_Tsugai relation, so the wish unlocks on any tsugai bond (including the core proposal system) and locks again when her husband dies. Clean-time: TsugaiFormation.CanBond gates the letter - bondable man gets the bonded text and Accept seals a full voluntary tsugai bond via TsugaiFormation.TryBond (AFTER RecruitUtility.Recruit, so the wild-bond auto-tame outcome early-returns instead of fighting the letter); any other cleaner frees her unbound. Letter/messages split into bonded/unbound variants
- 2026-09-01: feat: genie, the lamp elemental (eighth elemental). New flow, no wander-in: the PMM_GenieLampFound incident (Desert/AridShrubland/ExtremeDesert only, C#-gated like the wander-ins) drops an Old Dusty Lamp item; a colonist right-clicks → "Clean the lamp" (vanilla CompUsable + UseItem job, mech-serum pattern) → the genie swirls out bound to the cleaner (PMM_Hediff_GenieBond stores the master) with an accept/reject join-offer letter (custom ChoiceLetter - vanilla's AcceptJoiner needs quest signal plumbing). The wish (gene-granted ability, Dorome mud-merge pattern): targets only her bonded master, grants in priority order regrow biggest missing limb → erase a permanent scar → lift biggest temporary negative memory, greys out below 95% mana and drains it all, one-year cooldownTicksRange. Genes: base-game Genie xenotype verbatim minus Hair_BaldOnly and ElongatedFingers (user ruling), plus momo + PMM_Skin_GenieTan + wish gene. Full def stack: lamp item/incident/xenotype/race/wild pawn kind/backstory/bond hediff/letter def/keyed strings
- 2026-09-01: feat: dryad, the tree elemental (sixth elemental, plan §12 D14-D18 locked).
  Spawns on forest biomes (TemperateForest/BorealForest/TropicalRainforest) AND only
  while the current outdoor temperature is inside the vanilla tree growth range 0-58 C
  (D14) - she walks when her trees could be growing. Comfortable temp range set to the
  tree growth range 0-58 C on her race (D3). Photosynthesis (D15): a hidden marker
  hediff with a ticking HediffComp refills food/rest/mana at 2x natural fall rate while
  she stands unroofed in real sunlight (CurSkyGlow >= 0.5; sun lamps don't count). Green
  thumb (D16, user clarification): pawn-side PlantWorkSpeed 1.4 / PlantHarvestYield 1.25
  stat bases on her race - she works plants faster and her own harvests yield more (no
  aura). Butcher yields WoodLog, no leather (LeatherAmount 0). Bark-brown skin gene
  (D17). Gene kit (D18): AptitudeRemarkable_Plants, KindInstinct, Mood_Sanguine,
  Robust. Full def stack: xenotype/race/wild pawn
  kind/backstory/incident/think-tree branches
- 2026-09-01: balance: dryad kit - removed FireResistant and Immunity_Strong; added
  FireWeakness (tinderskin, fire damage x4), FireTerror (pyrophobia) and
  AptitudePoor_Social (user request). Final kit: AptitudeRemarkable_Plants, KindInstinct,
  Mood_Sanguine, FireWeakness, FireTerror, AptitudePoor_Social, Robust
- 2026-09-01: fix: dryad GeneDef cross-ref error - used Pyrophobia, which is a
  ThoughtDef (the pyrophobia thought), not a GeneDef. Swapped to FireTerror, the
  Biotech gene labelled pyrophobia (intense fear of fire, mental break near flames)
- 2026-09-01: feat: elemental wander-ins can fire on camp/temp maps - added
  Map_TempIncident to all five IncidentDefs alongside Map_PlayerHome (user request).
  Per-target camp chance boost investigated and deferred: 1.6 has no per-target
  chance-scaling hook (weights come from the global BaseChanceThisGame), so it would
  need a Harmony patch on the weight selector
- 2026-09-01: feat: dorome, the clay elemental (fifth elemental, plan §11 D9-D13 locked).
  Spawns on cave maps OR swamp biomes with >= 1000 mm rainfall (TemperateSwamp/
  TropicalSwamp). Sheds custom PMM_Filth_Mud instead of vanilla terrain filth/trash
  (full vanilla rate). Tamed-only mud-merge ability (gene-granted, any revealed
  natural tile, 6 h vanilla cooldown; wild doromes never teleport). Clay-grey skin
  gene with hair rendered in the same rolled shade (slime hair-patch pattern).
  Gene kit: Robust, MoveSpeed_Slow, Learning_Slow, Mood_Sanguine. Full def stack:
  xenotype/race/wild pawn kind/backstory/incident/think-tree branches
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
- 2026-08-30: balance: gnome living-stone regen 0.4 -> 1.0 severity/hour. Diagnose:
  added ignis-aura ignite logging (logs FlammableNow-false targets like mechs and
  successful ignites) to pin down why non-human mobs stopped igniting
- 2026-08-30: fix: ignis flame aura never fired for colony/recruited ignis - the aura
  was gated to wild-only (attacker.IsColonist returned early). Dropped the gate: any
  ignis (wild or colony) carries the aura. Removed the temporary ignite diagnostics
