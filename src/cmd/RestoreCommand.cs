namespace rune.cmd
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class RestoreCommand : RuneCommand<RestoreCommand>, IWithProject
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune restore",
                FullName = "Restore packages",
                Description = "Restore packages from current project"
            };


            app.HelpOption("-h|--help");
            var cmd = new RestoreCommand();
            var registry = app.Option("--registry <url>", "registry url", CommandOptionType.SingleValue);
            app.OnExecute(() => cmd.Execute(registry));
            return app;
        }


        public async Task<int> Execute(CommandOption registryOption)
        {
            var registry = registryOption.HasValue() ? registryOption.Value() : "github+https://github.com/ancientproject";
            var dir = Directory.GetCurrentDirectory();
            if (!this.Validate(dir))
                return 1;
            var indexer = Indexer.FromLocal().UseLock();
            foreach (var package in AncientProject.FromLocal().deps.Select(x => x.Key).Where(package => !indexer.Exist(package)))
            {
                if(!await Registry.By(registry).Exist(package))
                {
                    Console.WriteLine($"{":page_with_curl:".Emoji()} '{package}' is {"not".Nier(0).Color(Color.Red)} found in '{registry}' registry.");
                    continue;
                }

                try
                {
                    var (asm, bytes, spec) = await Registry.By(registry).Fetch(package);

                    if (asm is null)
                    {
                        Console.WriteLine($"{":movie_camera:".Emoji()} '{package}' restore {"fail".Nier(0).Color(Color.Red)}.");
                        continue;
                    }

                    Indexer.FromLocal()
                        .UseLock()
                        .SaveDep(asm, bytes, spec);
                    Console.WriteLine($"{":movie_camera:".Emoji()} '{package}' restore {"success".Nier(0).Color(Color.GreenYellow)}.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{":movie_camera:".Emoji()} '{package}' restore {"fail".Nier(0).Color(Color.Red)}.");
                    Trace.WriteLine(e.ToString());
                    continue;
                }
            }
            return await Success();
        }
    }
}