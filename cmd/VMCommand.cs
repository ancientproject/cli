﻿namespace rune.cmd
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using cli;
    using etc;
    using Internal;

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
                var buildResult = dotnetBuild.Execute(true);
                return buildResult != 0 ? buildResult : await vm.Execute(isDebug, keepMemory, fastWrite, isInteractive);
            });
            return app;
        }

        internal async Task<int> Execute(CommandOption isDebug, CommandOption keepMemory, CommandOption fastWrite, CommandOption isInteractive)
        {
            var dir = Directory.GetCurrentDirectory();
            if (!this.Validate(dir))
                return await Fail();

            var ancient_home = Environment.GetEnvironmentVariable("ANCIENT_HOME", EnvironmentVariableTarget.User);

            if (ancient_home is null)
                return await Fail($"Env variable 'ANCIENT_HOME' is not set.");
            if (!new DirectoryInfo(ancient_home).Exists)
                return await Fail($"Env variable 'ANCIENT_HOME' is invalid.");

            var vm_home = Path.Combine(ancient_home, "vm");
            var vm_bin = Path.Combine(vm_home, "vm.exe");

            if (!new DirectoryInfo(vm_home).Exists || !new FileInfo(vm_bin).Exists)
                throw new InvalidOperationException($"Ancient VM is not installed.");

            var argBuilder = new List<string>();

            if (!Directory.Exists("obj"))
                Directory.CreateDirectory("obj");

            var files = Directory.GetFiles(Path.Combine("obj"), "*.*")
                .Where(x => x.EndsWith(".dlx") || x.EndsWith(".bios")).ToArray();

            if (files.Any())
                argBuilder.Add($"\"{Path.Combine("obj", Path.GetFileNameWithoutExtension(files.First()))}\"");

            var external = new ExternalTools(vm_bin, string.Join(" ", argBuilder));
            return external
                .WithEnv("VM_ATTACH_DEBUGGER", isDebug.BoolValue.HasValue)
                .WithEnv("VM_KEEP_MEMORY", keepMemory.BoolValue.HasValue)
                .WithEnv("VM_MEM_FAST_WRITE", fastWrite.BoolValue.HasValue)
                .WithEnv("REPL", isInteractive.BoolValue.HasValue)
                .WithEnv("CLI", true)
                .WithEnv("CLI_WORK_PATH", dir)

                .Start()
                .Wait()
                .ExitCode();
        }
    }
}