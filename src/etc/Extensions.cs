namespace rune.etc
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Extensions
    {
        public static string ReadToEnd(this FileInfo info)
        {
            if(!info.Exists)
                throw new FileNotFoundException($"'{info.FullName}' not exist.");
            return info.OpenText().ReadToEnd();
        }

        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);
            return tcs.Task;
        }

        public static FileInfo AsFile(this string str) => new FileInfo(str);
    }
}