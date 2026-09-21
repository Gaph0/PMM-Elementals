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
    /// Def references resolved at startup. The eight wild pawn kinds and the eight custom
    /// races — everything the physiology patches need without a string lookup at call
    /// time. Backstory defs are fetched by name per kind (slime pattern) since they are
    /// looked up once per pawn generation, not per tick.
    ///
    /// The <b>xenotypes</b> live in ElementalXenotypeDefOf below instead, of necessity: a
    /// pawnkind and its xenotype deliberately share one defName (PMM_Elemental_Gnome), and
    /// a [DefOf] field binds by FIELD NAME, so two fields with that name cannot sit in one
    /// class. Two classes is also how vanilla splits PawnKindDefOf from XenotypeDefOf.
    /// </summary>
    [DefOf]
    public static class ElementalDefOf
    {
        public static PawnKindDef PMM_Elemental_Gnome;
        public static PawnKindDef PMM_Elemental_Ignis;
        public static PawnKindDef PMM_Elemental_Sylph;
        public static PawnKindDef PMM_Elemental_Undine;
        public static PawnKindDef PMM_Elemental_Dorome;
        public static PawnKindDef PMM_Elemental_Dryad;
        public static PawnKindDef PMM_Elemental_Apsara;
        public static PawnKindDef PMM_Elemental_Genie;

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

        // Phase-3 powers. Gnome living stone, sylph caprice and dryad photosynthesis need
        // no HediffDef of their own any more: their HediffComps sit on the race trackers
        // (see Race_ElementalMomo.xml).
        public static HediffDef PMM_Hediff_UndineWetSpeed;
        public static ThoughtDef PMM_Thought_SylphCapriceFoul;
        public static ThoughtDef PMM_Thought_SylphCapriceGiddy;
        // Apsara charm (the aura comp itself sits on her race tracker) + its thought.
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

    /// <summary>
    /// The eight elemental xenotypes, keyed by the same defNames as the kinds above.
    /// Split out of ElementalDefOf because a pawnkind and its xenotype share one defName
    /// and [DefOf] binds by field name — see the comment there. Nothing reads these yet;
    /// they exist so xenotype work (transformation, checks) has a resolved reference
    /// instead of a string lookup.
    /// </summary>
    [DefOf]
    public static class ElementalXenotypeDefOf
    {
        public static XenotypeDef PMM_Elemental_Gnome;
        public static XenotypeDef PMM_Elemental_Ignis;
        public static XenotypeDef PMM_Elemental_Sylph;
        public static XenotypeDef PMM_Elemental_Undine;
        public static XenotypeDef PMM_Elemental_Dorome;
        public static XenotypeDef PMM_Elemental_Dryad;
        public static XenotypeDef PMM_Elemental_Apsara;
        public static XenotypeDef PMM_Elemental_Genie;

        static ElementalXenotypeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ElementalXenotypeDefOf));
        }
    }
}
