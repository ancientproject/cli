namespace rune.etc.ExternalCommand
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using MoreLinq.Extensions;

    public class Windows
    {

        public async Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait)
        {
            var tmp = Path.GetTempPath();
            var uid = Guid.NewGuid().ToString();
            var instanceID = Path.Combine(tmp, uid);
            var cwd = Directory.GetCurrentDirectory();


            var execute = Path.Combine(instanceID, "execute.bat");
            var command = Path.Combine(instanceID, "command.bat");

            var stdout = Path.Combine(instanceID, "_.stdout");
            var stderr = Path.Combine(instanceID, "_.stderr");
            var status = Path.Combine(instanceID, "_.status");

            void cleanUp()
            {
                Directory.GetFiles(instanceID).Pipe(File.Delete).ForEach(x => Console.WriteLine($"Remove '{x}'."));
                Directory.Delete(instanceID, true);
            }
            Directory.CreateDirectory(instanceID);


            void WriteExecute()
            {
                var build = new StringBuilder();

                build.AppendLine("@echo off");
                build.AppendLine($"call \"{command}\" > \"{stdout}\" 2> \"{stderr}\"");
                build.AppendLine($"(echo %ERRORLEVEL%) > \"{status}\"");

                File.WriteAllText(execute, build.ToString());
            }
            void WriteCommand()
            {
                var build = new StringBuilder();

                build.AppendLine("@echo off");
                build.AppendLine($"chcp 65001>nul");
                build.AppendLine($"cd /d \"{cwd}\"");
                build.AppendLine(cmd);

                File.WriteAllText(command, build.ToString());
            }


            async Task<ElevateResult> Elevate()
            {
                var build = new List<string>();

                build.Add("powershell.exe");
                build.Add("Start-Process");
                build.Add("-FilePath");
                build.Add($"{execute}");
                build.Add("-WindowStyle hidden");


                var info = new ProcessStartInfo("cmd", $"/c \"{string.Join(" ", build)}\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                var proc = new Process { StartInfo = info };
                proc.Start();

                await proc.WaitForExitAsync();
                var err = await proc.StandardError.ReadToEndAsync();
                var @out = await proc.StandardOutput.ReadToEndAsync();

                if (err.Contains("canceled by the user"))
                    return ElevateResult.PERMISSION_DENIED;
                if (!err.Any()) return ElevateResult.SUCCESS;

                File.WriteAllText(stderr, err);
                return ElevateResult.ERROR;
            }



            WriteExecute();
            WriteCommand();

            var elevateStatus = await Elevate();

            async Task WaitForStatus()
            {
                var info = new FileInfo(status);
                var startDate = DateTimeOffset.UtcNow;

                while (!info.Exists || info.Length <= 2)
                {
                    await Task.Delay(300);
                    var outInfo = new FileInfo(stdout);
                    if (!outInfo.Exists)
                    {
                        elevateStatus = ElevateResult.PERMISSION_DENIED;
                        break;
                    }

                    if (DateTimeOffset.UtcNow - startDate > timeoutWait)
                    {
                        elevateStatus = ElevateResult.TIMEOUT;
                        break;
                    }
                    info = new FileInfo(status);
                }
            }



            await WaitForStatus();


            Expression<Func<string, string>> getData =
                s => s.AsFile().When(x => x.Exists, x => x.ReadAll());

            var exp = getData.Compile();

            var result = new ExecuteResult(elevateStatus,
                exp(stdout),
                exp(stderr),
                exp(status));



            cleanUp();
            return result;
        }
    }

    public static class Ex
    {
        public static T When<T>(this FileInfo info, Func<FileInfo, bool> condition, Func<FileInfo, T> actor)
            => condition(info) ? actor(info) : default;
        public static string ReadAll(this FileInfo info) => File.ReadAllText(info.FullName);
    }
}