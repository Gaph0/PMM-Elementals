using ProjectMomo;
using Verse;

namespace PMM_Elementals
{
    /// <summary>
    /// Puts the eight elemental momo corpses under the family's shared "momo corpses" line.
    ///
    /// The category def and all of the moving live in the core mod now
    /// (ProjectMomo.MomoCorpses, Defs/ThingCategoryDefs/ThingCategories_MomoCorpses.xml), so
    /// the insects, the slimes and the elementals share one line instead of one line each.
    /// See MomoCorpses for why a corpse's category cannot be set in XML.
    ///
    /// The races come from ElementalDefOf rather than from strings, so renaming a race def
    /// fails the build instead of quietly dropping that race off the line.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ElementalMomoCorpses
    {
        static ElementalMomoCorpses()
        {
            MomoCorpses.Register(
                ElementalDefOf.PMM_Race_GnomeMomo.defName,
                ElementalDefOf.PMM_Race_IgnisMomo.defName,
                ElementalDefOf.PMM_Race_SylphMomo.defName,
                ElementalDefOf.PMM_Race_UndineMomo.defName,
                ElementalDefOf.PMM_Race_DoromeMomo.defName,
                ElementalDefOf.PMM_Race_DryadMomo.defName,
                ElementalDefOf.PMM_Race_ApsaraMomo.defName,
                ElementalDefOf.PMM_Race_GenieMomo.defName);
        }
    }
}
