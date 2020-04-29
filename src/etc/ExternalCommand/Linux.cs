namespace rune.etc.ExternalCommand
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    public interface ICommandExecuter
    {
        Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait);
    }
    public class Linux : ICommandExecuter
    {
        public async Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait)
        {
            var binaries = new[] { "/usr/bin/zsh", "/bin/bash" }.Where(x => x.AsFile().Exists).ToArray();

            if (!binaries.Any())
                return new ExecuteResult(ElevateResult.UNK, "", "zsh/bash not found.", "-1");

            var bin = binaries.First();
            Console.WriteLine($"Detected '{bin}'..");
            var command = new List<string> {$"-c", $"'{cmd}'"};


            var proc = new Process
            {
                StartInfo = new ProcessStartInfo(bin, string.Join(" ", command))
                {
                }
            };
            Console.WriteLine($"Starting process '{bin}' when args '{string.Join(" ", command)}'...");

            await proc.WaitForExitAsync();

            var stderr = await proc.StandardError.ReadToEndAsync();
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            Console.WriteLine($"Complete execute {bin}..");
            var result = ElevateResult.UNK;

            if (stderr.Contains("Request dismissed") || stderr.Contains("Command failed") || stderr.Contains("Not authorized"))
                result = ElevateResult.PERMISSION_DENIED;
            else if (stderr.Any())
                result = ElevateResult.ERROR;
            else
                result = ElevateResult.SUCCESS;
            return new ExecuteResult(result, stdout, stderr, proc.ExitCode.ToString());
        }
    }

    public class ExecuteResult
    {
        public ExecuteResult(ElevateResult status, string stdout, string stderr, string code)
        {
            this.Code = code;
            this.Status = status;
            this.Stderr = stderr;
            this.Stdout = stdout;
        }
        public ElevateResult Status { get; set; }
        public string Code { get; set; }
        public string Stdout { get; set; }
        public string Stderr { get; set; }
    }

    public enum ElevateResult
    {
        UNK,
        PERMISSION_DENIED,
        ERROR,
        TIMEOUT,
        SUCCESS
    }
}