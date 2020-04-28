namespace rune.cmd
{
    using System.IO;
    using System.Linq;
    using Ancient.ProjectSystem;
    using cli;
    using DustInTheWind.ConsoleTools.InputControls;
    using etc;
    using Internal;
    using Newtonsoft.Json;

    public class NewCommand : RuneCommand<NewCommand>
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune new",
                FullName = "Ancient project initializer",
                Description = "Initializes empty project for Ancient VM"
            };

            app.HelpOption("-h|--help");

            var dotnetNew = new NewCommand();
            app.OnExecute(() => dotnetNew.CreateEmptyProject());
            return app;
        }

        private int CreateEmptyProject()
        {
            var projectName 
                = new ValueView<string>($"[1/4] {":drum:".Emoji()} Project Name:").WithDefault(Directory.GetCurrentDirectory().Split('/').Last()).Read();
            var version 
                = new ValueView<string>($"[2/4] {":boom:".Emoji()} Project Version:").WithDefault("0.0.0").Read();
            var desc 
                = new ValueView<string>($"[3/4] {":balloon:".Emoji()} Project Description:").WithDefault("").Read();
            var author 
                = new ValueView<string>($"[4/4] {":skull:".Emoji()} Project Author:").WithDefault("").Read();

            var dir = Directory.GetCurrentDirectory();

            var proj = new AncientProjectFile
            {
                name = projectName,
                version = version,
                author = author
            };

            proj.scripts.Add($"start", "echo 1");

            File.WriteAllText($"{Path.Combine(dir, $"{projectName}.rune.json")}", JsonConvert.SerializeObject(proj, Formatting.Indented));

            return 0;
        }
    }
}