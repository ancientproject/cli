namespace rune.cmd
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using ancient.runtime;
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
                return await Fail($"Compiler is not installed. Try 'rune install compiler'");


            var acc_bin = Dirs.Bin.ACC.FullName;
            var files = Directory.GetFiles(directory, "*.asm");

            if (!files.Any())
                return await Fail($"'*.asm' sources code in '{directory}' for compile not found.");

            if(!files.Any(x => x.Contains("entry")))
                Console.WriteLine($"{":warning:".Emoji()} {"'entry.asm' file not found, maybe VM cannot start executing files.".Color(Color.Orange)}");

            try
            {
                foreach (var file in files) Compile(file, isTemp);
                return await Success();
            }
            catch (Win32Exception e) // AccessDenied on linux
            {
                Console.WriteLine($"{":x:".Emoji()} {e.Message}");
                return await Fail($"Run [chmod +x \"{acc_bin}\"] for resolve this problem.");
            }
        }

        private void Compile(string file, bool isTemp)
        {
            var directory = Directory.GetCurrentDirectory();
            var acc_bin = Dirs.Bin.ACC.FullName;
            var argBuilder = new List<string>();
            var outputDir = "bin";

            if (isTemp)
                outputDir = "obj";
            var Project = AncientProject.FromLocal();
            var fileName = Path.GetFileNameWithoutExtension(file);
            argBuilder.Add($"-o ./{outputDir}/{fileName}");
            if (Project.Extension != null)
                argBuilder.Add($"-e {Project.Extension}");
            argBuilder.Add($"-s \"{file}\"");

            var external = new ExternalTools(acc_bin, string.Join(" ", argBuilder));
            Directory.CreateDirectory(Path.Combine(directory, outputDir));
            external.Start().Wait().ExitCode();
            
        }
    }
}