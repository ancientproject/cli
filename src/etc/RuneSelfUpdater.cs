namespace rune.etc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using cmd;
    using Flurl;
    using Flurl.Http;
    using Newtonsoft.Json;
    using NuGet.Versioning;

    public static class RuneSelfUpdater
    {
        private static NuGetVersion latest;
        private static NuGetVersion current;
        private static string UpdateCmd;
        public static void Check() => CheckAsync().Wait();

        public static async Task CheckAsync()
        {
            if(Config.Get("update", "disabled", false))
                return;

            var url = Config.Get("update", "origin", "https://cluster.ruler.runic.cloud");
            var release = await url.AppendPathSegment("/api/@me/version/latest")
                .WithHeader("User-Agent", $"RuneCLI/{HelpCommand.GetVersion()}")
                .GetJsonAsync<RuneVersion>();


            latest = NuGetVersion.Parse(release.Version.Sem.Version);
            current = NuGetVersion.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            UpdateCmd = release.UpdateCommand;

        }

        public static void DisplayIfNeeded()
        {
            if (current < latest)
            {
                var len = 56;
                Console.WriteLine($"╭{new string('─', len)}╮".Color(Color.Coral));
                Console.WriteLine($"│{new string(' ', len)}│".Color(Color.Coral));

                Console.WriteLine($"{"│".Color(Color.Coral)}{CenteredText($"Update available {current} → {latest}", len)}{"│".Color(Color.Coral)}");
                Console.WriteLine($"{"│".Color(Color.Coral)}{CenteredText($"Run {UpdateCmd} to update", len)}{"│".Color(Color.Coral)}");

                Console.WriteLine($"│{new string(' ', len)}│".Color(Color.Coral));
                Console.WriteLine($"╰{new string('─', len)}╯".Color(Color.Coral));
            }
        }

        private static string CenteredText(string text, int width)
        {
            var startPoint = (width - text.Length) / 2;
            return $"{new string(' ', startPoint)}{text}{new string(' ', startPoint)}";
        }
    }

    


    public class RuneVersion
    {
        [JsonProperty("version")]
        public VersionModel Version { get; set; }
        [JsonProperty("update_command")]
        public string UpdateCommand { get; set; }


        public class VersionModel
        {
            [JsonProperty("full")]
            public string Full { get; set; }

            [JsonProperty("sem")]
            public SemVersionModel Sem { get; set; }

            public partial class SemVersionModel
            {
                [JsonProperty("options")]
                public OptionsModel Options { get; set; }

                [JsonProperty("loose")]
                public bool Loose { get; set; }

                [JsonProperty("includePrerelease")]
                public bool IncludePrerelease { get; set; }

                [JsonProperty("raw")]
                public string Raw { get; set; }

                [JsonProperty("major")]
                public long Major { get; set; }

                [JsonProperty("minor")]
                public long Minor { get; set; }

                [JsonProperty("patch")]
                public long Patch { get; set; }

                [JsonProperty("prerelease")]
                public List<string> Prerelease { get; set; }

                [JsonProperty("build")]
                public List<object> Build { get; set; }

                [JsonProperty("version")]
                public string Version { get; set; }


                public class OptionsModel
                {
                    [JsonProperty("loose")]
                    public bool Loose { get; set; }

                    [JsonProperty("includePrerelease")]
                    public bool IncludePrerelease { get; set; }
                }
            }
        }
    }

    

    

    
}