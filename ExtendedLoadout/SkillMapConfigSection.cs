using BepInEx.Configuration;
using System.Text.RegularExpressions;

namespace ExtendedLoadout
{
    public class SkillMapConfigSection
    {
        private const string skillsDescription =
@"List of indices (comma-separated, starting from 0) of skills which should be enabled.
If ""^"" added at the start then all the skill will be enabled except of specified indices.
For example:
""0, 3, 4"" - will enable only skills at these indices.
""^0, 3, 4"" - will enable skills at indices 1, 2, 5 and so on if there are any.
Leaving option empty will disable extra skill row.
Leaving option with only ""^"" will enable all skills for this row.
";

        public string SectionName { get; private set; }
        public ConfigEntry<string> FirstRowSkills { get; private set; }
        public ConfigEntry<string> SecondRowSkills { get; private set; }
        public ConfigEntry<string> ThirdRowSkills { get; private set; }
        public ConfigEntry<string> FourthRowSkills { get; private set; }

        public SkillMapConfigSection(ConfigFile file, string sectionName)
        {
            SectionName = RemoveInvalidCharacters(sectionName);
            
            switch (SectionName)
            {
                case "Acrid": SetupRowSkills(); break;
                case "Artificer": SetupRowSkills(); break;
                case "Captain": SetupRowSkills(); break;
                case "Commando": SetupRowSkills(); break;
                case "Engineer": SetupRowSkills(fourth: ""); break;
                case "Huntress": SetupRowSkills(); break;
                case "Loader": SetupRowSkills(); break;
                case "Mercenary": SetupRowSkills(); break;
                case "MUL-T": SetupRowSkills(first: ""); break;
                case "Rex": SetupRowSkills(); break;
                case "Bandit": SetupRowSkills(first: ""); break;
                case "Heretic": SetupRowSkills(); break;
                default: SetupRowSkills("", "", "", ""); break;
            }

            void SetupRowSkills(string first = "^", string second = "^", string third = "^", string fourth = "^")
            {
                FirstRowSkills = file.Bind(SectionName, nameof(FirstRowSkills), first, skillsDescription);
                SecondRowSkills = file.Bind(SectionName, nameof(SecondRowSkills), second);
                ThirdRowSkills = file.Bind(SectionName, nameof(ThirdRowSkills), third);
                FourthRowSkills = file.Bind(SectionName, nameof(FourthRowSkills), fourth);
            }
        }

        private string RemoveInvalidCharacters(string sectionName)
        {
            return Regex.Replace(sectionName, @"[=\n\t""'\\[\]]", "").Trim();
        }
    }
}
