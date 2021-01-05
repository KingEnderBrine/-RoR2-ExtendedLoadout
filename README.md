# Description
Does what some people think `ExtraSkillSlots` does: adds an additional row of skills for vanilla characters, so you can use both skill variants.

# Exceptions

* `Engineer` - not adding an extra slot for turrets.

* `MUL-T` - nothing is added.

* `Captain` - nothing is added.

# Unsafe mode
Currently, all additional skill slots are added manually only for vanilla characters to ensure that everything works.
`Unsafe` mode will add additional skill slots for every character (including modded) for every skill that has at least 2 variants.
Some skills may and will not work correctly (e.g. Engineer's turrets), and I can't add support for everything, that is why this mode is called `Unsafe`.

# Changelog
**1.1.0**

* Added `Unsafe` config option

**1.0.0**

* Mod release.