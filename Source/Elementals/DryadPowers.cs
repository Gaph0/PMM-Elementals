using RimWorld;
using UnityEngine;
using Verse;

namespace PMM_Elementals
{
    // =====================================================================
    // DRYAD - photosynthesis (locked D15). A hidden marker hediff (granted by
    // Patch_GrantElementalMarkers, the gnome/sylph pattern) carries this ticking
    // HediffComp, which refills her food, rest and mana while she stands unroofed in
    // real sunlight. Race comps never instantiate on a pawn (the phase-3a architecture
    // note), so the power lives on a hediff - the slime mod's jelly-ooze mechanism.
    //
    // Her green thumb (locked D16) is NOT here: PlantWorkSpeed 1.4 / PlantHarvestYield
    // 1.25 are pawn-side stat bases on PMM_Race_DryadMamono - pure XML, no C# surface.
    // =====================================================================

    public class HediffCompProperties_Photosynthesis : HediffCompProperties
    {
        public HediffCompProperties_Photosynthesis() { compClass = typeof(HediffComp_Photosynthesis); }

        /// <summary>How often the refill pulses (ticks).</summary>
        public int intervalTicks = 250;
        /// <summary>Refill per pulse: 2x the natural need fall rate x intervalTicks (D15).</summary>
        public float foodPerPulse = 0.0133f;
        public float restPerPulse = 0.0079f;
        public float manaPerPulse = 0.0042f;
        /// <summary>Sky glow at/above which she photosynthesises (1.0 full day, ~0 night).</summary>
        public float skyGlowThreshold = 0.5f;
    }

    public class HediffComp_Photosynthesis : HediffComp
    {
        public HediffCompProperties_Photosynthesis Props => (HediffCompProperties_Photosynthesis)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || pawn.needs == null)
            {
                return;
            }
            if (!pawn.IsHashIntervalTick(Props.intervalTicks, delta))
            {
                return;
            }
            if (!InSunlight(pawn))
            {
                return;
            }
            Refill(pawn);
        }

        /// <summary>
        /// Real sky light only: spawned, unroofed, and the sky bright enough. Roofed
        /// cells, night, eclipses and volcanic winter all starve her - a dryad kept in
        /// the dark must eat, sleep and feed on mana like anyone else. Sun lamps do not
        /// count: they feed the GlowGrid, not CurSkyGlow.
        /// </summary>
        private bool InSunlight(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
            {
                return false;
            }
            if (pawn.Position.Roofed(map))
            {
                return false;
            }
            return map.skyManager.CurSkyGlow >= Props.skyGlowThreshold;
        }

        private void Refill(Pawn pawn)
        {
            if (pawn.needs.food != null)
            {
                pawn.needs.food.CurLevel = Mathf.Min(1f, pawn.needs.food.CurLevel + Props.foodPerPulse);
            }
            if (pawn.needs.rest != null)
            {
                pawn.needs.rest.CurLevel = Mathf.Min(1f, pawn.needs.rest.CurLevel + Props.restPerPulse);
            }
            // Cross-mod def (PMM.Core's mana need), resolved by name in ElementalDefOf
            // like ProjectMamono_TeaseDamage. Wild dryads photosynthesise too: their mana
            // drain is already reduced by PMM.Core's WildManaDrainFactor, and the sunlit
            // refill stacks on top.
            if (ElementalDefOf.ProjectMamono_Mana != null)
            {
                Need mana = pawn.needs.TryGetNeed(ElementalDefOf.ProjectMamono_Mana);
                if (mana != null)
                {
                    mana.CurLevel = Mathf.Min(1f, mana.CurLevel + Props.manaPerPulse);
                }
            }
        }
    }
}
