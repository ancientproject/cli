namespace rune.cmd
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using cli;
    using etc;
    using etc.ExternalCommand;
    using Internal;
    using MoreLinq;

    public class VMCommand : RuneCommand<VMCommand>, IWithProject
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune vm",
                FullName = "Ancient project execute in vm",
                Description = "Execute project in Ancient VM"
            };

            app.Command("install", InstallVM);

            app.HelpOption("-h|--help");
            var dotnetBuild = new BuildCommand();
            var vm = new VMCommand();
            var isDebug = app.Option("-d|--debug <bool>", "Is debug mode", CommandOptionType.BoolValue);
            var fastWrite = app.Option("-f|--fast_write <bool>", "Use fast-write mode?", CommandOptionType.BoolValue);
            var keepMemory = app.Option("-k|--keep_memory <bool>", "Keep memory?", CommandOptionType.BoolValue);
            var isInteractive = app.Option("-i|--interactive", "Start with interactive mode", CommandOptionType.BoolValue);
            app.OnExecute(async () =>
            {
                if (isInteractive.BoolValue.HasValue)
                    return await vm.Execute(isDebug, keepMemory, fastWrite, isInteractive);
                var buildResult = await dotnetBuild.Execute(true);
                return buildResult != 0 ? buildResult : await vm.Execute(isDebug, keepMemory, fastWrite, isInteractive);
            });
            return app;
        }

        internal void InstallACC(CommandLineApplication app)
        {
            app.Description = $"Install latest ancient compiler.";
            var force = app.Option("-f|--force", "Force install binaries?", CommandOptionType.BoolValue);

            bool isForce() => force.HasValue() && force.BoolValue != null && force.BoolValue.Value;
            app.OnExecute(async () =>
            {
                try
                {
                    if (!isForce() && Dirs.CompilerFolder.EnumerateFiles().Any())
                        return await Fail($"{":x:".Emoji()} {"Already".Nier(2)} installed. Try rune vm install compiler --force");


                    if (Dirs.CompilerFolder.EnumerateFiles().Any())
                        _ = Dirs.CompilerFolder.EnumerateFiles().Pipe(x => x.Delete()).ToArray();

                    var result = await Appx.By(AppxType.acc)
                        .DownloadAsync();
                    Console.Write($"{":open_file_folder:".Emoji()} Extract files");
                    await RuneTask.Fire(() =>
                        ZipFile.ExtractToDirectory(result.FullName, Dirs.CompilerFolder.FullName));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return await Success();
            });
        }

        internal void InstallVM(CommandLineApplication app)
        {
            app.Description = $"Install latest ancient VM.";
            var force = app.Option("-f|--force", "Force install binaries?", CommandOptionType.BoolValue);

            bool isForce() => force.HasValue() && force.BoolValue != null && force.BoolValue.Value;
            app.Command("compiler", InstallACC);
            app.OnExecute(async () =>
            {
                try
                {
                    if (!isForce() && Dirs.VMFolder.EnumerateFiles().Any())
                        return await Fail($"{":x:".Emoji()} {"Already".Nier(2)} installed. Try rune vm install --force");


                    if (Dirs.VMFolder.EnumerateFiles().Any())
                        _ = Dirs.VMFolder.EnumerateFiles().Pipe(x => x.Delete()).ToArray();

                    var result = await Appx.By(AppxType.vm)
                        .DownloadAsync();
                    Console.Write($"{":open_file_folder:".Emoji()} Extract files");
                    await RuneTask.Fire(() => 
                        ZipFile.ExtractToDirectory(result.FullName, Dirs.VMFolder.FullName));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return await Success();
            });
        }


        internal async Task<int> Execute(CommandOption isDebug, CommandOption keepMemory, CommandOption fastWrite, CommandOption isInteractive)
        {
            var dir = Directory.GetCurrentDirectory();
            if (!this.Validate(dir))
                return await Fail();

            if (!Dirs.Bin.VM.Exists)
                return await Fail($"VM is not installed. Try 'rune vm install'");


            var vm_bin = Dirs.Bin.VM.FullName;


            var argBuilder = new List<string>();

            if (!Directory.Exists("obj"))
                Directory.CreateDirectory("obj");

            var files = Directory.GetFiles(Path.Combine("obj"), "*.*")
                .Where(x => x.EndsWith(".dlx") || x.EndsWith(".bios")).ToArray();

            if (files.Any())
                argBuilder.Add($"\"{Path.Combine("obj", Path.GetFileNameWithoutExtension(files.First()))}\"");

            var external = new ExternalTools(vm_bin, string.Join(" ", argBuilder));

            var result = external
                .WithEnv("VM_ATTACH_DEBUGGER", isDebug.BoolValue.HasValue)
                .WithEnv("VM_KEEP_MEMORY", keepMemory.BoolValue.HasValue)
                .WithEnv("VM_MEM_FAST_WRITE", fastWrite.BoolValue.HasValue)
                .WithEnv("REPL", isInteractive.BoolValue.HasValue)
                .WithEnv("CLI", true)
                .WithEnv("CLI_WORK_PATH", dir);

            try
            {
                return result
                    .Start()
                    .Wait()
                    .ExitCode();
            }
            catch (Win32Exception e)
            {
                Console.WriteLine($"{":x:".Emoji()} {e.Message}");
                Console.WriteLine($"{"TODO"} try fix...");
                await OS.FireAsync($"chmod +x \"{vm_bin}\"");
            }
            return result
                .Start()
                .Wait()
                .ExitCode();
        }
    }
}