using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtraSkillSlots;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ExtendedLoadout
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(CommandHelper), nameof(LoadoutAPI))]
    [BepInDependency("com.KingEnderBrine.ExtraSkillSlots", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.KingEnderBrine.ExtendedLoadout", "Extended Loadout", "1.1.0")]
    public class ExtendedLoadoutPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<bool> Unsafe { get; set; }
        private static ExtendedLoadoutPlugin Instance { get; set; }
        private static ManualLogSource InstanceLogger => Instance?.Logger;

        public static SkillDef DisabledSkill { get; private set; }

        public void Awake()
        {
            Instance = this;

            Unsafe = Config.Bind("Main", "Unsafe", false, "Unsafe mode will add extra skill slots for every character (including modded), for every skill which has at least 2 variants. Which may or may not be broken");

            DisabledSkill = ScriptableObject.CreateInstance<SkillDef>();
            DisabledSkill.skillName = "Disabled";
            DisabledSkill.skillNameToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_NAME;
            DisabledSkill.skillDescriptionToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION;
            DisabledSkill.icon = LoadoutAPI.CreateSkinIcon(Color.black, Color.black, Color.black,Color.black, Color.red);
            DisabledSkill.activationStateMachineName = "Weapon";
            DisabledSkill.activationState = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Idle));
            DisabledSkill.interruptPriority = EntityStates.InterruptPriority.Any;
            DisabledSkill.baseRechargeInterval = 0;
            DisabledSkill.baseMaxStock = 1;
            DisabledSkill.rechargeStock = 0;
            DisabledSkill.isBullets = false;
            DisabledSkill.shootDelay = 0;
            DisabledSkill.beginSkillCooldownOnSkillEnd = false;
            DisabledSkill.rechargeStock = 2;
            DisabledSkill.stockToConsume = 0;
            DisabledSkill.isCombatSkill = false;

            LoadoutAPI.AddSkillDef(DisabledSkill);

            On.RoR2.SurvivorCatalog.Init += SurvivorCatalog_Init;

            On.EntityStates.Treebot.Weapon.AimMortar2.KeyIsDown += TreebotHooks.AimMortar2KeyIsDown;

            On.RoR2.Projectile.ProjectileGrappleController.FlyState.DeductOwnerStock += LoaderHooks.ProjectileGrappleControllerDeductOwnerStockHook;
        }

        private static void SurvivorCatalog_Init(On.RoR2.SurvivorCatalog.orig_Init orig)
        {
            orig();

            try
            {
                if (Unsafe.Value)
                {
                    foreach (var survivor in SurvivorCatalog.allSurvivorDefs)
                    {
                        ExtendSurvivor(survivor.survivorIndex, survivor.name);
                    }
                }
                else
                {
                    ExtendCommando();
                    ExtendHuntress();
                    ExtendEngi();
                    ExtendMage();
                    ExtendMerc();
                    ExtendTreebot();
                    ExtendLoader();
                    ExtendCroco();
                }
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning("Failed adding extra skill slots to survivors");
                InstanceLogger.LogError(e);
            }
        }

        private static void ExtendCommando() => ExtendSurvivor(SurvivorIndex.Commando, "Commando", addFisrt: false);    
        private static void ExtendHuntress() => ExtendSurvivor(SurvivorIndex.Huntress, "Huntress", addSecond: false);    
        private static void ExtendEngi() => ExtendSurvivor(SurvivorIndex.Engi, "Engi", addFisrt: false, addFourth: false);    
        private static void ExtendMage() => ExtendSurvivor(SurvivorIndex.Mage, "Mage", addThird: false);    
        private static void ExtendMerc() => ExtendSurvivor(SurvivorIndex.Merc, "Merc", addFisrt: false, addThird: false);    
        private static void ExtendTreebot() => ExtendSurvivor(SurvivorIndex.Treebot, "Treebot", addFisrt: false, addFourth: false);
        private static void ExtendLoader() => ExtendSurvivor(SurvivorIndex.Loader, "Loader", addFisrt: false, addFourth: false);
        private static void ExtendCroco() => ExtendSurvivor(SurvivorIndex.Croco, "Croco", addFisrt: false, addFourth: false);


        private static void ExtendSurvivor(
            SurvivorIndex survivorIndex,
            string familyPrefix,
            bool addFisrt = true,
            bool addSecond = true,
            bool addThird = true,
            bool addFourth = true)
        {
            var survivor = SurvivorCatalog.GetSurvivorDef(survivorIndex);
            var bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorIndex);
            var bodyPrefab = survivor.bodyPrefab;

            var skillLocator = bodyPrefab.GetComponent<SkillLocator>();

            if (bodyPrefab.GetComponent<ExtraSkillLocator>())
            {
                return;
            }

            var extraSkillLocator = bodyPrefab.AddComponent<ExtraSkillLocator>();

            var additionalLength = 0;
            if (addFisrt)
            {
                var firstExtraSkill = CopySkill(bodyPrefab, familyPrefix, "First", skillLocator.primary);
                extraSkillLocator.extraFirst = firstExtraSkill;
            }

            if (addSecond)
            {
                var secondExtraSkill = CopySkill(bodyPrefab, familyPrefix, "Second", skillLocator.secondary);
                extraSkillLocator.extraSecond = secondExtraSkill;
            }

            if (addThird)
            {
                var thirdExtraSkill = CopySkill(bodyPrefab, familyPrefix, "Third", skillLocator.utility);
                extraSkillLocator.extraThird = thirdExtraSkill;
            }

            if (addFourth)
            {
                var fourthExtraSkill = CopySkill(bodyPrefab, familyPrefix, "Fourth", skillLocator.special);
                extraSkillLocator.extraFourth = fourthExtraSkill;
            }

            additionalLength += extraSkillLocator.extraFirst ? 1 : 0;
            additionalLength += extraSkillLocator.extraSecond ? 1 : 0;
            additionalLength += extraSkillLocator.extraThird ? 1 : 0;
            additionalLength += extraSkillLocator.extraFourth ? 1 : 0;

            if (additionalLength == 0)
            {
                return;
            }

            var skillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
            var originalLength = skillSlots.Length;
            Array.Resize(ref skillSlots, originalLength + additionalLength);

            var index = 0;
            if (extraSkillLocator.extraFirst)
            {
                skillSlots[originalLength + index] = extraSkillLocator.extraFirst;
                index++;
            }
            if (extraSkillLocator.extraSecond)
            {
                skillSlots[originalLength + index] = extraSkillLocator.extraSecond;
                index++;
            }
            if (extraSkillLocator.extraThird)
            {
                skillSlots[originalLength + index] = extraSkillLocator.extraThird;
                index++;
            }
            if (extraSkillLocator.extraFourth)
            {
                skillSlots[originalLength + index] = extraSkillLocator.extraFourth;
            }

            var skillSlotsField = BodyCatalog.skillSlots;
            skillSlotsField[bodyIndex] = skillSlots;
        }

        private static GenericSkill CopySkill(GameObject bodyPrefab, string familyPrefix, string familySuffix, GenericSkill original)
        {
            if (!original)
            {
                return null;
            }
            var originalSkillFamily = original._skillFamily;
            if (!originalSkillFamily || originalSkillFamily.variants == null || originalSkillFamily.variants.Length < 2)
            {
                return null;
            }
            var extraSkill = bodyPrefab.AddComponent<GenericSkill>();
            var extraSkillFamily = ScriptableObject.CreateInstance<SkillFamily>();

            (extraSkillFamily as ScriptableObject).name = $"{familyPrefix}Extra{familySuffix}Family";
            var variants = new List<SkillFamily.Variant>() { new SkillFamily.Variant { skillDef = DisabledSkill, unlockableName = "" } };
            variants.AddRange(originalSkillFamily.variants);
            extraSkillFamily.variants = variants.ToArray();

            LoadoutAPI.AddSkillFamily(extraSkillFamily);
            extraSkill._skillFamily = extraSkillFamily;

            return extraSkill;
        }
    }
}