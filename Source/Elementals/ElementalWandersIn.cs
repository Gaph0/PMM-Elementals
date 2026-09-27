using System.Collections.Generic;
using ProjectMamono;
using RimWorld;
using Verse;

namespace PMM_Elementals
{
    /// <summary>
    /// An elemental wanders in. Modelled on the vanilla wild-man incident (and ported
    /// from the slime wander-in): the base worker reproduces the vanilla spawn flow but
    /// spawns a wild elemental pawn kind instead of a WildMan, and each subclass gates
    /// the event to that elemental's home terrain via <see cref="ClimateAcceptable"/>.
    /// Wild elementals are tamed like wild men (see Patch_ElementalIsWildMan).
    ///
    /// CRITICAL: none of the four IncidentDefs set allowedBiomes, so all gating lives
    /// here in C#. The storyteller-level biome gate would hard-block the Ignis
    /// temperature clause (a 33 °C TropicalRainforest tile must still be eligible) and
    /// the Sylph elevation / Undine river clauses.
    /// </summary>
    public abstract class IncidentWorker_ElementalWandersIn : IncidentWorker_WildManWandersIn
    {
        /// <summary>The pawn kind this incident spawns. Each subclass pins its own element.</summary>
        protected abstract PawnKindDef PawnKindToSpawn { get; }

        /// <summary>The environment gate. Each subclass implements its own terrain rule.</summary>
        protected abstract bool ClimateAcceptable(Map map);

        /// <summary>Vanilla checks, minus the seasonal/faction gates, plus the terrain gate.</summary>
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Skip the vanilla SeasonAcceptableFor(Human) check (it gates on the CURRENT
            // seasonal temperature being within a human's comfy range, which would block
            // e.g. Ignis in the hottest season - the very condition she wants). Elementals
            // aren't migrating for comfort, so we run the rest of the base gates but
            // substitute our own terrain rule below.
            if (!BaseCanFireNowSub(parms))
            {
                return false; // BaseCanFireNowSub logs the specific failing gate
            }

            if (!(parms.target is Map map))
            {
                PMMLog.Message("[PMM_Elementals] wander-in blocked: target is not a map");
                return false;
            }
            if (!ClimateAcceptable(map))
            {
                return false; // ClimateAcceptable logs the specific failing gate
            }
            if (!TryFindEntryCell(map, out _))
            {
                PMMLog.Message("[PMM_Elementals] wander-in blocked: no edge cell can reach the colony");
                return false;
            }
            return true;
        }

        // base.CanFireNowSub minus the SeasonAcceptableFor(Human) seasonal-temperature gate
        // and the former-faction requirement. The vanilla wild-man event requires a
        // non-colony humanlike faction to be the wild man's "former faction"; elementals are
        // factionless creatures, not ex-faction wild people, so that requirement makes no
        // sense here and only blocks the event on worlds without a suitable faction. We keep
        // the sensible environmental gates (toxic fallout, noxious haze) and log which fires.
        private bool BaseCanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map))
            {
                PMMLog.Message("[PMM_Elementals] wander-in blocked: target is not a map");
                return false;
            }
            if (map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                PMMLog.Message("[PMM_Elementals] wander-in blocked: toxic fallout active");
                return false;
            }
            if (ModsConfig.BiotechActive && map.GameConditionManager.ConditionIsActive(GameConditionDefOf.NoxiousHaze))
            {
                PMMLog.Message("[PMM_Elementals] wander-in blocked: noxious haze active");
                return false;
            }
            return true;
        }

        /// <summary>Same flow as the vanilla wild-man event, with an elemental kind instead.</summary>
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindEntryCell(map, out IntVec3 cell))
            {
                return false;
            }

            // Former faction is optional flavour for the pawn's bio (relations, title origin);
            // elementals don't need one, so a world without a suitable faction still spawns them.
            TryFindFormerFaction(out Faction formerFaction);

            PawnKindDef kind = PawnKindToSpawn;
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, formerFaction, PawnGenerationContext.NonPlayer, map.Tile,
                forceGenerateNewPawn: false, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true,
                inhabitant: false, certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f, biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null, relationWithExtraPawnChanceFactor: 1f,
                validatorPreGear: null, validatorPostGear: null, forcedTraits: null,
                prohibitedTraits: null, minChanceToRedressWorldPawn: null,
                fixedBiologicalAge: null, fixedChronologicalAge: null, fixedGender: null,
                fixedLastName: null, fixedBirthName: null, fixedTitle: null,
                fixedIdeo: null, forceNoIdeo: false, forceNoBackstory: false,
                forbidAnyTitle: false, forceDead: false, forcedXenogenes: null,
                forcedEndogenes: null, forcedXenotype: null, forcedCustomXenotype: null,
                allowedXenotypes: null, forceBaselinerChance: 0f,
                // Vanilla passes DevelopmentalStage.Adult (value 8) here; it acts as a floor,
                // so generated wild pawns are always adults. (Newborn would force babies.)
                developmentalStages: DevelopmentalStage.Adult,
                forceNoGear: false));

            // Elementals are factionless. GeneratePawn with a null faction request can already
            // leave the pawn factionless, and Pawn.SetFaction logs a warning (popping the
            // debug log) when the new faction equals the current one - so only clear it
            // if generation actually assigned one (e.g. the former faction).
            if (pawn.Faction != null)
            {
                pawn.SetFaction(null, null);
            }
            GenSpawn.Spawn(pawn, cell, map);

            // Mark the wild elemental as already having "reached outside" so it never tries
            // to march to the map edge and despawn (see Patch_ElementalShouldNotReachOutside).
            // Vanilla wild men get this cleared once arrested/tamed; here it simply keeps
            // the elemental wandering the map until the player deals with it.
            if (pawn.mindState != null)
            {
                pawn.mindState.WildManEverReachedOutside = true;
            }

            // Use the incident def's own letter text ({0}=kind label, {PAWN_*} grammar tokens),
            // formatted the same way vanilla formats its wild-man letter.
            string kindLabel = pawn.KindLabel;
            TaggedString letterText = def.letterText.Formatted(kindLabel.Named("1"), pawn.Named("PAWN"))
                .AdjustedFor(pawn)
                .CapitalizeFirst();
            TaggedString letterLabel = def.letterLabel.Formatted(kindLabel.Named("0")).CapitalizeFirst();
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref letterLabel, pawn);
            SendStandardLetter(letterLabel, letterText, def.letterDef, parms, pawn);
            return true;
        }

        // The vanilla helpers are private; these are straight reimplementations.

        // Identical to the vanilla wild-man helper (which is private): any edge cell that
        // can reach the colony, ignoring roads.
        private static bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith(
                c => map.reachability.CanReachColony(c),
                map, CellFinder.EdgeRoadChance_Ignore, out cell);
        }

        private static bool TryFindFormerFaction(out Faction formerFaction)
        {
            return Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(
                out formerFaction, tryMedievalOrBetter: false, allowDefeated: true, TechLevel.Undefined);
        }

        /// <summary>
        /// Shared water gate for the water elementals (undine, apsara): the world tile
        /// must touch water - coastal tiles bordering an ocean, tiles on a river, or the
        /// Lake biome. Extracted from the undine worker so both water elementals share it.
        /// <paramref name="caller"/> names the elemental in the debug log lines.
        /// </summary>
        protected static bool TileHasWater(Map map, string caller)
        {
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile == null)
            {
                PMMLog.Message($"[PMM_Elementals] {caller} wander-in blocked: world tile is null");
                return false;
            }
            if (tile.IsCoastal)
            {
                return true; // borders an ocean
            }
            // Rivers live on SurfaceTile (the surface-layer Tile subclass) in 1.6, not on
            // the abstract Tile base. The int indexer returns the surface tile.
            if (Find.WorldGrid[map.Tile.tileId] is RimWorld.Planet.SurfaceTile surface &&
                surface.Rivers != null && surface.Rivers.Count > 0)
            {
                return true; // on a river
            }
            if (tile.PrimaryBiome?.defName == "Lake")
            {
                return true; // the Lake biome
            }
                PMMLog.Message($"[PMM_Elementals] {caller} wander-in blocked: tile is not coastal, riverine, or lake");
            return false;
        }
    }

    /// <summary>
    /// A gnome wanders in. Same wild-man spawn flow as the base, but it only fires on
    /// tiles that GENERATE caves - the caves and mines earth elementals call home - and
    /// always spawns the gnome-only pawn kind. No biome or climate gate: gnomes are not
    /// tied to surface weather, only to caves.
    /// </summary>
    public class IncidentWorker_GnomeWandersIn : IncidentWorker_ElementalWandersIn
    {
        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Gnome;

        /// <summary>
        /// Gnome gate: the world tile must generate caves. Uses the game's own
        /// <see cref="RimWorld.Planet.World.HasCaves"/>, which reports whether the tile
        /// carries a cave TileMutator (the same check vanilla's cave map generator uses).
        /// This replaced the slime-style roof-grid sampler: that heuristic only counted
        /// overhead-mountain ROCK on the current map, so any map with a small rocky hill
        /// (~500 mountain cells) read as "caves" even on a caveless world tile. The world
        /// mutator answers "does this tile have caves" directly and cannot false-positive
        /// on incidental rock.
        /// </summary>
        protected override bool ClimateAcceptable(Map map)
        {
            if (!Find.World.HasCaves(map.Tile))
            {
                PMMLog.Message("[PMM_Elementals] gnome wander-in blocked: world tile has no caves (no cave tile mutator)");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// An ignis wanders in. Only fires on hot maps: the Desert, ExtremeDesert or
    /// LavaField biomes, or any tile whose average temperature exceeds 30 °C - the hot
    /// lands fire elementals wander.
    /// </summary>
    public class IncidentWorker_IgnisWandersIn : IncidentWorker_ElementalWandersIn
    {
        /// <summary>Biomes hot enough for an ignis regardless of measured temperature.</summary>
        private static readonly HashSet<string> HotBiomes = new HashSet<string>
        {
            "Desert",
            "ExtremeDesert",
            "LavaField", // Odyssey; simply never matches on a non-Odyssey game
        };

        /// <summary>Average-temperature threshold in °C for the hot-lands clause.</summary>
        private const float MinAverageTemp = 30f;

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Ignis;

        /// <summary>Ignis gate: a hot biome, or a tile averaging over 30 °C.</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile?.PrimaryBiome != null && HotBiomes.Contains(tile.PrimaryBiome.defName))
            {
                return true; // desert / extreme desert / lava field: always hot enough
            }
            // 1.6 has no single AverageTemperatureAtTile; the yearly average is the
            // midpoint of the coldest and hottest twelfths (the same Min/Max helpers the
            // slime wander-in uses, and how vanilla builds its average-temperature label).
            float avgTemp = (GenTemperature.MinTemperatureAtTile(map.Tile) +
                             GenTemperature.MaxTemperatureAtTile(map.Tile)) / 2f;
            if (avgTemp <= MinAverageTemp)
            {
                PMMLog.Message($"[PMM_Elementals] ignis wander-in blocked: biome {tile?.PrimaryBiome?.defName ?? "null"}, avg temp {avgTemp:F1}C <= {MinAverageTemp}C");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A sylph wanders in. Only fires on tiles over 1000 m elevation - the high country
    /// where the mountain winds blow.
    /// </summary>
    public class IncidentWorker_SylphWandersIn : IncidentWorker_ElementalWandersIn
    {
        /// <summary>Elevation threshold in metres.</summary>
        private const float MinElevation = 1000f;

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Sylph;

        /// <summary>Sylph gate: the world tile must sit above 1000 m.</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            float elevation = Find.WorldGrid[map.Tile].elevation;
            if (elevation <= MinElevation)
            {
                PMMLog.Message($"[PMM_Elementals] sylph wander-in blocked: elevation {elevation:F0}m <= {MinElevation:F0}m");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// An undine wanders in. Only fires on tiles with water: coastal tiles bordering an
    /// ocean, tiles on a river, or the Lake biome (locked D1) - the waters undines call home.
    /// </summary>
    public class IncidentWorker_UndineWandersIn : IncidentWorker_ElementalWandersIn
    {
        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Undine;

        /// <summary>Undine gate: the world tile must touch water (coast, river, or lake).</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            return TileHasWater(map, "undine");
        }
    }

    /// <summary>
    /// An apsara wanders in. Same water gate as the undine (coast, river, or Lake) -
    /// her fellow water elemental; she is the joy elemental of the waters.
    /// </summary>
    public class IncidentWorker_ApsaraWandersIn : IncidentWorker_ElementalWandersIn
    {
        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Apsara;

        /// <summary>Apsara gate: the world tile must touch water (coast, river, or lake).</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            return TileHasWater(map, "apsara");
        }
    }

    /// <summary>
    /// A dorome wanders in. Fires on her two habitats (locked D9): tiles that generate
    /// caves - the deep earth she shares with gnomes - OR swamp biomes with at least
    /// 1000 mm of yearly rainfall: the sodden wetlands her muddy body calls home.
    /// </summary>
    public class IncidentWorker_DoromeWandersIn : IncidentWorker_ElementalWandersIn
    {
        /// <summary>Swamp biomes wet enough for a dorome when the rainfall clause passes.</summary>
        private static readonly HashSet<string> SwampBiomes = new HashSet<string>
        {
            "TemperateSwamp",
            "TropicalSwamp",
        };

        /// <summary>Rainfall threshold in millimetres per year for the wetland clause.</summary>
        private const float MinRainfall = 1000f;

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Dorome;

        /// <summary>Dorome gate: cave tile, or swamp biome with >= 1000 mm rainfall.</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            if (Find.World.HasCaves(map.Tile))
            {
                return true; // cave tiles: the gnome world-mutator check
            }
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile?.PrimaryBiome != null && SwampBiomes.Contains(tile.PrimaryBiome.defName))
            {
                if (tile.rainfall >= MinRainfall)
                {
                    return true; // rainy wetlands
                }
                PMMLog.Message($"[PMM_Elementals] dorome wander-in blocked: swamp biome {tile.PrimaryBiome.defName} but rainfall {tile.rainfall:F0}mm < {MinRainfall}mm");
                return false;
            }
                PMMLog.Message($"[PMM_Elementals] dorome wander-in blocked: no caves, biome {tile?.PrimaryBiome?.defName ?? "null"} is not a swamp");
            return false;
        }
    }

    /// <summary>
    /// A dryad wanders in. Fires on her one habitat (locked D14): forested maps, and
    /// only while the current outdoor temperature sits inside the vanilla tree growth
    /// range - she walks when her trees could be growing, never in deep winter or a
    /// killing heat wave. Swamps stay the dorome's wetlands; bogs are not her woods.
    /// </summary>
    public class IncidentWorker_DryadWandersIn : IncidentWorker_ElementalWandersIn
    {
        /// <summary>The three Core forest biomes a dryad calls home.</summary>
        private static readonly HashSet<string> ForestBiomes = new HashSet<string>
        {
            "TemperateForest",
            "BorealForest",
            "TropicalRainforest",
        };

        // Vanilla PlantProperties growth range (verified 1.6 IL): trees grow 0-58 C
        // (optimal 6-42). The dryad walks only when her trees could be growing.
        private const float MinGrowthTemp = 0f;
        private const float MaxGrowthTemp = 58f;

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_Elemental_Dryad;

        /// <summary>Dryad gate: forest biome AND current outdoor temp in 0-58 C.</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile?.PrimaryBiome == null || !ForestBiomes.Contains(tile.PrimaryBiome.defName))
            {
                PMMLog.Message($"[PMM_Elementals] dryad wander-in blocked: biome {tile?.PrimaryBiome?.defName ?? "null"} is not forest");
                return false;
            }
            float temp = map.mapTemperature.OutdoorTemp;
            if (temp < MinGrowthTemp || temp > MaxGrowthTemp)
            {
                PMMLog.Message($"[PMM_Elementals] dryad wander-in blocked: {temp:F0}C outside tree growth range {MinGrowthTemp:F0}-{MaxGrowthTemp:F0}C");
                return false;
            }
            return true;
        }
    }
}
