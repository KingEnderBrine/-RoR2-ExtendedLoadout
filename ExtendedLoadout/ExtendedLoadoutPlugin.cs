using BepInEx;
using BepInEx.Logging;
using ExtraSkillSlots;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion(ExtendedLoadout.ExtendedLoadoutPlugin.Version)]
namespace ExtendedLoadout
{
    [BepInDependency("com.KingEnderBrine.ExtraSkillSlots", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(GUID, Name, Version)]
    public class ExtendedLoadoutPlugin : BaseUnityPlugin, IContentPackProvider
    {
        public const string GUID = "com.KingEnderBrine.ExtendedLoadout";
        public const string Name = "Extended Loadout";
        public const string Version = "2.2.0";

        private static ExtendedLoadoutPlugin Instance { get; set; }
        private static ManualLogSource InstanceLogger => Instance?.Logger;

        public static SkillDef DisabledSkill { get; private set; }

        public string identifier => "ExtendedLoadout";
        private ContentPack contentPack;
        private readonly Dictionary<SurvivorDef, SkillFamily[]> cachedFamilies = new Dictionary<SurvivorDef, SkillFamily[]>();
        private Language english;

        private void Start()
        {
            Instance = this;

            NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append($"{GUID};{Version}");
            ContentManager.collectContentPackProviders += CollectContentPackProviders;

            On.EntityStates.Treebot.Weapon.AimMortar2.KeyIsDown += TreebotHooks.AimMortar2KeyIsDown;
            On.RoR2.Projectile.ProjectileGrappleController.FlyState.DeductOwnerStock += LoaderHooks.ProjectileGrappleControllerDeductOwnerStockHook;

#warning Fix for language, remove when next update is out
            if (RoR2Application.GetBuildId() == "1.2.2.0")
            {
                On.RoR2.Language.SetFolders += LanguageSetFolders;
            }
            else
            {
                Language.collectLanguageRootFolders += CollectLanguageRootFolders;
            }
        }

        private void CollectLanguageRootFolders(List<string> folders)
        {
            folders.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "Language"));
        }

        private void CollectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        private SkillFamily[] ExtendSurvivor(SurvivorDef survivorDef)
        {
            var survivorName = survivorDef.displayNameToken;
            try
            {
                var bodyPrefab = survivorDef.bodyPrefab;

                if (bodyPrefab.GetComponent<ExtraSkillLocator>())
                {
                    return Array.Empty<SkillFamily>();
                }

                var skillMap = new SkillMapConfigSection(Instance.Config, english.GetLocalizedStringByToken(survivorDef.displayNameToken));

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

            var originalSkillFamily = original.skillFamily;
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
            extraSkill.skillName = $"Extra{familySuffix}";
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
            DisabledSkill.icon = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), new Vector2(0, 0));
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

            args.ReportProgress(0.90F);

            //Early load of language to get display names
            Language.collectLanguageRootFolders += CollectRoRLanguageFolder;
            english = new Language("en");
            english.SetFolders(Language.GetLanguageRootFolders().SelectMany(el => Directory.EnumerateDirectories(el, "en")));
            english.LoadStrings();
            Language.collectLanguageRootFolders -= CollectRoRLanguageFolder;

            args.ReportProgress(1F);
            yield break;
        }

        private static void CollectRoRLanguageFolder(List<string> list)
        {
            list.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.streamingAssetsPath, "Language")));
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
            english.UnloadStrings();
            english = null;

            cachedFamilies.Clear();

            args.ReportProgress(1);
            yield break;
        }

#warning Fix for language, remove when next update is out
        private void LanguageSetFolders(On.RoR2.Language.orig_SetFolders orig, Language self, IEnumerable<string> newFolders)
        {
            var dirs = Directory.EnumerateDirectories(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "Language"), self.name);
            orig(self, newFolders.Union(dirs));
        }
    }
}