using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PMM_Elementals
{
    // =====================================================================
    // DOROME - clay-elemental powers (§11, locked D9–D13).
    //   * Mud filth: she sheds custom PMM_Filth_Mud instead of vanilla terrain
    //     filth and trash (verbatim slime/undine filth-patch pattern, D10).
    //   * Mud-merge teleport: TAMED-ONLY (D11). A gene-granted ability targets
    //     any revealed natural tile on the map; the vanilla ability system runs
    //     the 6 h cooldown from cooldownTicksRange - no custom cooldown state,
    //     no wild surface, no marker hediff.
    //   * Clay-grey hair: her hair renders in her rolled skin shade (§11.4b),
    //     the slime Patch_SlimeHairColor pattern.
    // =====================================================================

    /// <summary>
    /// Doromes ooze mud, never dirt or trash. A full replacement for the vanilla
    /// ambient-filth branch: keeps the FilthRate gate, then always drops
    /// <see cref="ElementalDefOf.PMM_Filth_Mud"/> - vanilla would drop the terrain
    /// filth 66 % of the time and <c>Filth_Trash</c> the other 34 %. Runs as a
    /// prefix that skips the original for dorome pawns only. Full vanilla rate
    /// (D10 - no ×0.33 mercy; slimes shed at full rate and so does wet clay).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.Notify_EnteredNewCell))]
    public static class Patch_DoromeMudFilth
    {
        private static readonly AccessTools.FieldRef<Pawn_FilthTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_FilthTracker, Pawn>("pawn");

        // AdditionalFilthSourceFlags getter is public-but-hidden and FilthMonitor is internal;
        // reach both through reflection (same as the slime/undine filth patches).
        private static readonly System.Reflection.PropertyInfo AdditionalFlagsProp =
            AccessTools.Property(typeof(Pawn_FilthTracker), "AdditionalFilthSourceFlags");
        private static readonly System.Reflection.MethodInfo NotifyHumanFilth =
            AccessTools.Method("RimWorld.FilthMonitor:Notify_FilthHumanGenerated");

        public static bool Prefix(Pawn_FilthTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike || !ElementalXenotypes.IsDorome(pawn))
            {
                return true; // not a dorome: vanilla logic untouched
            }

            // Same gate vanilla applies to every pawn: filth drops on roughly
            // FilthRate * 0.5 % of entered cells.
            if (Rand.Value < pawn.GetStatValue(StatDefOf.FilthRate) * 0.005f)
            {
                var flags = (FilthSourceFlags)(AdditionalFlagsProp?.GetValue(__instance) ?? FilthSourceFlags.None);
                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ElementalDefOf.PMM_Filth_Mud, 1, flags, true);
                NotifyHumanFilth?.Invoke(null, null);
            }
            return false; // handled; skip vanilla so no terrain filth or trash is dropped
        }
    }

    // =====================================================================
    // Mud-merge teleport helper. One static home for the natural-tile rule and
    // the teleport itself, shared by the ability effect comp below.
    // =====================================================================

    public static class DoromeTeleport
    {
        /// <summary>
        /// The natural-tile rule (locked D12): standable, not a constructed floor,
        /// not deep water, and revealed (no fog - the player must be able to see
        /// where she surfaces). Any roof is fine: caves are home. Shallow water and
        /// marsh pass: a wetland native does not fear a soak.
        /// </summary>
        public static bool IsValidTarget(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }
            if (map.fogGrid.IsFogged(cell))
            {
                return false; // must be revealed
            }
            if (!cell.Standable(map))
            {
                return false;
            }
            TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
            if (terrain == null || terrain.IsFloor)
            {
                return false; // no built floors
            }
            if (terrain.IsWater && terrain.passability == Traversability.Impassable)
            {
                return false; // deep water is impassable and fails Standable anyway, but be explicit
            }
            return true;
        }

        /// <summary>
        /// Melt her into the ground and have her emerge at the target cell: stops her
        /// current job, despawns and respawns, then splashes a little mud and a puff
        /// of dust at BOTH ends - she wells out of the earth, not a clean blink.
        /// </summary>
        public static void Teleport(Pawn pawn, IntVec3 target)
        {
            Map map = pawn.Map;
            if (map == null)
            {
                return;
            }
            IntVec3 origin = pawn.Position;

            pawn.jobs?.StopAll();
            pawn.DeSpawn();
            GenSpawn.Spawn(pawn, target, map, pawn.Rotation);

            Splash(map, origin);
            Splash(map, target);
        }

        /// <summary>A little sloughed-off mud and a burst of dust where she melts in/out.</summary>
        private static void Splash(Map map, IntVec3 cell)
        {
            FilthMaker.TryMakeFilth(cell, map, ElementalDefOf.PMM_Filth_Mud, Rand.RangeInclusive(2, 4),
                FilthSourceFlags.None, true);
            FleckMaker.ThrowDustPuff(cell.ToVector3Shifted(), map, 2f);
        }
    }

    /// <summary>Properties for the mud-merge ability effect.</summary>
    public class CompProperties_AbilityMudMerge : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityMudMerge() { compClass = typeof(CompAbilityEffect_MudMerge); }
    }

    /// <summary>
    /// The mud-merge ability effect: validates the targeted cell against the
    /// natural-tile rule and teleports the caster there. The vanilla ability
    /// system applies <c>cooldownTicksRange</c> (15000 ticks = 6 h) after the
    /// cast, so no cooldown bookkeeping lives here.
    /// </summary>
    public class CompAbilityEffect_MudMerge : CompAbilityEffect
    {
        public new CompProperties_AbilityMudMerge Props => (CompProperties_AbilityMudMerge)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = parent.pawn;
            if (!DoromeTeleport.IsValidTarget(pawn?.Map, target.Cell))
            {
                if (throwMessages)
                {
                    Messages.Message("PMM_DoromeMudMerge_InvalidTarget".Translate(),
                        MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (DoromeTeleport.IsValidTarget(pawn?.Map, target.Cell))
            {
                DoromeTeleport.Teleport(pawn, target.Cell);
            }
        }
    }

    // =====================================================================
    // Clay-grey hair (§11.4b): a dorome's hair IS more wet clay, the exact same
    // shade as her body. A postfix on the render node's colour lookup means it
    // holds no matter how she was spawned or restyled - the slime
    // Patch_SlimeHairColor pattern, keyed on the dorome xenotype.
    // =====================================================================

    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.ColorFor))]
    public static class Patch_DoromeHairColor
    {
        public static void Postfix(PawnRenderNode __instance, Pawn pawn, ref Color __result)
        {
            if (__instance is PawnRenderNode_Hair && pawn?.story != null &&
                ElementalXenotypes.IsDorome(pawn))
            {
                __result = pawn.story.SkinColor; // hair = her rolled clay shade
            }
        }
    }
}
