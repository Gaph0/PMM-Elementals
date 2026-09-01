using HarmonyLib;
using RimWorld;
using Verse;

namespace PMM_Elementals
{
    /// <summary>
    /// Entry point. Applies Harmony patches when the mod loads.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ElementalMod
    {
        static ElementalMod()
        {
            var harmony = new Harmony("PMM.Elementals");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }
    }

    /// <summary>
    /// Def references resolved at startup. The four wild pawn kinds, the four xenotypes
    /// and the four custom races — everything the physiology patches need without a
    /// string lookup at call time. Backstory defs are fetched by name per kind (slime
    /// pattern) since they are looked up once per pawn generation, not per tick.
    /// </summary>
    [DefOf]
    public static class ElementalDefOf
    {
        public static PawnKindDef PMM_GnomeWild;
        public static PawnKindDef PMM_IgnisWild;
        public static PawnKindDef PMM_SylphWild;
        public static PawnKindDef PMM_UndineWild;
        public static PawnKindDef PMM_DoromeWild;
        public static PawnKindDef PMM_DryadWild;
        public static PawnKindDef PMM_ApsaraWild;
        public static PawnKindDef PMM_GenieWild;

        public static XenotypeDef PMM_ElementalGnome;
        public static XenotypeDef PMM_ElementalIgnis;
        public static XenotypeDef PMM_ElementalSylph;
        public static XenotypeDef PMM_ElementalUndine;
        public static XenotypeDef PMM_ElementalDorome;
        public static XenotypeDef PMM_ElementalDryad;
        public static XenotypeDef PMM_ElementalApsara;
        public static XenotypeDef PMM_ElementalGenie;

        public static ThingDef PMM_Race_GnomeMomo;
        public static ThingDef PMM_Race_IgnisMomo;
        public static ThingDef PMM_Race_SylphMomo;
        public static ThingDef PMM_Race_UndineMomo;
        public static ThingDef PMM_Race_DoromeMomo;
        public static ThingDef PMM_Race_DryadMomo;
        public static ThingDef PMM_Race_ApsaraMomo;
        public static ThingDef PMM_Race_GenieMomo;

        // Dorome powers (filth + tamed-only mud-merge ability).
        public static ThingDef PMM_Filth_Mud;
        public static AbilityDef PMM_Ability_DoromeMudMerge;

        // Genie (lamp item, wish ability, lamp bond, freedom timer, join-offer letter).
        public static ThingDef PMM_Item_OldDustyLamp;
        public static AbilityDef PMM_Ability_GenieWish;
        public static HediffDef PMM_Hediff_GenieBond;
        public static HediffDef PMM_Hediff_GenieFreedom;
        public static LetterDef PMM_Letter_GenieOffer;

        // Phase-3 powers.
        public static HediffDef PMM_Hediff_GnomeLivingStone;
        public static HediffDef PMM_Hediff_SylphCaprice;
        public static HediffDef PMM_Hediff_UndineWetSpeed;
        public static HediffDef PMM_Hediff_DryadPhotosynthesis;
        public static ThoughtDef PMM_Thought_SylphCapriceFoul;
        public static ThoughtDef PMM_Thought_SylphCapriceGiddy;
        // Apsara aura (marker + exposure) and charm thought.
        public static HediffDef PMM_Hediff_ApsaraAllure;
        public static HediffDef PMM_Hediff_ApsaraCharm;
        public static ThoughtDef PMM_Thought_ApsaraCharm;
        // Resolved from Project Momo core (cross-mod def; assigned by name, not DefOf).
        public static HediffDef ProjectMomo_TeaseDamage;
        public static NeedDef ProjectMomo_Mana;
        public static PawnRelationDef ProjectMomo_Tsugai;

        static ElementalDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ElementalDefOf));
            ProjectMomo_TeaseDamage = DefDatabase<HediffDef>.GetNamedSilentFail("ProjectMomo_TeaseDamage");
            ProjectMomo_Mana = DefDatabase<NeedDef>.GetNamedSilentFail("ProjectMomo_Mana");
            ProjectMomo_Tsugai = DefDatabase<PawnRelationDef>.GetNamedSilentFail("ProjectMomo_Tsugai");
        }
    }
}
