using EntityStates;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedLoadout
{
    public static class LoaderHooks
    {
        public static void ProjectileGrappleControllerDeductOwnerStockHook(On.RoR2.Projectile.ProjectileGrappleController.FlyState.orig_DeductOwnerStock orig, BaseState self)
        {
            if (!(self is ProjectileGrappleController.FlyState state))
            {
                return;
            }

            if (!state.ownerValid || !state.owner.hasEffectiveAuthority)
            {
                return;
            }

            if (!(state.owner.stateMachine.state is BaseSkillState baseState))
            {
                return;
            }

            baseState.activatorSkillSlot.DeductStock(1);
        }
    }
}
