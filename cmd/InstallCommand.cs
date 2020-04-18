namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class InstallCommand : RuneCommand<InstallCommand>, IWithProject
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune install",
                FullName = "Install device package.",
                Description = "Install device package from ancient registry"
            };


            app.HelpOption("-h|--help");
            var package = app.Argument("<package>", "package name");
            var registry = app.Option("--registry <url>", "registry url", CommandOptionType.SingleValue);
            var cmd = new InstallCommand();
            var restore = new RestoreCommand();
            app.OnExecute(async () =>
            {
                if (string.IsNullOrEmpty(package.Value))
                    return await Fail("Argument <package> is null.");

                var result = await cmd.Execute(package.Value, registry);
                if (result != 0)
                    return result;
                return await restore.Execute(registry);
            });

            return app;
        }

        public async Task<int> Execute(string package, CommandOption registryOption)
        {
            var registry = 
                registryOption.HasValue() ? 
                    registryOption.Value() : 
                    Config.Get("core", "registry", "github+https://github.com/ancientproject");

            var dir = Directory.GetCurrentDirectory();

            if (!this.Validate(dir))
                return await Fail();

            if (Indexer.FromLocal().UseLock().Exist(package))
            {
                Console.WriteLine($"{":page_with_curl:".Emoji()} '{package}' is already {"found".Nier(0).Color(Color.Red)} in project.");
                return await Fail();
            }


            if(!await Registry.By(registry).Exist(package))
            {
                Console.WriteLine($"{":page_with_curl:".Emoji()} '{package}' is {"not".Nier(0).Color(Color.Red)} found in '{registry}' registry.");
                return await Fail();
            }

            try
            {
                var (asm, bytes, spec) = await Registry.By(registry).Fetch(package);

                if (asm is null)
                    return await Fail();

                Indexer.FromLocal()
                    .UseLock()
                    .SaveDep(asm, bytes, registry);
                AncientProject.FromLocal().AddDep(package, $"{spec.Version}", DepVersionKind.Fixed);
                Console.WriteLine($"{":movie_camera:".Emoji()} '{package}-{spec.Version}' save to deps is {"success".Nier(0).Color(Color.GreenYellow)}.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString().Color(Color.Red));
                return await Fail(2);
            }
            return await Success();
        }
    }
}