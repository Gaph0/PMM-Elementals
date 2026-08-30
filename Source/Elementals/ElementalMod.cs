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

        public static XenotypeDef PMM_ElementalGnome;
        public static XenotypeDef PMM_ElementalIgnis;
        public static XenotypeDef PMM_ElementalSylph;
        public static XenotypeDef PMM_ElementalUndine;

        public static ThingDef PMM_Race_GnomeMomo;
        public static ThingDef PMM_Race_IgnisMomo;
        public static ThingDef PMM_Race_SylphMomo;
        public static ThingDef PMM_Race_UndineMomo;

        // Phase-3 powers.
        public static HediffDef PMM_Hediff_UndineWetSpeed;
        public static ThoughtDef PMM_Thought_SylphCapriceFoul;
        public static ThoughtDef PMM_Thought_SylphCapriceGiddy;
        // Resolved from Project Momo core (cross-mod def; assigned by name, not DefOf).
        public static HediffDef ProjectMomo_TeaseDamage;

        static ElementalDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ElementalDefOf));
            ProjectMomo_TeaseDamage = DefDatabase<HediffDef>.GetNamedSilentFail("ProjectMomo_TeaseDamage");
        }
    }
}
