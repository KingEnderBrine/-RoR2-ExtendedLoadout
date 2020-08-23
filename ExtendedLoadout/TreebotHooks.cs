using EntityStates.Treebot.Weapon;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedLoadout
{
    public static class TreebotHooks
    {
        public static bool AimMortar2KeyIsDown(On.EntityStates.Treebot.Weapon.AimMortar2.orig_KeyIsDown orig, AimMortar2 self)
        {
            return self.IsKeyDownAuthority();
        }
    }
}
