using RoR2;

namespace ExtendedLoadout
{
    public static class LanguageConsts
    {
        public static readonly string EXTENDED_LOADOUT_SKILL_DISABLED_NAME = nameof(EXTENDED_LOADOUT_SKILL_DISABLED_NAME);
        public static readonly string EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION = nameof(EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION);

        public static void OnLoadStrings(On.RoR2.Language.orig_LoadStrings orig, Language self)
        {
            orig(self);

            switch (self.name.ToLower())
            {
                case "ru":
                    self.SetStringByToken(EXTENDED_LOADOUT_SKILL_DISABLED_NAME, "Выключен");
                    self.SetStringByToken(EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION, "Доп. навык не будет показан в течение забега");
                    break;
                default:
                    self.SetStringByToken(EXTENDED_LOADOUT_SKILL_DISABLED_NAME, "Disabled");
                    self.SetStringByToken(EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION, "Extra skill will not be shown during a run");
                    break;
            }
        }
    }
}
