namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class PublishCommand : RuneCommand<PublishCommand>, IWithProject, IExecuterAsync
    {
        private readonly CommandOption _registry;

        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune publish",
                FullName = "Ancient package pack and publish",
                Description = "Build rune package and send to registry"
            };

            app.HelpOption("-h|--help");
            var type = app.Option("-r|--registry <url>", "Registry url", CommandOptionType.SingleValue);
            app.OnExecute(new PublishCommand(type));

            return app;
        }

        public PublishCommand()
        {
        }

        public PublishCommand(CommandOption registry) => _registry = registry;

        public async Task<int> ExecuteAsync()
        {
            var registry = Registry.By(_registry.HasValue() ? _registry.Value() : Config.Get("core", "registry", "runic"));
            var directory = Directory.GetCurrentDirectory();
            if (!this.Validate(directory))
                return 1;
            Console.WriteLine($"{":thought_balloon:".Emoji()} preparing...".Color(Color.DimGray));

            if (!this.ValidateRuneSpec(directory, out var spec, out var path))
                return 1;
            var tempPkg = Path.Combine(directory, "obj");

            if (!new DirectoryInfo(tempPkg).Exists)
                Directory.CreateDirectory(tempPkg);
            tempPkg = Path.Combine(tempPkg, "temp.rpkg");

            if (new FileInfo(tempPkg).Exists)
            {
                Console.Write($"{":thought_balloon:".Emoji()} clear temporary files...".Color(Color.DimGray));
                File.Delete(tempPkg);
                Console.WriteLine($" OK".Color(Color.GreenYellow));

            }

            await using var mem = File.Open(tempPkg, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var pkg = new ZipArchive(mem, ZipArchiveMode.Create);

            if (!spec.Files.Any())
            {
                Console.WriteLine($"{":fried_shrimp:".Emoji()} Files enumeration is not {"defined".Nier(4).Color(Color.Red)} in specification file.");
                return await Fail();
            }
            Console.Write($"{":thought_balloon:".Emoji()} collecting files...".Color(Color.DimGray));

            foreach (var file in spec.Files)
            {
                if (!file.StartsWith("#/"))
                {
                    Console.WriteLine($" FAIL".Color(Color.Red));
                    Console.WriteLine($"{":fried_shrimp:".Emoji()} File '{file}' is not {"start".Nier(0).Color(Color.Red)} with '#/'.");
                    return await Fail();
                }

                var entity = new FileInfo(Path.Combine(directory, file.Remove(0, 2)));
                if (!entity.Exists)
                {
                    Console.WriteLine($" FAIL".Color(Color.Red));
                    Console.WriteLine($"{":fried_shrimp:".Emoji()} {"Couldn't".Nier().Color(Color.Red)} find '{file}' file in '{directory}'.");
                    continue;
                }
                pkg.CreateEntryFromFile(entity.FullName, Path.GetFileName(entity.FullName));
            }

            pkg.CreateEntryFromFile(path, "target.rspec");

            Console.WriteLine($" OK".Color(Color.GreenYellow));


            await mem.FlushAsync();

            pkg.Dispose();
            await mem.DisposeAsync();

            var outputFile = new FileInfo(Path.Combine(directory, $"{spec.ID}-{spec.Version}.rpkg"));

            if(outputFile.Exists)
                outputFile.Delete();

            File.Copy(tempPkg, Path.Combine(directory, $"{spec.ID}-{spec.Version}.rpkg"));

            return await registry.Publish(new FileInfo(tempPkg), spec) ? 
                await Success() :
                await Fail();
        }
    }
}