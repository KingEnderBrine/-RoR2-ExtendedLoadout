using EntityStates;
using EntityStates.Engi.EngiWeapon;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtendedLoadout
{
    public static class EngiHooks
    {
        public static void PlaceTurretFixedUpdateILHook(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchCall(out var _),
                x => x.MatchCall(out var _),
                x => x.MatchBrfalse(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(out _),
                x => x.MatchLdcI4(3),
                x => x.MatchCall(out _),
                x => x.MatchStloc(0));
        }

        public static void PlaceTurretFixedUpdateHook(On.EntityStates.Engi.EngiWeapon.PlaceTurret.orig_FixedUpdate orig, PlaceTurret self)
        {
            (self as BaseState).FixedUpdate();
            if (!self.isAuthority)
                return;
            self.entryCountdown -= Time.fixedDeltaTime;
            if (self.exitPending)
            {
                self.exitCountdown -= Time.fixedDeltaTime;
                if ((double)self.exitCountdown > 0.0)
                    return;
                self.outer.SetNextStateToMain();
            }
            else
            {
                if (!self.inputBank || (double)self.entryCountdown > 0.0)
                    return;
                if ((self.inputBank.skill1.down || self.inputBank.skill4.justPressed) && self.currentPlacementInfo.ok)
                {
                    if (self.characterBody)
                    {
                        self.characterBody.SendConstructTurret(self.characterBody, self.currentPlacementInfo.position, self.currentPlacementInfo.rotation, MasterCatalog.FindMasterIndex(self.turretMasterPrefab));
                        //self.
                        if (self.skillLocator)
                        {
                            GenericSkill skill = self.skillLocator.GetSkill(SkillSlot.Special);
                            if (skill)
                                skill.DeductStock(1);
                        }
                    }
                    int num = (int)Util.PlaySound(self.placeSoundString, self.gameObject);
                    self.DestroyBlueprints();
                    self.exitPending = true;
                }
                if (!self.inputBank.skill2.justPressed)
                    return;
                self.DestroyBlueprints();
                self.exitPending = true;
            }
        }
    }
}
