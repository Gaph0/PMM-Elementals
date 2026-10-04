# Project Mamono Elementals

Wild elementals for Project Mamono, for RimWorld 1.6. It adds eight spirit-folk
of earth, fire, wind and water who live in the wild places of the rim.

- **Eight elemental species.** Gnome, ignis, sylph, undine, dorome, dryad, apsara
  and genie, of earth, fire, wind, water, clay, wood and joy.
- **They arrive wild.** Seven walk onto your map through their own event and are
  tamed or captured like wild men. The genie is found in a lamp instead.
- **Each in her own land.** Caves, deserts and lava fields, high mountains,
  rivers and coasts, wetlands, or forests.
- **A power each.** Healing on bare ground, a mood that turns with the wind,
  speed in the rain, swimming through the earth, food from sunlight, a dance that
  charms, blows that set desire alight, and one wish a year.

**Requires:** Harmony, Biotech, Odyssey, Project Mamono, Big and Small - Framework

**Optional:** nothing beyond the mods listed above.

## Content

Eight elementals live in the wild places of the rim. All of them are women, all
of them are factionless, and none of them ever joins a faction's caravan or raid:
the only way in is her own event. Tame one, or capture her and convince her to
join, and she works, fights and bonds like any other mamono.

### Xenotypes

| Mamono | Element | Lore | Where she appears | In plain words |
|---|---|---|---|---|
| **Gnome** | Earth | A quiet earth elemental of caves and mines. Her worst wound closes while she stands on bare soil or gravel. | Caves and mines. | Tougher than a human and slower, very sleepy, and short-sighted. |
| **Ignis** | Fire | A fire elemental of the hot places. Fire, heat and lava cannot hurt her, and her blows set desire alight. | Deserts, lava fields, and any land averaging over 30 °C. | Quicker than a human, and a pessimist. |
| **Sylph** | Wind | A wind elemental who rides the high air looking for men. Her mood turns about once a day - foul or giddy. | Land over 1000 m high. | Frail and pretty, quick to learn, and she needs little sleep. |
| **Undine** | Water | A gentle water elemental of lakes and springs. She is quick while it rains, and she leaves less filth behind her. | Coasts, river tiles, and the Lake biome. | Slow and short-sighted, and even-tempered. |
| **Dorome** | Clay | An earth elemental melted into living mud. She can merge with the ground and travel under it. | Caves, and swamps with 1000 mm of rain or more. | Tough for a mamono and slow, and slow to learn. |
| **Dryad** | Wood | A tree elemental of living wood who feeds on sunlight. Roofed, or in the dark, she must eat and sleep like anyone else. | Forests, while the outdoor temperature is in the tree growth range. | Tough, and terrified of fire. |
| **Apsara** | Water | A water elemental of joy. Her dance charms everyone near her, and the charmed feel lighter. | Coasts, river tiles, and the Lake biome. | Slow on her feet, well liked, and no hand at fighting. |
| **Genie** | - | An elemental bound in a lamp, freed by the colonist who cleans it. She grants that colonist one wish a year. | Found in the desert, as an old dusty lamp. | Frail, easily hurt, and hard to anger. |

### Genes

- `gnome skin`, `ignis skin`, `sylph skin`, `undine skin`, `dorome skin`,
  `dryad skin`, `apsara skin` and `genie skin` - the colour of her body, in
  several shades each. No stat change.
- `mud merge` - lets the dorome swim through the earth. Grants her merge ability.
  No stat change.
- `wish-granting` - the genie's one wish a year. Grants her wish ability. No
  stat change.

Every elemental also carries the core Mamono gene and a set of vanilla aptitude
and mood genes. The ignis carries the core fiery gene, which is what makes fire,
heat and lava harmless to her.

### Events

- **A wild elemental wanders in** - one event per element, seven in all. Each
  fires only on land that suits her: a gnome on caves and mines, an ignis on
  deserts, lava fields or land averaging over 30 °C, a sylph over 1000 m up, an
  undine and an apsara by coasts, rivers and lakes, a dorome on caves or wet
  swamps, and a dryad in forest while the trees are growing. The elemental
  arrives factionless and alone, and is tamed through the vanilla wild-man flow
  (Animals, then Tame).
- **The strange lamp in the sand** - an event drops an old dusty lamp on a desert
  map. Have a colonist clean it, and the genie is freed, bonds to that colonist,
  and can grant her one wish a year.

### Abilities

- `merge with the earth` (dorome) - "Melt into the ground and swim through the
  earth, emerging at any natural spot on the map. Works only on natural ground -
  no floors, no deep water - and the strain keeps her from merging more than once
  every few hours." Cooldown 6 hours, the whole map, no cost.
- `grant wish` (genie) - "Grant one wish to the colonist she is bound to: regrow
  a missing limb, erase an old scar, or lift a lasting sorrow - in that order.
  The wish takes nearly all her mana and cannot be granted more than once a
  year." Cooldown 1 year, warmup 2 seconds, the whole map, and it takes nearly
  all of her mana. Only the colonist who freed her can be wished for.

## Found a bug?

Report it on the issue tracker: https://github.com/Gaph0/PMM-Elementals/issues

Please do not leave bug reports in the comments section. Post them on the
tracker so they can be tracked and fixed.

## License and attributions

Licensed under the Unlicense. See the
[licence](https://github.com/Gaph0/PMM-Elementals/blob/main/LICENSE.txt).

Thanks to:

- Tynan Sylvester and the Ludeon Studios team - RimWorld, and the Biotech and Odyssey expansions.
- Big and Small - Framework - the elemental races and the race pattern.
- Harmony - the patches under everything.
- Kenkou Cross - the Monster Girl Encyclopedia, where these creatures come from.
