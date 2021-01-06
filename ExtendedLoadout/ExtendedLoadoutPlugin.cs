using BepInEx;
using BepInEx.Logging;
using ExtraSkillSlots;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ExtendedLoadout
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(CommandHelper), nameof(LoadoutAPI))]
    [BepInDependency("com.KingEnderBrine.ExtraSkillSlots", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.KingEnderBrine.ExtendedLoadout", "Extended Loadout", "2.0.2")]
    public class ExtendedLoadoutPlugin : BaseUnityPlugin
    {
        private static ExtendedLoadoutPlugin Instance { get; set; }
        private static ManualLogSource InstanceLogger => Instance?.Logger;

        public static SkillDef DisabledSkill { get; private set; }

        public void Awake()
        {
            Instance = this;

            DisabledSkill = ScriptableObject.CreateInstance<SkillDef>();
            DisabledSkill.skillName = "Disabled";
            DisabledSkill.skillNameToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_NAME;
            DisabledSkill.skillDescriptionToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION;
            DisabledSkill.icon = LoadoutAPI.CreateSkinIcon(Color.black, Color.black, Color.black, Color.black, Color.red);
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


            foreach (var survivor in SurvivorCatalog.allSurvivorDefs)
            {
                try
                {
                    ExtendSurvivor(survivor.survivorIndex);
                }
                catch (Exception e)
                {
                    InstanceLogger.LogWarning($"Failed adding extra skill slots for \"{Language.english.GetLocalizedStringByToken(survivor.displayNameToken)}\"");
                    InstanceLogger.LogError(e);
                }
            }
        }

        private static void ExtendSurvivor(SurvivorIndex survivorIndex)
        {
            var survivor = SurvivorCatalog.GetSurvivorDef(survivorIndex);
            var bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorIndex);
            var bodyPrefab = survivor.bodyPrefab;

            var skillLocator = bodyPrefab.GetComponent<SkillLocator>();

            if (bodyPrefab.GetComponent<ExtraSkillLocator>())
            {
                return;
            }
            var skillMap = new SkillMapConfigSection(Instance.Config, Language.english.GetLocalizedStringByToken(survivor.displayNameToken));

            var extraSkillLocator = bodyPrefab.AddComponent<ExtraSkillLocator>();

            
            extraSkillLocator.extraFirst = CopySkill(bodyPrefab, survivor.name, "First", skillLocator.primary, skillMap.FirstRowSkills.Value);
            extraSkillLocator.extraSecond = CopySkill(bodyPrefab, survivor.name, "Second", skillLocator.secondary, skillMap.SecondRowSkills.Value);
            extraSkillLocator.extraThird = CopySkill(bodyPrefab, survivor.name, "Third", skillLocator.utility, skillMap.ThirdRowSkills.Value);
            extraSkillLocator.extraFourth = CopySkill(bodyPrefab, survivor.name, "Fourth", skillLocator.special, skillMap.FourthRowSkills.Value);

            var additionalLength = 0;
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
                skillSlots[originalLength + index++] = extraSkillLocator.extraFirst;
            }
            if (extraSkillLocator.extraSecond)
            {
                skillSlots[originalLength + index++] = extraSkillLocator.extraSecond;
            }
            if (extraSkillLocator.extraThird)
            {
                skillSlots[originalLength + index++] = extraSkillLocator.extraThird;
            }
            if (extraSkillLocator.extraFourth)
            {
                skillSlots[originalLength + index++] = extraSkillLocator.extraFourth;
            }

            var skillSlotsField = BodyCatalog.skillSlots;
            skillSlotsField[bodyIndex] = skillSlots;
        }

        private static GenericSkill CopySkill(GameObject bodyPrefab, string familyPrefix, string familySuffix, GenericSkill original, string skillMap)
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

            var filteredVariants = FilterVariants(originalSkillFamily.variants, skillMap);
            if (filteredVariants.Count() == 0)
            {
                return null;
            }

            var extraSkill = bodyPrefab.AddComponent<GenericSkill>();
            var extraSkillFamily = ScriptableObject.CreateInstance<SkillFamily>();

            (extraSkillFamily as ScriptableObject).name = $"{familyPrefix}Extra{familySuffix}Family";
            var variants = new List<SkillFamily.Variant>() { new SkillFamily.Variant { skillDef = DisabledSkill, unlockableName = "" } };
            variants.AddRange(filteredVariants);
            extraSkillFamily.variants = variants.ToArray();

            LoadoutAPI.AddSkillFamily(extraSkillFamily);
            extraSkill._skillFamily = extraSkillFamily;

            return extraSkill;
        }

        private static IEnumerable<SkillFamily.Variant> FilterVariants(SkillFamily.Variant[] variants, string skillMap)
        {
            var trimmedMap = skillMap.Trim();
            var isBlacklist = trimmedMap.StartsWith("^");

            var indices = (isBlacklist ? trimmedMap.Substring(1) : trimmedMap).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(el => int.Parse(el.Trim()));

            return variants.Where((el, index) => isBlacklist ^ indices.Contains(index));
        }
    }
}