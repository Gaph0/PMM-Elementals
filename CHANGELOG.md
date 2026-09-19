# Changelog

## Player-facing

- 2026-09-19: Added Big and Small - Framework as a required mod.
- 2026-09-19: Rebalanced all eight elementals to 0.8 body size. They carry less, butcher smaller and dodge better than a human.
- 2026-09-01: Removed the beardless gene from the genie.
- 2026-09-01: Fixed a freed genie walking straight off the map. She now waits four hours near her lamp, then leaves if nobody welcomes her.
- 2026-09-01: Changed the genie's bond. Cleaning her lamp no longer makes the cleaner her master. The wish unlocks when she and a man form a tsugai bond, and locks again if he dies.
- 2026-09-01: Added the genie, the lamp elemental. A colonist finds an old lamp and cleans it. The genie swirls out bound to her, with a join offer. Her wish regrows a lost limb, erases a scar, or lifts a bad memory.
- 2026-09-01: Added the dryad, the tree elemental. She spawns in forests when the temperature suits tree growth. She feeds herself in sunlight, works plants faster, and her harvests yield more.
- 2026-09-01: Rebalanced the dryad's genes. She loses fire resistance and strong immunity, takes four times fire damage, fears fire, and is poor at social.
- 2026-09-01: Changed elemental wander-in events to fire on camps and temporary maps as well as your home map.
- 2026-09-01: Added the dorome, the clay elemental. She spawns in caves or in wet swamps, sheds mud instead of dirt, and can merge into a stone tile to hide.
- 2026-08-30: Added the first four elementals: gnome, ignis, sylph and undine. Each has her own race, xenotype, wild pawn kind, backstory and wander-in event.
- 2026-08-30: Added blue skin for the undine and light green skin for the sylph, with a slightly different shade on each woman.
- 2026-08-30: Added dirt-brown skin for the gnome and flame-orange skin for the ignis, with a slightly different shade on each woman.
- 2026-08-30: Fixed wild elementals walking straight off the map.
- 2026-08-30: Fixed the gnome's cave gate opening on maps that have no caves.
- 2026-08-30: Added a full gene kit to the gnome: calm, cave-dweller, tough, and good with plants and building.
- 2026-08-30: Added a full gene kit to the ignis: fast, immune to fire, a great cook, and weak to cold.
- 2026-08-30: Added a full gene kit to the sylph: happy, kind, a light sleeper, delicate, attractive, a quick study, and artistic.
- 2026-08-30: Added a full gene kit to the undine: happy, kind, good with plants and medicine, a poor miner, short-sighted, weak to cold, and slow.
- 2026-08-30: Changed the ignis to shrug off heat while she suffers cold.
- 2026-08-30: Rebalanced the sylph: happy instead of very happy, and strong artistic instead of remarkable.
- 2026-08-30: Added the ignis flame aura. Nearby humans take extra tease damage; momos, animals, insects and mechs catch fire.
- 2026-08-30: Added gnome living-stone healing. She recovers while she stands on soil or bare rock, or under a mountain.
- 2026-08-30: Added the sylph caprice. Her moods swing between giddy and foul.
- 2026-08-30: Added the undine water affinity. She moves faster in wet weather and cleans filth quickly.
- 2026-08-30: Changed gnome living-stone healing to work on bare mountain rock as well as soil.
- 2026-08-30: Rebalanced gnome living-stone healing to 1.0 per hour, up from 0.4.
- 2026-08-30: Fixed the ignis flame aura not working for a recruited ignis.

## Internal

- 2026-09-20: Changed the README to match the code.
- 2026-09-19: Changed the build to use MSBuild.
- 2026-09-19: Changed the genie's lamp to use its own jar art instead of the vanilla golden cube.
- 2026-09-02: Removed the bundled MGE wiki copies and the elementals plan doc.
- 2026-09-01: Fixed a dryad gene that pointed at a thought instead of a gene.
- 2026-08-30: Added the About metadata, build and release scripts, and the README.
- 2026-08-30: Added gated wander-in workers for the four elementals, wild-man patches and per-element backstories.
- 2026-08-30: Added sync.sh to copy the mod into RimWorld/Mods.
- 2026-08-30: Fixed a malformed gene-kit block that the loader dropped.
- 2026-08-30: Changed the elemental powers to run from hediff comps and patches, because race comps never run.
