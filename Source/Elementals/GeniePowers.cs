using System.Collections.Generic;
using HarmonyLib;
using ProjectMomo;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PMM_Elementals
{
    // =====================================================================
    // GENIE — the lamp elemental (eighth elemental).
    //
    // Flow (no wander-in for her — she enters through her lamp):
    //   1. IncidentWorker_GenieLampFound drops an Old Dusty Lamp item on the
    //      map — Desert, AridShrubland and ExtremeDesert biomes only.
    //   2. A colonist right-clicks the lamp → "Clean the lamp" (the vanilla
    //      CompUsable + UseItem job, useLabel in XML). CompUseEffect_GenieLamp
    //      fires at the end of the cleaning: the genie swirls out of the smoke
    //      and a ChoiceLetter_GenieOffer asks the player to welcome her or send
    //      her away. Accept → she joins the colony; reject → she vanishes.
    //   3. THE BOND IS THE TSUGAI BOND (user ruling 2026-09-01): the genie has
    //      no master of her own — only a man she forms a core-mod tsugai bond
    //      with can receive her wishes. If the lamp cleaner passes
    //      TsugaiFormation.CanBond (adult, male, unattached-or-Protagonist), the
    //      letter promises the bond and Accept forms a full voluntary tsugai
    //      bond with him; a woman or already-bonded man frees an UNBOUND genie
    //      who can bond a colonist later via the core proposal system. The wish
    //      resolves her master live from the tsugai relation, so it follows
    //      re-bonds and locks again when her husband dies.
    //   4. The wish: PMM_Ability_GenieWish, granted by PMM_Gene_GenieWish (the
    //      Dorome mud-merge pattern — a gene-granted ability, the vanilla
    //      ability system runs the one-year cooldownTicksRange). Targets pawns
    //      but only ever applies to her bonded husband, granting in priority
    //      order: regrow the biggest missing limb → erase a permanent scar →
    //      lift the biggest temporary negative memory. Requires 95% mana and
    //      drains it all on cast (the HemogenCost gizmo-gate pattern).
    // =====================================================================

    // =====================================================================
    // 1. LAMP ARRIVAL — desert-biome-gated incident that drops the lamp item.
    //    Same "all gating lives in C#" ruling as the four wander-ins: the def
    //    sets no allowedBiomes, the worker checks the world tile's biome.
    // =====================================================================

    /// <summary>A strange lamp turns up. Fires only on desert maps (Desert,
    /// AridShrubland, ExtremeDesert) and drops the Old Dusty Lamp item.</summary>
    public class IncidentWorker_GenieLampFound : IncidentWorker
    {
        /// <summary>The three arid biomes a lamp-riding genie calls home.</summary>
        private static readonly HashSet<string> LampBiomes = new HashSet<string>
        {
            "Desert",
            "AridShrubland",
            "ExtremeDesert",
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            if (!(parms.target is Map map))
            {
                return false;
            }
            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile?.PrimaryBiome == null || !LampBiomes.Contains(tile.PrimaryBiome.defName))
            {
                PMMLog.Message($"[PMM_Elementals] genie lamp blocked: biome {tile?.PrimaryBiome?.defName ?? "null"} is not desert, arid shrubland or extreme desert");
                return false;
            }
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // Half-buried in the sand: outdoors, standable, unfogged and reachable
            // so the player can actually find and haul to it.
            if (!CellFinderLoose.TryGetRandomCellWith(
                    c => c.Standable(map) && !c.Roofed(map) && !c.Fogged(map) &&
                         map.reachability.CanReachColony(c),
                    map, 1000, out IntVec3 cell))
            {
                return false;
            }
            Thing lamp = ThingMaker.MakeThing(ElementalDefOf.PMM_Item_OldDustyLamp);
            GenSpawn.Spawn(lamp, cell, map);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, lamp);
            return true;
        }
    }

    // =====================================================================
    // 2. CLEAN THE LAMP — the use-effect that frees the genie.
    // =====================================================================

    /// <summary>
    /// Fired when a colonist finishes the "Clean the lamp" use job: spawns the
    /// genie at the lamp and pops the join-offer letter. If the cleaner is a
    /// man she can tsugai-bond (the core mod's CanBond rules), the letter
    /// promises that bond; anyone else frees an unbound genie. The lamp itself
    /// is removed by the paired <see cref="CompUseEffect_DestroySelf"/> comp
    /// (it orders last).
    /// </summary>
    public class CompUseEffect_GenieLamp : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            Map map = parent.MapHeld ?? usedBy?.Map;
            if (map == null || usedBy == null)
            {
                return;
            }
            IntVec3 cell = parent.PositionHeld;
            if (!cell.IsValid || !cell.InBounds(map))
            {
                cell = usedBy.Position;
            }
            Pawn genie = GenieSpawner.SpawnGenie(map, cell);
            if (genie == null)
            {
                return;
            }

            // The tsugai gate: only a bondable man (adult, male, humanlike,
            // non-momo, unattached or a Protagonist) can become her master.
            // A woman — or another momo's husband — frees her unbound.
            bool bondable = TsugaiFormation.CanBond(genie, usedBy);

            ChoiceLetter_GenieOffer letter =
                (ChoiceLetter_GenieOffer)LetterMaker.MakeLetter(ElementalDefOf.PMM_Letter_GenieOffer);
            letter.genie = genie;
            letter.master = bondable ? usedBy : null;
            letter.Label = "PMM_GenieOffer_Label".Translate();
            letter.Text = bondable
                ? "PMM_GenieOffer_Text_Bonded".Translate(genie.Named("GENIE"), usedBy.Named("CLEANER"))
                : "PMM_GenieOffer_Text_Unbound".Translate(genie.Named("GENIE"), usedBy.Named("CLEANER"));
            letter.lookTargets = new LookTargets(genie);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }

    // =====================================================================
    // 3. GENIE SPAWNING — generation + tsugai resolution. Mirrors the wander-in
    //    spawn flow (factionless, wild-man-marked so she wanders harmlessly
    //    while the offer letter waits) but she steps out of the lamp, not the
    //    map edge. Her master is never stored here — BondedMaster reads the
    //    core mod's tsugai relation live.
    // =====================================================================

    public static class GenieSpawner
    {
        /// <summary>
        /// Generate the genie and spawn her at (or beside) the lamp. She is
        /// always UNBOUND here — the tsugai bond with an eligible cleaner forms
        /// only when the player accepts the offer letter, so rejecting her never
        /// strands a bond on a destroyed pawn. Returns null if no walkable cell
        /// could be found.
        /// </summary>
        public static Pawn SpawnGenie(Map map, IntVec3 cell)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                ElementalDefOf.PMM_Elemental_Genie, null, PawnGenerationContext.NonPlayer, map.Tile,
                forceGenerateNewPawn: false, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true,
                inhabitant: false, certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f, biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null, relationWithExtraPawnChanceFactor: 1f,
                validatorPreGear: null, validatorPostGear: null, forcedTraits: null,
                prohibitedTraits: null, minChanceToRedressWorldPawn: null,
                fixedBiologicalAge: null, fixedChronologicalAge: null, fixedGender: null,
                fixedLastName: null, fixedBirthName: null, fixedTitle: null,
                fixedIdeo: null, forceNoIdeo: false, forceNoBackstory: false,
                forbidAnyTitle: false, forceDead: false, forcedXenogenes: null,
                forcedEndogenes: null, forcedXenotype: null, forcedCustomXenotype: null,
                allowedXenotypes: null, forceBaselinerChance: 0f,
                developmentalStages: DevelopmentalStage.Adult,
                forceNoGear: false));

            // Factionless like the wander-ins: SetFaction warns when the faction is
            // unchanged, so only clear it if generation actually assigned one.
            if (pawn.Faction != null)
            {
                pawn.SetFaction(null, null);
            }

            // She pours out of the lamp: its cell if walkable, else beside it.
            IntVec3 spawnCell = cell;
            if (!spawnCell.Standable(map) || spawnCell.GetFirstPawn(map) != null)
            {
                spawnCell = CellFinder.RandomClosewalkCellNear(cell, map, 4,
                    c => c.GetFirstPawn(map) == null);
            }
            if (!spawnCell.IsValid || !spawnCell.Standable(map))
            {
                pawn.Discard();
                return null;
            }
            GenSpawn.Spawn(pawn, spawnCell, map);

            // Wander idly by the lamp until the player answers the offer (the
            // wild-man mark, same as the wander-ins — never marches off the map).
            if (pawn.mindState != null)
            {
                pawn.mindState.WildManEverReachedOutside = true;
            }

            FleckMaker.ThrowSmoke(spawnCell.ToVector3Shifted(), map, 2.5f);
            return pawn;
        }

        /// <summary>
        /// The genie's master: her living tsugai husband, resolved straight from
        /// the core mod's relation. Null while she is unbound — the wish gates
        /// on this, so it unlocks when she bonds and locks again when he dies.
        /// </summary>
        public static Pawn BondedMaster(Pawn genie)
        {
            if (genie?.relations == null || ElementalDefOf.ProjectMomo_Tsugai == null)
            {
                return null;
            }
            return genie.relations.GetFirstDirectRelationPawn(
                ElementalDefOf.ProjectMomo_Tsugai, p => p != null && !p.Dead);
        }

        /// <summary>True once her freedom timer (PMM_Hediff_GenieFreedom) has run
        /// out — the moment she stops wandering and heads for the map edge.</summary>
        public static bool FreedomSpent(Pawn genie)
        {
            if (genie?.health?.hediffSet == null || ElementalDefOf.PMM_Hediff_GenieFreedom == null)
            {
                return false;
            }
            Hediff h = genie.health.hediffSet.GetFirstHediffOfDef(ElementalDefOf.PMM_Hediff_GenieFreedom);
            return h?.TryGetComp<HediffComp_GenieFreedom>()?.Spent ?? false;
        }
    }

    // =====================================================================
    // 4. JOIN OFFER — the accept/reject letter. Vanilla's ChoiceLetter_AcceptJoiner
    //    runs on quest signal plumbing, so this is a self-contained subclass
    //    whose DiaOptions act directly. Scribed refs keep it save-safe.
    // =====================================================================

    public class ChoiceLetter_GenieOffer : ChoiceLetter
    {
        public Pawn genie;
        /// <summary>The intended master: the lamp cleaner, but ONLY when he is a
        /// bondable man (set at clean time; null for any other cleaner).</summary>
        public Pawn master;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                }
                else
                {
                    DiaOption accept = new DiaOption("PMM_GenieOffer_Accept".Translate())
                    {
                        action = Accept,
                        resolveTree = true,
                    };
                    yield return accept;

                    DiaOption reject = new DiaOption("PMM_GenieOffer_Reject".Translate())
                    {
                        action = Reject,
                        resolveTree = true,
                    };
                    yield return reject;

                    if (lookTargets.IsValid)
                    {
                        yield return Option_JumpToLocationAndPostpone;
                    }
                }
            }
        }

        /// <summary>
        /// Welcome her: she joins the colony, and if her rescuer is still
        /// bondable she seals the promised tsugai bond with him (re-checked now
        /// — he may have died or bonded someone else while the letter waited).
        /// A no-op if she somehow already joined.
        /// </summary>
        private void Accept()
        {
            if (genie == null || genie.Dead || !genie.Spawned || genie.Faction == Faction.OfPlayer)
            {
                return;
            }
            // The vanilla recruit path flips a wild pawn to colonist cleanly —
            // the core mod's ExecuteJoin learned that a plain SetFaction can
            // leave a wild/ex-guest pawn in a broken guest state.
            RecruitUtility.Recruit(genie, Faction.OfPlayer);

            // Recruit FIRST: TryBond's wild-bond outcome auto-tames a factionless
            // wild momo at 100%, which would fight this letter. As a colonist the
            // outcome early-returns and only the bond (relation, hediffs, essence,
            // thoughts) forms.
            if (master != null && TsugaiFormation.CanBond(genie, master))
            {
                TsugaiFormation.TryBond(genie, master, voluntary: true);
            }

            // She is staying — her freedom is spent. Drop the countdown so the
            // colony genie never shows "leaves in …" again (the exit job giver
            // also faction-checks, so this is belt-and-braces).
            if (ElementalDefOf.PMM_Hediff_GenieFreedom != null)
            {
                Hediff freedom = genie.health?.hediffSet?.GetFirstHediffOfDef(ElementalDefOf.PMM_Hediff_GenieFreedom);
                if (freedom != null)
                {
                    genie.health.RemoveHediff(freedom);
                }
            }

            // Report the FINAL bond state: if she bonded someone else on her own
            // while the letter waited, he is her master now.
            Pawn husband = GenieSpawner.BondedMaster(genie);
            Messages.Message(
                husband != null
                    ? "PMM_GenieJoined_Bonded".Translate(genie.Named("PAWN"), husband.Named("MASTER"))
                    : "PMM_GenieJoined_Unbound".Translate(genie.Named("PAWN")),
                genie, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>Send her away: she dissolves back into smoke and is gone for good.
        /// Never fires once she is a colonist (e.g. tamed while the letter waited).</summary>
        private void Reject()
        {
            if (genie == null || !genie.Spawned || genie.Faction == Faction.OfPlayer)
            {
                return;
            }
            Map map = genie.Map;
            IntVec3 cell = genie.Position;
            genie.DeSpawn();
            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 3f);
            genie.Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref genie, "genie");
            Scribe_References.Look(ref master, "master");
        }
    }

    // =====================================================================
    // 5. FREEDOM TIMER (user request 2026-09-01): a genie fresh from the lamp
    //    no longer beelines for the map edge. PMM_Hediff_GenieFreedom (granted
    //    with the other xenotype markers in Patch_GrantElementalMarkers) counts
    //    down her few hours of freedom; while it runs, her think-tree branch
    //    (PMM_ElementalWildBehavior) gives her the wild-man wander. When it runs
    //    out, ThinkNode_ConditionalGenieFreedomSpent opens the vanilla
    //    JobGiver_ExitMapBest and she walks off to enjoy it — unless the player
    //    welcomed her first (Accept drops the hediff; the node also ignores
    //    player-faction genies).
    // =====================================================================

    public class HediffCompProperties_GenieFreedom : HediffCompProperties
    {
        public HediffCompProperties_GenieFreedom() { compClass = typeof(HediffComp_GenieFreedom); }
        /// <summary>How long she savors her freedom before leaving (10000 = 4 in-game hours).</summary>
        public int freedomTicks = 10000;
    }

    public class HediffComp_GenieFreedom : HediffComp
    {
        private int leaveTick = -1;

        public HediffCompProperties_GenieFreedom Props => (HediffCompProperties_GenieFreedom)props;

        /// <summary>The countdown starts when she steps out of the lamp.</summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            leaveTick = Find.TickManager.TicksGame + Props.freedomTicks;
        }

        public bool Spent => leaveTick >= 0 && Find.TickManager.TicksGame >= leaveTick;

        /// <summary>"(leaves in 3 h)" in the health tab — hidden once she belongs
        /// to the colony, where the countdown no longer applies.</summary>
        public override string CompLabelInBracketsExtra
        {
            get
            {
                Pawn pawn = parent?.pawn;
                if (pawn != null && pawn.Faction != null && pawn.Faction.IsPlayer)
                {
                    return null;
                }
                if (leaveTick < 0 || Spent)
                {
                    return null;
                }
                return "PMM_GenieFreedom_LeavesIn".Translate(
                    GenDate.ToStringTicksToPeriod(leaveTick - Find.TickManager.TicksGame, true, true).Named("TIME"));
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref leaveTick, "leaveTick", -1);
        }
    }

    /// <summary>
    /// Think-tree gate for the genie's exit branch (PMM_ElementalWildBehavior):
    /// satisfied only while she is still factionless AND her freedom timer has
    /// run out, letting the vanilla JobGiver_ExitMapBest take her off the map.
    /// </summary>
    public class ThinkNode_ConditionalGenieFreedomSpent : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.Faction != null && pawn.Faction.IsPlayer)
            {
                return false; // welcomed or tamed: she stays
            }
            return GenieSpawner.FreedomSpent(pawn);
        }
    }

    /// <summary>
    /// Let the player know when an unclaimed genie finally wanders off. Postfix
    /// on the pawn exit path (the end of JobGiver_ExitMapBest's job): only for
    /// still-factionless genies — a colonist leaving in a caravan stays quiet.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap), new[] { typeof(bool), typeof(Rot4) })]
    public static class Patch_GenieDepartureMessage
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.kindDef != ElementalDefOf.PMM_Elemental_Genie)
            {
                return;
            }
            if (__instance.Faction != null && __instance.Faction.IsPlayer)
            {
                return;
            }
            Messages.Message("PMM_GenieDeparted".Translate(__instance.Named("PAWN")),
                MessageTypeDefOf.NeutralEvent, false);
        }
    }

    // =====================================================================
    // 6. LAMP BOND (LEGACY) — superseded by the tsugai bond (user ruling
    //    2026-09-01): the hediff is no longer granted to anyone, and the wish
    //    reads the tsugai relation instead. The def and comp stay so saves from
    //    the first build still load; those stale copies are inert (a dead
    //    master reference at worst) — remove them with dev tools if they offend.
    //    (They are NOT auto-stripped: vanilla's HealthTickInterval walks the
    //    hediff list with a foreach enumerator, so a comp removing its own
    //    hediff mid-tick would throw collection-modified.)
    // =====================================================================

    public class HediffCompProperties_GenieBond : HediffCompProperties
    {
        public HediffCompProperties_GenieBond() { compClass = typeof(HediffComp_GenieBond); }
    }

    public class HediffComp_GenieBond : HediffComp
    {
        /// <summary>The lamp cleaner (legacy — no longer read by the wish).</summary>
        public Pawn master;

        /// <summary>Shows "(master: NAME)" in the health tab so the bond is discoverable.</summary>
        public override string CompLabelInBracketsExtra =>
            master == null ? null : "PMM_GenieBond_Master".Translate(master.Named("MASTER"));

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref master, "master");
        }
    }

    // =====================================================================
    // 7. THE WISH — gene-granted ability, one-year cooldown, 95% mana drained
    //    to zero. Grants in priority order: missing limb → scar → biggest
    //    temporary negative memory.
    // =====================================================================

    public class CompProperties_AbilityGenieWish : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityGenieWish() { compClass = typeof(CompAbilityEffect_GenieWish); }
    }

    public class CompAbilityEffect_GenieWish : CompAbilityEffect
    {
        /// <summary>The wish needs nearly all her mana — 95% of the mana need.</summary>
        private const float ManaRequired = 0.95f;

        public new CompProperties_AbilityGenieWish Props => (CompProperties_AbilityGenieWish)props;

        /// <summary>
        /// Grey out the gizmo (the HemogenCost pattern) when she has no living
        /// bonded colonist to serve, or her mana sits below the 95% the wish needs.
        /// </summary>
        public override bool GizmoDisabled(out string reason)
        {
            Pawn caster = parent.pawn;
            Pawn master = GenieSpawner.BondedMaster(caster);
            if (master == null || master.Dead || !master.IsColonist)
            {
                reason = "PMM_GenieWish_NoBond".Translate(caster.Named("PAWN"));
                return true;
            }
            Need mana = caster?.needs?.TryGetNeed(ElementalDefOf.ProjectMomo_Mana);
            if (mana == null || mana.CurLevel < ManaRequired)
            {
                reason = "PMM_GenieWish_NoMana".Translate(caster.Named("PAWN"));
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        /// <summary>Only her bonded master highlights while targeting.</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) &&
                   target.Pawn != null && target.Pawn == GenieSpawner.BondedMaster(parent.pawn) &&
                   HasBoon(target.Pawn);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn master = GenieSpawner.BondedMaster(parent.pawn);
            if (target.Pawn == null || target.Pawn != master)
            {
                if (throwMessages)
                {
                    Messages.Message("PMM_GenieWish_NotBondedTarget".Translate(),
                        MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            // Guard the wasted cast: with nothing to mend the wish would burn a
            // year of cooldown for nothing.
            if (!HasBoon(target.Pawn))
            {
                if (throwMessages)
                {
                    Messages.Message("PMM_GenieWish_NothingToGrant".Translate(target.Pawn.Named("PAWN")),
                        MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn master = GenieSpawner.BondedMaster(caster);
            if (master == null || target.Pawn != master)
            {
                return;
            }
            string granted = GrantWish(master);
            if (granted == null)
            {
                return;
            }
            // The wish costs everything she has: mana fully drained.
            Need mana = caster.needs?.TryGetNeed(ElementalDefOf.ProjectMomo_Mana);
            if (mana != null)
            {
                mana.CurLevel = 0f;
            }
            FleckMaker.ThrowSmoke(master.Position.ToVector3Shifted(), master.Map, 2f);
            Messages.Message(granted, master, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>True while the pawn has any boon a wish could grant.</summary>
        private static bool HasBoon(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }
            if (pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count > 0)
            {
                return true;
            }
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury && HediffUtility.IsPermanent(h))
                {
                    return true;
                }
            }
            if (pawn.needs?.mood != null)
            {
                foreach (Thought_Memory t in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (!t.permanent && t.MoodOffset() < 0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Grant the single highest-priority boon: the biggest missing limb
        /// regrown, else a permanent scar erased, else the biggest temporary
        /// negative memory lifted. Returns the message to show, or null if the
        /// master had nothing to wish away.
        /// </summary>
        private static string GrantWish(Pawn pawn)
        {
            // 1. A missing limb: the heaviest missing part (most coverage) regrows whole.
            List<Hediff_MissingPart> missing = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            if (missing.Count > 0)
            {
                Hediff_MissingPart worst = missing[0];
                for (int i = 1; i < missing.Count; i++)
                {
                    if (missing[i].Part.coverageAbsWithChildren > worst.Part.coverageAbsWithChildren)
                    {
                        worst = missing[i];
                    }
                }
                BodyPartRecord part = worst.Part;
                pawn.health.RestorePart(part, null, true);
                return "PMM_GenieWish_Limb".Translate(pawn.Named("PAWN"), part.Label.Named("PART"));
            }

            // 2. A scar: the first permanent injury simply ceases to be.
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury injury && HediffUtility.IsPermanent(injury))
                {
                    pawn.health.RemoveHediff(injury);
                    return "PMM_GenieWish_Scar".Translate(pawn.Named("PAWN"));
                }
            }

            // 3. The biggest temporary negative moodlet: the lowest mood offset
            //    among non-permanent memories is lifted from her master's heart.
            if (pawn.needs?.mood != null)
            {
                Thought_Memory worstThought = null;
                foreach (Thought_Memory t in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (!t.permanent && t.MoodOffset() < 0f &&
                        (worstThought == null || t.MoodOffset() < worstThought.MoodOffset()))
                    {
                        worstThought = t;
                    }
                }
                if (worstThought != null)
                {
                    string label = worstThought.LabelCap;
                    pawn.needs.mood.thoughts.memories.RemoveMemory(worstThought);
                    return "PMM_GenieWish_Mood".Translate(pawn.Named("PAWN"), label.Named("THOUGHT"));
                }
            }
            return null;
        }
    }
}
