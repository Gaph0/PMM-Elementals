using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PMM_Elementals
{
    // =====================================================================
    // Shared racial-comp base. The four elementals are Human-race-derived custom
    // races (D2); their phase-3 signature powers live in ThingComps attached to the
    // race ThingDefs. Pawn_HealthTracker.AddHediff calls pawn.health (not
    // ThingWithComps.health), so a pawn's own race comps initialise and tick.
    // =====================================================================

    public abstract class CompProperties_ElementalRacial : CompProperties
    {
    }

    public abstract class CompElementalRacial : ThingComp
    {
        protected Pawn Pawn => parent as Pawn;
    }

    // =====================================================================
    // IGNIS — flame aura. When a wild ignis lands a melee hit:
    //   * HUMANS            → extra tease damage, never fire (the wiki's "her flames
    //                         burn only when she wills it"; and a Momo's melee on a
    //                         human already deals tease, not physical harm — core
    //                         TeaseDamagePatch). We build the tease further.
    //   * everything else    → ignited (momos, animals, insects, mechanoids).
    // Driven by a postfix on the same Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget
    // the core tease patch hooks, gated to wild ignis attackers.
    // =====================================================================

    public class CompProperties_FlameAura : CompProperties_ElementalRacial
    {
        public CompProperties_FlameAura() { compClass = typeof(CompFlameAura); }

        /// <summary>Extra tease severity added per hit on a human victim.</summary>
        public float teasePerHit = 0.03f;

        /// <summary>Fire size rolled per hit on a non-human victim (AttachFire uses Rand.Range(0, fireSize)).</summary>
        public float fireSize = 0.35f;
    }

    public class CompFlameAura : CompElementalRacial
    {
        public CompProperties_FlameAura Props => (CompProperties_FlameAura)props;
    }

    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_IgnisFlameAura
    {
        private static GeneDef momoGene;

        /// <summary>True if the pawn has an active Momo gene (momos get burned, humans teased).</summary>
        private static bool IsMomoCarrier(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return false;
            }
            if (momoGene == null)
            {
                momoGene = DefDatabase<GeneDef>.GetNamedSilentFail("ProjectMomo_Momo");
            }
            if (momoGene == null)
            {
                return false;
            }
            Gene g = pawn.genes.GetGene(momoGene);
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
            // Only wild ignis carry the aura (a tamed ignis keeps her fire in check).
            if (attacker.def?.GetCompProperties<CompProperties_FlameAura>() == null)
            {
                return;
            }
            CompFlameAura aura = attacker.TryGetComp<CompFlameAura>();
            if (aura == null || victim.health?.hediffSet == null)
            {
                return;
            }

            if (victim.RaceProps != null && victim.RaceProps.Humanlike && !IsMomoCarrier(victim))
            {
                // Human (non-momo): tease only, never fire. The brain is where tease lives.
                BodyPartRecord brain = victim.health.hediffSet.GetBrain();
                if (brain == null)
                {
                    return;
                }
                float severity = aura.Props.teasePerHit;
                Hediff tease = victim.health.hediffSet.GetFirstHediffOfDef(ElementalDefOf.ProjectMomo_TeaseDamage);
                if (tease != null)
                {
                    tease.Severity = Mathf.Clamp(tease.Severity + severity, tease.def.minSeverity, tease.def.maxSeverity);
                }
                else
                {
                    tease = HediffMaker.MakeHediff(ElementalDefOf.ProjectMomo_TeaseDamage, victim, brain);
                    tease.Severity = Mathf.Clamp(severity, tease.def.minSeverity, tease.def.maxSeverity);
                    victim.health.AddHediff(tease, brain);
                }
            }
            else
            {
                // Momos, animals, insects, mechanoids: will the flames to burn.
                victim.TryAttachFire(aura.Props.fireSize, attacker);
            }
        }
    }

    // =====================================================================
    // GNOME — living stone. A gnome standing on natural stone/rough-hewn ground (or
    // under mountain roof) slowly knits her wounds: existing injuries heal a little
    // each interval. Comp ticks on a rare interval and shaves severity off injuries.
    // =====================================================================

    public class CompProperties_LivingStone : CompProperties_ElementalRacial
    {
        public CompProperties_LivingStone() { compClass = typeof(CompLivingStone); }

        /// <summary>Ticks between healing pulses (2500 = ~1 in-game hour).</summary>
        public int intervalTicks = 2500;

        /// <summary>Total injury severity healed per pulse, spread worst-first.</summary>
        public float severityPerPulse = 0.4f;
    }

    public class CompLivingStone : CompElementalRacial
    {
        public CompProperties_LivingStone Props => (CompProperties_LivingStone)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Map == null || pawn.health?.hediffSet == null)
            {
                return;
            }
            if (!OnStone(pawn))
            {
                return;
            }
            HealWorstInjury(pawn, Props.severityPerPulse);
        }

        private static TerrainAffordanceDef diggableAffordance;

        /// <summary>True if the gnome stands on rough/diggable ground or under mountain.</summary>
        private static bool OnStone(Pawn pawn)
        {
            // Under any thick (overhead-mountain) roof counts as deep in the earth.
            if (pawn.Position.Roofed(pawn.Map) &&
                pawn.Position.GetRoof(pawn.Map) == RoofDefOf.RoofRockThick)
            {
                return true;
            }
            // Rough, diggable natural ground (gravel, soil, sand, stone) — not smoothed
            // or built floors (those lack the Diggable affordance). Diggable is a real
            // affordance def but not a TerrainAffordanceDefOf member, so resolve by name.
            if (diggableAffordance == null)
            {
                diggableAffordance = DefDatabase<TerrainAffordanceDef>.GetNamedSilentFail("Diggable");
            }
            TerrainDef terrain = pawn.Position.GetTerrain(pawn.Map);
            return terrain != null && terrain.affordances != null &&
                   diggableAffordance != null && terrain.affordances.Contains(diggableAffordance);
        }

        /// <summary>Shave severity off the most-severe healing injury, worst-first.</summary>
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
    // SYLPH — caprice. A hidden mood that swings with the wind: the comp re-rolls a
    // whim on a daily interval and applies a matching memory thought (giddy / neutral /
    // foul) whose mood offset swings her Needs-tab mood. Capricious and free-spirited.
    // =====================================================================

    public class CompProperties_Caprice : CompProperties_ElementalRacial
    {
        public CompProperties_Caprice() { compClass = typeof(CompCaprice); }

        /// <summary>Hours between whim re-rolls.</summary>
        public float whimIntervalHours = 24f;

        /// <summary>Chance the whim lands foul or giddy (otherwise neutral/calm).</summary>
        public float foulChance = 0.3f;
        public float giddyChance = 0.3f;
    }

    public class CompCaprice : CompElementalRacial
    {
        private int nextWhimTick = -1;

        public CompProperties_Caprice Props => (CompProperties_Caprice)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            Pawn pawn = Pawn;
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

        /// <summary>Roll the day's whim and set the matching mood thought.</summary>
        private void ApplyWhim(Pawn pawn)
        {
            // Clear any prior caprice thought, then apply the new one (or none for a calm whim).
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
            // else: a calm whim — no thought, neutral mood.
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextWhimTick, "nextWhimTick", -1);
        }
    }

    // =====================================================================
    // UNDINE — wet-weather speed + water filth + slower filth rate.
    //   * Wet-weather speed: a MapComponent maintains a hidden hediff (with a
    //     MoveSpeed offset stage) on every undine while it rains.
    //   * Water filth, slower rate: prefix on Pawn_FilthTracker.Notify_EnteredNewCell
    //     (the slime-filth pattern) that drops Filth_Water at a lower rate than
    //     vanilla's terrain-filth/trash branches.
    // =====================================================================

    public class CompProperties_UndineWaterAffinity : CompProperties_ElementalRacial
    {
        public CompProperties_UndineWaterAffinity() { compClass = typeof(CompUndineWaterAffinity); }
    }

    public class CompUndineWaterAffinity : CompElementalRacial
    {
    }

    /// <summary>Applies/removes the wet-weather speed hediff on undines as the rain comes and goes.</summary>
    public class MapComponent_UndineWetSpeed : MapComponent
    {
        private const int CheckInterval = 500; // ~8 game-seconds between weather re-checks
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
                if (p?.def == null || p.def.GetCompProperties<CompProperties_UndineWaterAffinity>() == null)
                {
                    continue;
                }
                Hediff h = p.health?.hediffSet?.GetFirstHediffOfDef(ElementalDefOf.PMM_Hediff_UndineWetSpeed);
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

    /// <summary>MapComponents aren't def-registered; inject the undine wet-speed tracker on map init.</summary>
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

        /// <summary>Undines leave puddles, not trash — and at a slower rate than vanilla filth.</summary>
        public static bool Prefix(Pawn_FilthTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn?.def == null || !pawn.RaceProps.Humanlike ||
                pawn.def.GetCompProperties<CompProperties_UndineWaterAffinity>() == null)
            {
                return true; // not an undine: vanilla logic untouched
            }

            // Slower rate than vanilla's FilthRate * 0.5% (undines are tidy): a third of it.
            if (Rand.Value < pawn.GetStatValue(StatDefOf.FilthRate) * 0.005f * 0.33f)
            {
                var flags = (FilthSourceFlags)(AdditionalFlagsProp?.GetValue(__instance) ?? FilthSourceFlags.None);
                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_Water, 1, flags, true);
                NotifyHumanFilth?.Invoke(null, null);
            }
            return false; // handled; skip vanilla so no terrain filth or trash is dropped
        }
    }
}
