namespace rune.cmd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class BuildCommand : RuneCommand<BuildCommand>, IWithProject
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune build",
                FullName = "Ancient project build",
                Description = "Build all files from project"
            };

            app.HelpOption("-h|--help");
            var type = app.Option("-t|--temp <bool>", "Is temp", CommandOptionType.BoolValue);
            var dotnetNew = new BuildCommand();
            app.OnExecute(() => dotnetNew.Execute(type.BoolValue.HasValue));

            return app;
        }

        public async Task<int> Execute(bool isTemp)
        {
            var directory = Directory.GetCurrentDirectory();
            if (!this.Validate(directory))
                return await Fail();

            if (!Dirs.Bin.ACC.Exists)
                return await Fail($"Compiler is not installed. Try 'rune vm install compiler'");


            var acc_bin = Dirs.Bin.ACC.FullName;


            var argBuilder = new List<string>();

            var files = Directory.GetFiles(directory, "*.asm");

            if (!files.Any())
                return await Fail($"'*.asm' sources code in '{directory}' for compile not found.");

            var outputDir = "bin";

            if (isTemp)
                outputDir = "obj";
            var Project = AncientProject.FromLocal();
            argBuilder.Add($"-o ./{outputDir}/{Project.Name}");
            if (Project.Extension != null)
                argBuilder.Add($"-e {Project.Extension}");
            argBuilder.Add($"-s \"{files.First()}\"");

            var external = new ExternalTools(acc_bin, string.Join(" ", argBuilder));
            Directory.CreateDirectory(Path.Combine(directory, outputDir));
            return external.Start().Wait().ExitCode();
        }
    }
}