using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PMM_Elementals
{
    // =====================================================================
    // APSARA — joy elemental. Four mechanics, all locked D-A1–D-A6:
    //
    //   1. Allure aura (this file's HediffComp_ApsaraAura, on her race tracker):
    //      map-wide (D-A1), every 600 ticks refreshes the PMM_Hediff_ApsaraCharm
    //      exposure hediff on every non-hostile humanlike and tops up their joy need
    //      (D-A3). The charm's LOVIN' effect is pure XML — vanilla
    //      HediffComp_GiveLovinMTBFactor (D-A2 0.6) scales the post-lovin' cooldown in
    //      JobDriver_Lovin.GenerateRandomMinTicksToNextLovin, and its HediffComp_Disappears
    //      fades it ~6h off-aura (PleasurePulse pattern). No code needed for either.
    //   2. Charm mood thought: ThoughtWorker_ApsaraCharmPresence drives the +3
    //      PMM_Thought_ApsaraCharm situational thought while a pawn carries the charm
    //      (the radius-limited, hediff-driven analogue of the joybringer's JoyousPresence).
    //   3. Party MTB: Patch_ApsaraPartyMtb adds one bonus gathering roll while an apsara
    //      is present (D-A4).
    //   4. Romance compatibility: Patch_ApsaraRomanceAura scales
    //      SecondaryRomanceChanceFactor ×1.25 map-wide while an apsara is present (D-A5).
    //
    // One MapComponent_ApsaraPresence cache serves patches 3 and 4 (D-A1: a single
    // HasApsara flag, no radius anywhere).
    // =====================================================================

    // ---------------------------------------------------------------------
    // 1. The aura comp.
    // ---------------------------------------------------------------------

    public class HediffCompProperties_ApsaraAura : HediffCompProperties
    {
        public HediffCompProperties_ApsaraAura() { compClass = typeof(HediffComp_ApsaraAura); }

        /// <summary>Ticks between aura pulses. 600 = 10 s real time.</summary>
        public int intervalTicks = 600;

        /// <summary>Joy need added per pulse (D-A3). +0.05 / 600 ticks ≈ +0.5 need/day at full exposure.</summary>
        public float joyPerPulse = 0.05f;
    }

    /// <summary>
    /// Ticks on the apsara's race tracker. Every interval, sweeps the map
    /// (D-A1: map-wide — no radius check) and, for every non-hostile humanlike on the
    /// same map, refreshes the charm hediff and tops up the joy need. A pawn who leaves
    /// the aura keeps its charm until the hediff's own HediffComp_Disappears fades it —
    /// PleasurePulse semantics, no removal pass. Multiple apsaras refresh the same charm
    /// idempotently, so the aura never stacks.
    /// </summary>
    public class HediffComp_ApsaraAura : HediffComp
    {
        public HediffCompProperties_ApsaraAura Props => (HediffCompProperties_ApsaraAura)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn apsara = parent?.pawn;
            if (apsara == null || apsara.Dead || apsara.Map == null || !apsara.Spawned)
            {
                return;
            }
            if (!apsara.IsHashIntervalTick(Props.intervalTicks, delta))
            {
                return;
            }

            Map map = apsara.Map;
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == apsara || p.Dead)
                {
                    continue;
                }
                if (p.RaceProps == null || !p.RaceProps.Humanlike || p.health?.hediffSet == null)
                {
                    continue;
                }
                if (p.HostileTo(apsara))
                {
                    continue;
                }

                RefreshCharm(p);

                // Literal joy (D-A3): JoyGainFactor is Building-category, so the aura tops
                // the need up directly. Social joy kind (she's a people-person dancer).
                if (p.needs?.joy != null)
                {
                    p.needs.joy.GainJoy(Props.joyPerPulse, JoyKindDefOf.Social);
                }
            }
        }

        /// <summary>Ensure the pawn carries the charm; add it fresh if absent (which also
        /// restarts its Disappears timer). If present, do nothing — the aura pulse keeps it
        /// alive by re-adding only on expiry, and the comp's refresh is idempotent.</summary>
        private static void RefreshCharm(Pawn p)
        {
            HediffDef charmDef = ElementalDefOf.PMM_Hediff_ApsaraCharm;
            if (charmDef == null || p.health.hediffSet.GetFirstHediffOfDef(charmDef) != null)
            {
                return; // already charmed
            }
            Hediff charm = HediffMaker.MakeHediff(charmDef, p);
            charm.Severity = 1f;
            p.health.AddHediff(charm);
        }
    }

    // ---------------------------------------------------------------------
    // 2. Charm mood thought worker (+3 while charmed).
    // ---------------------------------------------------------------------

    /// <summary>Active while the pawn carries the apsara's charm hediff. Drives the +3
    /// situational PMM_Thought_ApsaraCharm (the joybringer-analogue mood lift).</summary>
    public class ThoughtWorker_ApsaraCharmPresence : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            HediffDef charmDef = ElementalDefOf.PMM_Hediff_ApsaraCharm;
            if (charmDef == null || p?.health?.hediffSet == null)
            {
                return ThoughtState.Inactive;
            }
            return p.health.hediffSet.GetFirstHediffOfDef(charmDef) != null
                ? ThoughtState.ActiveAtStage(0)
                : ThoughtState.Inactive;
        }
    }

    // ---------------------------------------------------------------------
    // Presence cache: one map-wide "is an apsara here" flag for the two patches below.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Rescans the map's pawns every 2500 ticks and exposes <see cref="HasApsara"/>. Lets
    /// the party and romance patches be O(1) per call instead of scanning pawns themselves.
    /// Injected onto every map by Patch_AddApsaraPresenceComponent (undine wet-speed pattern).
    /// </summary>
    public class MapComponent_ApsaraPresence : MapComponent
    {
        private const int RescanInterval = 2500;
        private int nextRescanTick = -1;
        private bool hasApsara;

        public MapComponent_ApsaraPresence(Map map) : base(map) { }

        public bool HasApsara
        {
            get
            {
                if (Find.TickManager.TicksGame >= nextRescanTick)
                {
                    Rescan();
                }
                return hasApsara;
            }
        }

        private void Rescan()
        {
            nextRescanTick = Find.TickManager.TicksGame + RescanInterval;
            hasApsara = false;
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && ElementalXenotypes.IsApsara(p))
                {
                    hasApsara = true;
                    return;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextRescanTick, "apsaraPresenceNextRescan", -1);
            Scribe_Values.Look(ref hasApsara, "apsaraPresenceHas", false);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.ConstructComponents))]
    public static class Patch_AddApsaraPresenceComponent
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<MapComponent_ApsaraPresence>() == null)
            {
                __instance.components.Add(new MapComponent_ApsaraPresence(__instance));
            }
        }
    }

    // ---------------------------------------------------------------------
    // 3. Party MTB (D-A4). Vanilla: VoluntarilyJoinableLordsStarter ticks every 5000
    //    ticks, rolls MTBEventOccurs(40d) → startRandomGatheringASAP, then a 10-day
    //    floor (lastLordStartTick). We add ONE bonus roll (MTB 120d) while an apsara
    //    is present ≈ +33 % gatherings, still capped by the vanilla floor. Parties are
    //    map-global (no per-pawn party MTB), so "higher for these pawns" = "more parties
    //    for the colony while she's around".
    // ---------------------------------------------------------------------

    [HarmonyPatch(typeof(VoluntarilyJoinableLordsStarter), "Tick_TryStartRandomGathering")]
    public static class Patch_ApsaraPartyMtb
    {
        // Bonus roll: MTB 120 days, on the same 5000-tick cadence as the vanilla call.
        private const float BonusMtbDays = 120f;
        private const float CheckDuration = 5000f;

        private static readonly AccessTools.FieldRef<VoluntarilyJoinableLordsStarter, bool> StartAsapRef =
            AccessTools.FieldRefAccess<VoluntarilyJoinableLordsStarter, bool>("startRandomGatheringASAP");
        private static readonly AccessTools.FieldRef<VoluntarilyJoinableLordsStarter, Map> MapRef =
            AccessTools.FieldRefAccess<VoluntarilyJoinableLordsStarter, Map>("map");

        public static void Postfix(VoluntarilyJoinableLordsStarter __instance)
        {
            // Only the map-level random-gathering tick; fires every 5000 ticks (the caller's
            // cadence), so this postfix is already throttled — no extra timing needed.
            Map map = MapRef(__instance);
            if (map?.GetComponent<MapComponent_ApsaraPresence>()?.HasApsara != true)
            {
                return;
            }
            if (Rand.MTBEventOccurs(BonusMtbDays, GenDate.TicksPerDay, CheckDuration))
            {
                StartAsapRef(__instance) = true;
            }
        }
    }

    // ---------------------------------------------------------------------
    // 4. Romance compatibility (D-A5). Both romance-attempt frequency and success chance
    //    flow through Pawn_RelationsTracker.SecondaryRomanceChanceFactor, which returns 0
    //    for orientation/relation mismatches. A multiplicative ×1.25 raises both while an
    //    apsara is on the map — and because it's multiplicative, zero stays zero (she
    //    amplifies existing compatibility; she doesn't pair the unwilling). There is no
    //    vanilla stat/hediff lever for romance (unlike lovin'), so this postfix is the
    //    minimal clean route.
    // ---------------------------------------------------------------------

    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
    public static class Patch_ApsaraRomanceAura
    {
        private const float Factor = 1.25f;

        private static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

        public static void Postfix(Pawn_RelationsTracker __instance, ref float __result)
        {
            if (__result <= 0f)
            {
                return; // preserve orientation/relation zeros
            }
            Pawn pawn = PawnRef(__instance);
            if (pawn?.MapHeld?.GetComponent<MapComponent_ApsaraPresence>()?.HasApsara == true)
            {
                __result *= Factor;
            }
        }
    }
}
