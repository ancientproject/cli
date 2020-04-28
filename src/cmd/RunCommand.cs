namespace rune.cmd
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class RunCommand : RuneCommand<RunCommand>, IWithProject
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune run",
                FullName = "Ancient script runner",
                Description = "Run script from project"
            };


            app.HelpOption("-h|--help");
            var type = app.Argument("<script>", "script name");
            var dotnetNew = new RunCommand();
            app.OnExecute(() => dotnetNew.Execute(type.Value));
            return app;
        }

        public async Task<int> Execute(string value)
        {
            var directory = Directory.GetCurrentDirectory();
            if (!this.Validate(directory))
                return await Fail();
            var script = AncientProject.FromLocal().scripts.FirstOrDefault(x => x.Key.Equals(value, StringComparison.InvariantCultureIgnoreCase)).Value;

            if (script is null)
                return await Fail($"Command '{value}' not found.");
            Console.WriteLine($"trace :: call :> cmd /c '{script}'".Color(Color.DimGray));
            var proc = default(Process);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", $"/c \"{script}\"")
                    {
                        RedirectStandardError = true, 
                        RedirectStandardOutput = true
                    }
                };
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo("bash", $"-c \"{script}\"")
                    {
                        RedirectStandardError = true, 
                        RedirectStandardOutput = true
                    }
                };
            }

            proc.Start();
            proc.WaitForExit();

            var err = proc.StandardError.ReadToEnd();
            var @out = proc.StandardOutput.ReadToEnd();
            if(!string.IsNullOrEmpty(err )) Console.WriteLine($"{err}".Color(Color.Red));
            if(!string.IsNullOrEmpty(@out)) Console.WriteLine($"{@out}".Color(Color.DarkGray));

            return proc.ExitCode;
        }
    }
}