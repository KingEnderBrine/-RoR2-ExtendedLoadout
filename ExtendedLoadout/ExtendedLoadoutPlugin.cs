using BepInEx;
using BepInEx.Logging;
using ExtraSkillSlots;
using RoR2;
using RoR2.ContentManagement;
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
    [BepInDependency("com.KingEnderBrine.ExtraSkillSlots", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(GUID, Name, Version)]
    public class ExtendedLoadoutPlugin : BaseUnityPlugin, IContentPackProvider
    {
        public const string GUID = "com.KingEnderBrine.ExtendedLoadout";
        public const string Name = "Extended Loadout";
        public const string Version = "2.1.0";

        private static ExtendedLoadoutPlugin Instance { get; set; }
        private static ManualLogSource InstanceLogger => Instance?.Logger;

        public static SkillDef DisabledSkill { get; private set; }

        public string identifier => "ExtendedLoadout";
        private ContentPack contentPack;
        private readonly Dictionary<SurvivorDef, SkillFamily[]> cachedFamilies = new Dictionary<SurvivorDef, SkillFamily[]>();


        private void Awake()
        {
            Instance = this;

            ContentManager.collectContentPackProviders += CollectContentPackProviders;

            On.RoR2.Language.LoadStrings += LanguageConsts.OnLoadStrings;
            On.EntityStates.Treebot.Weapon.AimMortar2.KeyIsDown += TreebotHooks.AimMortar2KeyIsDown;
            On.RoR2.Projectile.ProjectileGrappleController.FlyState.DeductOwnerStock += LoaderHooks.ProjectileGrappleControllerDeductOwnerStockHook;

            NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append($"{GUID};{Version}");
        }

        private void CollectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        private static SkillFamily[] ExtendSurvivor(SurvivorDef survivorDef)
        {
            var survivorName = ((ScriptableObject)survivorDef).name;
            try
            {
                var bodyPrefab = survivorDef.bodyPrefab;

                if (bodyPrefab.GetComponent<ExtraSkillLocator>())
                {
                    return Array.Empty<SkillFamily>();
                }

                var skillMap = new SkillMapConfigSection(Instance.Config, survivorName);

                var skillLocator = bodyPrefab.GetComponent<SkillLocator>();
                var extraSkillLocator = bodyPrefab.AddComponent<ExtraSkillLocator>();

                extraSkillLocator.extraFirst = CopySkill(bodyPrefab, survivorName, "First", skillLocator.primary, skillMap.FirstRowSkills.Value);
                extraSkillLocator.extraSecond = CopySkill(bodyPrefab, survivorName, "Second", skillLocator.secondary, skillMap.SecondRowSkills.Value);
                extraSkillLocator.extraThird = CopySkill(bodyPrefab, survivorName, "Third", skillLocator.utility, skillMap.ThirdRowSkills.Value);
                extraSkillLocator.extraFourth = CopySkill(bodyPrefab, survivorName, "Fourth", skillLocator.special, skillMap.FourthRowSkills.Value);

                var families = new List<SkillFamily>();

                if (extraSkillLocator.extraFirst) families.Add(extraSkillLocator.extraFirst.skillFamily);
                if (extraSkillLocator.extraSecond) families.Add(extraSkillLocator.extraSecond.skillFamily);
                if (extraSkillLocator.extraThird) families.Add(extraSkillLocator.extraThird.skillFamily);
                if (extraSkillLocator.extraFourth) families.Add(extraSkillLocator.extraFourth.skillFamily);

                return families.ToArray();
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed adding extra skill slots for \"{survivorName}\"");
                InstanceLogger.LogError(e);
            }

            return Array.Empty<SkillFamily>();
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
            var variants = new List<SkillFamily.Variant>() { new SkillFamily.Variant { skillDef = DisabledSkill } };
            variants.AddRange(filteredVariants);
            extraSkillFamily.variants = variants.ToArray();

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

        System.Collections.IEnumerator IContentPackProvider.LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            contentPack = new ContentPack();

            DisabledSkill = ScriptableObject.CreateInstance<SkillDef>();
            DisabledSkill.skillName = "Disabled";
            ((ScriptableObject)DisabledSkill).name = DisabledSkill.skillName;
            DisabledSkill.skillNameToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_NAME;
            DisabledSkill.skillDescriptionToken = LanguageConsts.EXTENDED_LOADOUT_SKILL_DISABLED_DESCRIPTION;
            DisabledSkill.icon = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), new Vector2(0, 0));// LoadoutAPI.CreateSkinIcon(Color.black, Color.black, Color.black, Color.black, Color.red);
            DisabledSkill.activationStateMachineName = "Weapon";
            DisabledSkill.activationState = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Idle));
            DisabledSkill.interruptPriority = EntityStates.InterruptPriority.Any;
            DisabledSkill.baseRechargeInterval = 0;
            DisabledSkill.baseMaxStock = 1;
            DisabledSkill.rechargeStock = 0;
            DisabledSkill.beginSkillCooldownOnSkillEnd = false;
            DisabledSkill.rechargeStock = 1;
            DisabledSkill.stockToConsume = 2;
            DisabledSkill.isCombatSkill = false;

            contentPack.skillDefs.Add(new[] { DisabledSkill });

            args.ReportProgress(0.99F);
            yield break;
        }

        System.Collections.IEnumerator IContentPackProvider.GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);

            foreach (var loadInfo in args.peerLoadInfos)
            {
                foreach (var survivorDef in loadInfo.previousContentPack.survivorDefs)
                {
                    if (!cachedFamilies.TryGetValue(survivorDef, out var families))
                    {
                        cachedFamilies[survivorDef] = families = ExtendSurvivor(survivorDef);
                    }

                    if (families.Length == 0)
                    {
                        continue;
                    }

                    args.output.skillFamilies.Add(families);
                }
            }

            args.ReportProgress(1);
            yield break;
        }

        System.Collections.IEnumerator IContentPackProvider.FinalizeAsync(FinalizeAsyncArgs args)
        {
            cachedFamilies.Clear();
            args.ReportProgress(1);
            yield break;
        }
    }
}