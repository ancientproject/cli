﻿namespace rune.cmd
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class RestoreCommand : WithProject
    {
        public static async Task<int> Run(string[] args)
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
            try
            {
                return await app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString().Color(Color.Red));
                return 1;
            }
        }


        public async Task<int> Execute(CommandOption registryOption)
        {
            var registry = registryOption.HasValue() ? registryOption.Value() : "github+https://github.com/ancientproject";
            var dir = Directory.GetCurrentDirectory();
            if (!Validate(dir))
                return 1;
            var indexer = Indexer.FromLocal().UseLock();
            foreach (var (package, _) in AncientProject.FromLocal().deps)
            {
                if (!indexer.Exist(package))
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
            }
            return 0;
        }
    }
}