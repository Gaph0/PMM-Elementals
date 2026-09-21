using HarmonyLib;
using RimWorld;
using Verse;

namespace PMM_Elementals
{
    /// <summary>The five wild elemental pawn kinds, shared by the wild-man patches.</summary>
    public static class ElementalKinds
    {
        public static bool IsElementalKind(PawnKindDef kind)
        {
            return kind == ElementalDefOf.PMM_Elemental_Gnome ||
                   kind == ElementalDefOf.PMM_Elemental_Ignis ||
                   kind == ElementalDefOf.PMM_Elemental_Sylph ||
                   kind == ElementalDefOf.PMM_Elemental_Undine ||
                   kind == ElementalDefOf.PMM_Elemental_Dorome ||
                   kind == ElementalDefOf.PMM_Elemental_Dryad ||
                   kind == ElementalDefOf.PMM_Elemental_Apsara ||
                   kind == ElementalDefOf.PMM_Elemental_Genie;
        }
    }

    /// <summary>
    /// Elementals are wild men: vanilla hardcodes <see cref="WildManUtility.IsWildMan"/> to
    /// the WildMan pawn kind, which drives taming (WorkGiver_Tame), arrest, wander-off
    /// behaviour and the wild-man labels. This postfix treats every elemental pawn kind the
    /// same way, so wild elementals can be tamed and convinced to join exactly like wild men.
    /// </summary>
    [HarmonyPatch(typeof(WildManUtility), nameof(WildManUtility.IsWildMan))]
    public static class Patch_ElementalIsWildMan
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (!__result && ElementalKinds.IsElementalKind(p?.kindDef) && !p.IsSubhuman)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Stop wild elementals from marching to the map edge and despawning. Vanilla
    /// <see cref="WildManUtility.WildManShouldReachOutsideNow"/> returns true for any
    /// wild man who hasn't yet "reached outside", which makes the pawn walk to the
    /// nearest edge. Report that wild elementals have already reached outside so the
    /// edge-walk never triggers and they wander the map until tamed.
    /// </summary>
    [HarmonyPatch(typeof(WildManUtility), nameof(WildManUtility.WildManShouldReachOutsideNow))]
    public static class Patch_ElementalShouldNotReachOutside
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (__result && ElementalKinds.IsElementalKind(p?.kindDef))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Every elemental is generated with its own life story. A postfix on
    /// <see cref="PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo"/> forces both
    /// backstory slots to the shared elemental childhood + the per-element adulthood
    /// after generation, so even callers that bypass the pawn kind's backstory filter
    /// cannot produce a different story.
    /// </summary>
    [HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    public static class Patch_ElementalBackstory
    {
        private static readonly AccessTools.FieldRef<Pawn_StoryTracker, BackstoryDef> ChildhoodRef =
            AccessTools.FieldRefAccess<Pawn_StoryTracker, BackstoryDef>("childhood");
        private static readonly AccessTools.FieldRef<Pawn_StoryTracker, BackstoryDef> AdulthoodRef =
            AccessTools.FieldRefAccess<Pawn_StoryTracker, BackstoryDef>("adulthood");

        public static void Postfix(Pawn pawn)
        {
            // Detect the elemental by its pawn KIND, not its genes: this postfix runs during
            // generation (GiveAppropriateBioAndNameTo), BEFORE the generator applies genes
            // (GenerateGenes), so the xenotype/skin genes are not yet active. The pawn kind
            // is set before the bio, so it is the reliable signal here.
            if (pawn?.story == null || !ElementalKinds.IsElementalKind(pawn.kindDef))
            {
                return;
            }

            BackstoryDef childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail("PMM_ElementalChild");
            // Each element gets its own adulthood (with its own skill flavour). The pawn
            // kind is the only reliable per-element signal at this point.
            string adulthoodName = AdulthoodNameFor(pawn.kindDef);
            BackstoryDef adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(adulthoodName);
            if (childhood != null)
            {
                ChildhoodRef(pawn.story) = childhood;
            }
            if (adulthood != null)
            {
                AdulthoodRef(pawn.story) = adulthood;
            }
        }

        /// <summary>The per-element adulthood defName for a given wild pawn kind.</summary>
        private static string AdulthoodNameFor(PawnKindDef kind)
        {
            if (kind == ElementalDefOf.PMM_Elemental_Gnome) return "PMM_GnomeAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Ignis) return "PMM_IgnisAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Sylph) return "PMM_SylphAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Undine) return "PMM_UndineAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Dorome) return "PMM_DoromeAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Dryad) return "PMM_DryadAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Apsara) return "PMM_ApsaraAdult";
            if (kind == ElementalDefOf.PMM_Elemental_Genie) return "PMM_GenieAdult";
            return "PMM_ElementalChild"; // unreachable; safe fallback
        }
    }

    /// <summary>
    /// Point a generated elemental at the race matching its rolled xenotype. The pawn
    /// kinds already pin the matching race in XML, so for incident spawns this is a
    /// no-op; it exists for callers that roll an elemental xenotype directly (dev tools,
    /// future incidents, other mods), so the pawn carries the correct custom race.
    /// Runs after genes are applied, so the xenotype is already set. A no-op for
    /// non-elemental pawns, so vanilla and other races are untouched.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
        new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_ElementalRaceByXenotype
    {
        public static void Postfix(Pawn __result)
        {
            ThingDef race = ElementalRaces.RaceFor(__result?.genes?.Xenotype?.defName);
            if (race != null && __result.def != race)
            {
                __result.def = race;
            }
        }
    }

    /// <summary>
    /// Maps an elemental xenotype to its custom race. Returns null for any non-elemental
    /// xenotype, so the race-swap postfix leaves other pawns untouched.
    /// </summary>
    public static class ElementalRaces
    {
        /// <summary>Race def for the given xenotype defName, or null if not an elemental.</summary>
        public static ThingDef RaceFor(string xenotypeDefName)
        {
            switch (xenotypeDefName)
            {
                case "PMM_Elemental_Gnome": return ElementalDefOf.PMM_Race_GnomeMomo;
                case "PMM_Elemental_Ignis": return ElementalDefOf.PMM_Race_IgnisMomo;
                case "PMM_Elemental_Sylph": return ElementalDefOf.PMM_Race_SylphMomo;
                case "PMM_Elemental_Undine": return ElementalDefOf.PMM_Race_UndineMomo;
                case "PMM_Elemental_Dorome": return ElementalDefOf.PMM_Race_DoromeMomo;
                case "PMM_Elemental_Dryad": return ElementalDefOf.PMM_Race_DryadMomo;
                case "PMM_Elemental_Apsara": return ElementalDefOf.PMM_Race_ApsaraMomo;
                case "PMM_Elemental_Genie": return ElementalDefOf.PMM_Race_GenieMomo;
                default: return null;
            }
        }
    }
}
