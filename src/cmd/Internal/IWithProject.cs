namespace rune.cmd.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Ancient.ProjectSystem;
    using etc;
    using Newtonsoft.Json;

    public interface IWithProject
    {
        
    }

    public static class WithProjectEx
    {
        public static bool Validate(this IWithProject _, string directory)
        {
            var projectFiles = Directory.GetFiles(directory, "*.rune.json");

            if (projectFiles.Length == 0)
            {
                Console.WriteLine($"{":fried_shrimp:".Emoji()} {"Couldn't".Nier().Color(Color.Red)} find a project to run. Ensure a project exists in {directory}.");
                return false;
            }
            if (projectFiles.Length > 1)
            {
                Console.WriteLine($"{":fried_shrimp:".Emoji()} {"Specify".Nier().Color(Color.Red)} which project file to use because this folder contains more than one project file..");
                return false;
            }
            return true;
        }
        public static bool ValidateRuneSpec(this IWithProject _, string directory, out RuneSpec specFile, out string path)
        {
            var projectFiles = Directory.GetFiles(directory, "*.rspec.json");
            specFile = default;
            path = string.Empty;
            if (projectFiles.Length == 0)
            {
                Console.WriteLine($"{":trident:".Emoji()} {"Couldn't".Nier().Color(Color.Red)} find a rune package spec. Ensure a project specification in {directory}.");
                return false;
            }
            if (projectFiles.Length > 1)
            {
                Console.WriteLine($"{":trident:".Emoji()} {"Specify".Nier().Color(Color.Red)} which spec file to use because this folder contains more than one spec file..");
                return false;
            }

            path = projectFiles.Single();

            try
            {
                specFile = JsonConvert.DeserializeObject<RuneSpec>(File.ReadAllText(path));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine($"{":trident:".Emoji()} {"Fail".Nier(1).Color(Color.Red)} parse specification file...");
                return false;
            }

            // TODO regex rules
            if (string.IsNullOrEmpty(specFile.ID))
            {
                Console.WriteLine($"{":trident:".Emoji()} [{"Validate".Nier(4).Color(Color.Red)}] ID can't empty.");
                return false;
            }

            if (specFile.Version is null)
            {
                Console.WriteLine($"{":trident:".Emoji()} [{"Validate".Nier(4).Color(Color.Red)}] Version can't empty.");
                return false;
            }




            return true;
        }
    }
}