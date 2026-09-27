using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PMM_Elementals
{
    // =====================================================================
    // Phase-3 signature powers.
    //
    // ARCHITECTURE NOTE (the phase-3a bug): a Pawn's RACE-def <comps> are never
    // instantiated - ThingWithComps.InitializeComps is called for HediffWithComps
    // and WorldObject, never for a Pawn's race def, and Pawn never calls it for its
    // own race comps. So race <comps> on PMM_Race_*Mamono were inert (no gnome healing,
    // no undine filth, no ignis aura). Two reliable mechanisms are used instead:
    //   * Hediff comps on a hidden marker hediff (hediffs on a pawn DO get comps and
    //     DO tick - the slime mod's jelly-ooze uses exactly this), driven per-xenotype;
    //   * Harmony patches keyed on the pawn's xenotype (the phase-2 elementals pattern).
    // =====================================================================

    /// <summary>Xenotype helpers for the powers, keyed on genes.Xenotype.defName.</summary>
    public static class ElementalXenotypes
    {
        public static bool IsGnome(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Gnome";
        public static bool IsIgnis(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Ignis";
        public static bool IsSylph(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Sylph";
        public static bool IsUndine(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Undine";
        public static bool IsDorome(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Dorome";
        public static bool IsDryad(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Dryad";
        public static bool IsApsara(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Apsara";
        public static bool IsGenie(Pawn p) => p?.genes?.Xenotype?.defName == "PMM_Elemental_Genie";
    }

    // =====================================================================
    // IGNIS - flame aura. When an ignis (wild or colony) lands a melee hit:
    //   * HUMANS (non-mamono) → extra tease, never fire (a Mamono's melee on a human
    //     already deals tease instead of physical harm - core TeaseDamagePatch).
    //   * everything else (mamonos, animals, insects, mechanoids) → ignited.
    // =====================================================================

    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_IgnisFlameAura
    {
        private const float TeasePerHit = 0.03f;
        private const float FireSize = 0.35f;
        private static GeneDef mamonoGene;

        private static bool IsMamonoCarrier(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return false;
            }
            if (mamonoGene == null)
            {
                mamonoGene = DefDatabase<GeneDef>.GetNamedSilentFail("ProjectMamono_Mamono");
            }
            if (mamonoGene == null)
            {
                return false;
            }
            Gene g = pawn.genes.GetGene(mamonoGene);
            return g != null && g.Active;
        }

        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn attacker = __instance.CasterPawn;
            Pawn victim = target.HasThing ? target.Thing as Pawn : null;
            if (attacker == null || victim == null || attacker == victim || victim.Dead)
            {
                return;
            }
            if (!ElementalXenotypes.IsIgnis(attacker) || victim.health?.hediffSet == null)
            {
                return;
            }

            if (victim.RaceProps != null && victim.RaceProps.Humanlike && !IsMamonoCarrier(victim))
            {
                BodyPartRecord brain = victim.health.hediffSet.GetBrain();
                if (brain == null)
                {
                    return;
                }
                Hediff tease = victim.health.hediffSet.GetFirstHediffOfDef(ElementalDefOf.ProjectMamono_TeaseDamage);
                if (tease != null)
                {
                    tease.Severity = Mathf.Clamp(tease.Severity + TeasePerHit, tease.def.minSeverity, tease.def.maxSeverity);
                }
                else
                {
                    tease = HediffMaker.MakeHediff(ElementalDefOf.ProjectMamono_TeaseDamage, victim, brain);
                    tease.Severity = Mathf.Clamp(TeasePerHit, tease.def.minSeverity, tease.def.maxSeverity);
                    victim.health.AddHediff(tease, brain);
                }
            }
            else
            {
                // Mamonos, animals, insects, mechanoids: will the flames to burn.
                victim.TryAttachFire(FireSize, attacker);
            }
        }
    }

    // =====================================================================
    // GNOME - living stone. Hidden marker hediff carries a HediffComp that, on a rare
    // interval, shaves severity off her worst injury while on rough ground / under mountain.
    // =====================================================================

    public class HediffCompProperties_LivingStone : HediffCompProperties
    {
        public HediffCompProperties_LivingStone() { compClass = typeof(HediffComp_LivingStone); }
        public int intervalTicks = 2500;
        public float severityPerPulse = 1.0f;
    }

    public class HediffComp_LivingStone : HediffComp
    {
        private static TerrainAffordanceDef diggableAffordance;

        public HediffCompProperties_LivingStone Props => (HediffCompProperties_LivingStone)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || pawn.Map == null || pawn.health?.hediffSet == null)
            {
                return;
            }
            if (!pawn.IsHashIntervalTick(Props.intervalTicks, delta))
            {
                return;
            }
            if (!OnStone(pawn))
            {
                return;
            }
            HealWorstInjury(pawn, Props.severityPerPulse);
        }

        private static bool OnStone(Pawn pawn)
        {
            if (pawn.Position.Roofed(pawn.Map) &&
                pawn.Position.GetRoof(pawn.Map) == RoofDefOf.RoofRockThick)
            {
                return true; // deep under the mountain
            }
            if (diggableAffordance == null)
            {
                diggableAffordance = DefDatabase<TerrainAffordanceDef>.GetNamedSilentFail("Diggable");
            }
            TerrainDef terrain = pawn.Position.GetTerrain(pawn.Map);
            if (terrain?.affordances == null)
            {
                return false;
            }
            // Rough stone (Granite/Slate/etc. - the rough-rock a mountain map bares, which the
            // smooth-floor designator detects via SmoothableStone) OR rough diggable ground
            // (gravel/soil/sand). Smoothed and built floors carry neither affordance.
            return terrain.affordances.Contains(TerrainAffordanceDefOf.SmoothableStone) ||
                   (diggableAffordance != null && terrain.affordances.Contains(diggableAffordance));
        }

        private static void HealWorstInjury(Pawn pawn, float amount)
        {
            Hediff worst = null;
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury injury && injury.Severity > 0f && injury.Visible &&
                    (worst == null || injury.Severity > worst.Severity))
                {
                    worst = injury;
                }
            }
            if (worst != null)
            {
                worst.Severity = Mathf.Max(worst.def.minSeverity, worst.Severity - amount);
            }
        }
    }

    // =====================================================================
    // SYLPH - caprice. Hidden marker hediff carries a HediffComp that re-rolls a whim
    // each ~24 h and applies a 1-day memory thought (foul / giddy / calm).
    // =====================================================================

    public class HediffCompProperties_Caprice : HediffCompProperties
    {
        public HediffCompProperties_Caprice() { compClass = typeof(HediffComp_Caprice); }
        public float whimIntervalHours = 24f;
        public float foulChance = 0.3f;
        public float giddyChance = 0.3f;
    }

    public class HediffComp_Caprice : HediffComp
    {
        private int nextWhimTick = -1;

        public HediffCompProperties_Caprice Props => (HediffCompProperties_Caprice)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || pawn.needs?.mood == null)
            {
                return;
            }
            if (Find.TickManager.TicksGame >= nextWhimTick)
            {
                ApplyWhim(pawn);
                nextWhimTick = Find.TickManager.TicksGame + (int)(Props.whimIntervalHours * GenDate.TicksPerHour);
            }
        }

        private void ApplyWhim(Pawn pawn)
        {
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ElementalDefOf.PMM_Thought_SylphCapriceFoul);
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ElementalDefOf.PMM_Thought_SylphCapriceGiddy);

            float roll = Rand.Value;
            if (roll < Props.foulChance)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(ElementalDefOf.PMM_Thought_SylphCapriceFoul);
            }
            else if (roll < Props.foulChance + Props.giddyChance)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(ElementalDefOf.PMM_Thought_SylphCapriceGiddy);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextWhimTick, "nextWhimTick", -1);
        }
    }

    // =====================================================================
    // Marker-hediff granter: adds the hidden genie-freedom marker once genes are applied
    // (it carries the countdown comp). Gnome, sylph, dryad and apsara need no marker:
    // their comps sit on their race trackers.
    // =====================================================================

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
        new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_GrantElementalMarkers
    {
        public static void Postfix(Pawn __result)
        {
            if (__result?.health?.hediffSet == null)
            {
                return;
            }
            if (ElementalXenotypes.IsGenie(__result))
            {
                GrantMarker(__result, ElementalDefOf.PMM_Hediff_GenieFreedom);
            }
        }

        private static void GrantMarker(Pawn pawn, HediffDef def)
        {
            if (def == null || pawn.health.hediffSet.GetFirstHediffOfDef(def) != null)
            {
                return;
            }
            Hediff h = HediffMaker.MakeHediff(def, pawn);
            h.Severity = 0.01f;
            pawn.health.AddHediff(h);
        }
    }

    // =====================================================================
    // UNDINE - wet-weather speed + water filth + slower filth rate.
    // =====================================================================

    public class MapComponent_UndineWetSpeed : MapComponent
    {
        private const int CheckInterval = 500;
        private int nextCheck;

        public MapComponent_UndineWetSpeed(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame < nextCheck)
            {
                return;
            }
            nextCheck = Find.TickManager.TicksGame + CheckInterval;

            bool wet = map.weatherManager.RainRate > 0.01f || map.weatherManager.SnowRate > 0.01f;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!ElementalXenotypes.IsUndine(p) || p?.health?.hediffSet == null)
                {
                    continue;
                }
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(ElementalDefOf.PMM_Hediff_UndineWetSpeed);
                if (wet && h == null)
                {
                    h = HediffMaker.MakeHediff(ElementalDefOf.PMM_Hediff_UndineWetSpeed, p);
                    h.Severity = 1f;
                    p.health.AddHediff(h);
                }
                else if (!wet && h != null)
                {
                    p.health.RemoveHediff(h);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.ConstructComponents))]
    public static class Patch_AddUndineWetSpeedComponent
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<MapComponent_UndineWetSpeed>() == null)
            {
                __instance.components.Add(new MapComponent_UndineWetSpeed(__instance));
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.Notify_EnteredNewCell))]
    public static class Patch_UndineWaterFilth
    {
        private static readonly AccessTools.FieldRef<Pawn_FilthTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_FilthTracker, Pawn>("pawn");

        private static readonly System.Reflection.PropertyInfo AdditionalFlagsProp =
            AccessTools.Property(typeof(Pawn_FilthTracker), "AdditionalFilthSourceFlags");
        private static readonly System.Reflection.MethodInfo NotifyHumanFilth =
            AccessTools.Method("RimWorld.FilthMonitor:Notify_FilthHumanGenerated");

        public static bool Prefix(Pawn_FilthTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike || !ElementalXenotypes.IsUndine(pawn))
            {
                return true;
            }

            if (Rand.Value < pawn.GetStatValue(StatDefOf.FilthRate) * 0.005f * 0.33f)
            {
                var flags = (FilthSourceFlags)(AdditionalFlagsProp?.GetValue(__instance) ?? FilthSourceFlags.None);
                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_Water, 1, flags, true);
                NotifyHumanFilth?.Invoke(null, null);
            }
            return false;
        }
    }
}
