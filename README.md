# Description
Does what some people think `ExtraSkillSlots` does: adds an additional row of skills for vanilla characters, so you can use both skill variants.

# Exceptions
* `Engineer` - not adding an extra slot for turrets.
* `MUL-T` - nothing is added.
* `Captain` - nothing is added.
* `Bandit` - not adding an extra slot for primary skill.

# Configs (and modded characters)
By default configs are configured to add skills only for vanilla characters (with exceptions listed above).
You can enable extra skills for modded characters in corresponding config sections.


If you want to support me, [you can do this here](https://www.buymeacoffee.com/KingEnderBrine)

# Changelog
**2.2.0**

* Fixes for `Survivor of the Void` update.

**2.1.1**

* Gone back to using display name instead of internals because that didn't work out for modded character. (Had to do hacky stuff to make it work, but who cares)

**2.1.0**

* Fixed for Anniversary Update.
* Now using internal character names for config sections (Not glad about that, but had to do).

**2.0.2**

* Fixed an issue when some symbols in character names causing errors.

**2.0.1**

* Readme update

**2.0.0**

* Removed `Unsafe` config option.
* Added config options for each character (including modded) that allow you to manually select which skills should be selected from the corresponding skill row. 

**1.1.0**

* Added `Unsafe` config option.

**1.0.0**

* Mod release.