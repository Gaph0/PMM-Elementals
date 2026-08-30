using System.Collections.Generic;
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
                Log.Message("[PMM_Elementals] wander-in blocked: target is not a map");
                return false;
            }
            if (!ClimateAcceptable(map))
            {
                return false; // ClimateAcceptable logs the specific failing gate
            }
            if (!TryFindEntryCell(map, out _))
            {
                Log.Message("[PMM_Elementals] wander-in blocked: no edge cell can reach the colony");
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
                Log.Message("[PMM_Elementals] wander-in blocked: target is not a map");
                return false;
            }
            if (map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                Log.Message("[PMM_Elementals] wander-in blocked: toxic fallout active");
                return false;
            }
            if (ModsConfig.BiotechActive && map.GameConditionManager.ConditionIsActive(GameConditionDefOf.NoxiousHaze))
            {
                Log.Message("[PMM_Elementals] wander-in blocked: noxious haze active");
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
    }

    /// <summary>
    /// A gnome wanders in. Same wild-man spawn flow as the base, but it only fires on
    /// tiles that GENERATE caves - the caves and mines earth elementals call home - and
    /// always spawns the gnome-only pawn kind. No biome or climate gate: gnomes are not
    /// tied to surface weather, only to caves.
    /// </summary>
    public class IncidentWorker_GnomeWandersIn : IncidentWorker_ElementalWandersIn
    {
        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_GnomeWild;

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
                Log.Message("[PMM_Elementals] gnome wander-in blocked: world tile has no caves (no cave tile mutator)");
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

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_IgnisWild;

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
                Log.Message($"[PMM_Elementals] ignis wander-in blocked: biome {tile?.PrimaryBiome?.defName ?? "null"}, avg temp {avgTemp:F1}C <= {MinAverageTemp}C");
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

        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_SylphWild;

        /// <summary>Sylph gate: the world tile must sit above 1000 m.</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            float elevation = Find.WorldGrid[map.Tile].elevation;
            if (elevation <= MinElevation)
            {
                Log.Message($"[PMM_Elementals] sylph wander-in blocked: elevation {elevation:F0}m <= {MinElevation:F0}m");
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
        protected override PawnKindDef PawnKindToSpawn => ElementalDefOf.PMM_UndineWild;

        /// <summary>Undine gate: the world tile must touch water (coast, river, or lake).</summary>
        protected override bool ClimateAcceptable(Map map)
        {
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile == null)
            {
                Log.Message("[PMM_Elementals] undine wander-in blocked: world tile is null");
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
            Log.Message("[PMM_Elementals] undine wander-in blocked: tile is not coastal, riverine, or lake");
            return false;
        }
    }
}
