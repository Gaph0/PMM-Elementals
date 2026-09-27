using ProjectMamono;
using Verse;

namespace PMM_Elementals
{
    /// <summary>
    /// Puts the eight elemental mamono corpses under the family's shared "mamono corpses" line.
    ///
    /// The category def and all of the moving live in the core mod now
    /// (ProjectMamono.MamonoCorpses, Defs/ThingCategoryDefs/ThingCategories_MamonoCorpses.xml), so
    /// the insects, the slimes and the elementals share one line instead of one line each.
    /// See MamonoCorpses for why a corpse's category cannot be set in XML.
    ///
    /// The races come from ElementalDefOf rather than from strings, so renaming a race def
    /// fails the build instead of quietly dropping that race off the line.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ElementalMamonoCorpses
    {
        static ElementalMamonoCorpses()
        {
            MamonoCorpses.Register(
                ElementalDefOf.PMM_Race_GnomeMamono.defName,
                ElementalDefOf.PMM_Race_IgnisMamono.defName,
                ElementalDefOf.PMM_Race_SylphMamono.defName,
                ElementalDefOf.PMM_Race_UndineMamono.defName,
                ElementalDefOf.PMM_Race_DoromeMamono.defName,
                ElementalDefOf.PMM_Race_DryadMamono.defName,
                ElementalDefOf.PMM_Race_ApsaraMamono.defName,
                ElementalDefOf.PMM_Race_GenieMamono.defName);
        }
    }
}
