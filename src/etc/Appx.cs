namespace rune.etc
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using cmd;
    using Flurl.Http;
    using Konsole;

    internal class Appx
    {
        private readonly string _owner;
        private readonly string _repo;
        private readonly AppxType _type;

        public Appx(string owner, string repo, AppxType type)
        {
            _owner = owner;
            _repo = repo;
            _type = type;
        }


        public async ValueTask<FileInfo> DownloadAsync()
        {
            var releases = await $"https://api.github.com/repos/{_owner}/{_repo}/releases"
                .WithHeader("User-Agent", $"RuneCLI/{HelpCommand.GetVersion()}")
                .GetJsonAsync<GithubRelease[]>();
            var release = releases.First();
            var targetFile = Config.Get($"github_{_type}", "file", $"{_type}-{OS}.zip");

            var asset = release.Assets.FirstOrDefault(x => x.Name == targetFile);

            if(asset is null)
                throw new Exception($"Failed find {targetFile} in latest release in '{_owner}/{_repo}'");


            using var handler = HttpClientDownloadWithProgress.Create(asset.BrowserDownloadUrl, 
                new FileInfo(Path.Combine(Dirs.CacheFolder.FullName, targetFile)));
            Console.WriteLine($"{":page_with_curl:".Emoji()} Download {asset.BrowserDownloadUrl}..");
            var pb = new ProgressBar(100, 0, '=');

            handler.ProgressChanged += (size, downloaded, percentage) =>
            {
                if(percentage != null)
                    pb.Refresh((int)percentage.Value, $"");
            };

            await handler.StartDownload();


            Console.WriteLine();

            return new FileInfo(Path.Combine(Dirs.CacheFolder.FullName, targetFile));
        }


        private string OS 
        {
            get
            {
                if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                    throw new NotSupportedException($"{RuntimeInformation.ProcessArchitecture} is not support.");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "win64";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "linux64";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "osx64";
                throw new NotSupportedException($"{RuntimeInformation.OSDescription} is not support.");
            }
        }

        public static Appx By(AppxType type)
        {
            var owner = Config.Get($"github_{type}", "owner", "ancientproject");
            var repo = Config.Get($"github_{type}", "repo", "VM");

            return new Appx(owner, repo, type);
        }
    }

    public enum AppxType
    {
        vm,
        acc
    }
}