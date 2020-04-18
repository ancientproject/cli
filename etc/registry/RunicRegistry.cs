namespace rune.etc.registry
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using Flurl.Http;
    using Flurl.Http.Content;
    using Newtonsoft.Json;

    public class RunicRegistry : IRegistry
    {

        public static string Endpoint = "https://registry.runic.cloud";
        public static string Name = "registry.runic.cloud";

        public async Task<(Assembly, byte[], RuneSpec)> Fetch(string id)
        {
            // todo: logic for cache
            //var cacheFolder = EnsureFolders();
            //if(Directory.Exists(Path.Combine(cacheFolder, $"{id}")))

            if (!await Exist(id))
            {
                Console.WriteLine($"{":thought_balloon:".Emoji()} {"package".Nier()} '{id}' not found in '{Name}'".Color(Color.Orange));
                return default;
            }

            Console.Write($"{":thought_balloon:".Emoji()} FETCH '{Name}/{id}'...".Color(Color.DimGray));

            var task = Endpoint
                .WithTimeout(30)
                .AllowAnyHttpStatus()
                .AppendPathSegment($"/@/{id}")
                .GetAsync();

            new Task(() =>
            {
                while (!task.IsCompleted)
                {
                    Console.Write(".".Color(Color.Gray));
                    Task.Delay(500).Wait();
                }
            }).Start();

            var req = await task;

            if (!req.IsSuccessStatusCode)
            {
                Console.WriteLine($" {"FAIL".Color(Color.Red)}");
                Console.WriteLine($"{":thought_balloon:".Emoji()} {"package".Nier()} '{Name}/{id}' failed fetch files.".Color(Color.Orange));
                return default;
            }
            Console.WriteLine(" OK".Color(Color.GreenYellow));

            var memory = new MemoryStream();
            await req.Content.CopyToAsync(memory);

            var result = await RunePackage.Unwrap(memory.ToArray());


            Console.Write($"{":thought_balloon:".Emoji()} Extract '{id}-{result.Version}'...".Color(Color.DimGray));

            var packageFolder = Path.Combine(EnsureFolders(), $"{result.ID}/{result.Version}");

            if (!Directory.Exists(packageFolder))
                Directory.CreateDirectory(packageFolder);

            var targetFile = Path.Combine(packageFolder, "target.rpkg");
            var extractFolder = new DirectoryInfo(Path.Combine(packageFolder, "target"));

            File.WriteAllBytes(targetFile, result.Content.ToArray());


            ZipFile.ExtractToDirectory(targetFile, extractFolder.FullName);


            Console.WriteLine($".. OK".Color(Color.DimGray));


            if (result.Spec.Type == RuneSpec.PackageType.Binary)
            {
                var targetDll = extractFolder.EnumerateFiles("*.dll", SearchOption.AllDirectories).First();
                var targetAssembly = Assembly.LoadFile(targetDll.FullName);
                return (targetAssembly, File.ReadAllBytes(targetDll.FullName), result.Spec);
            }
            else
            {
                var targetDll = extractFolder.EnumerateFiles("*.cs", SearchOption.AllDirectories).First();
                var raw = CSharpCompile.Build(id, await File.ReadAllTextAsync(targetDll.FullName));
                return (Assembly.Load(raw), raw, result.Spec);
            }
            
        }

        public async Task<bool> Publish(FileInfo pkg, RuneSpec spec)
        {
            var key = Config.Get("credentials", "apiKey");
            if (key is null)
            {
                Console.WriteLine($"{":thought_balloon:".Emoji()} Fail find credentials...".Color(Color.Red));
                return false;
            }
            Console.Write($"{":thought_balloon:".Emoji()} Publish '{spec.ID}' with version '{spec.Version}' to registry...".Color(Color.Gray));

            var task = Endpoint
                .WithTimeout(30)
                .AllowAnyHttpStatus()
                .AppendPathSegments("/@/")
                .WithHeader("X-Rune-Key", key)
                .PutAsync(new MultipartFormDataContent
                {
                    {
                        new FileContent(pkg.FullName),
                        "file", 
                        Path.GetFileName(pkg.FullName)
                    }
                });
            new Task(() =>
            {
                while (!task.IsCompleted)
                {
                    Console.Write(".".Color(Color.Gray));
                    Task.Delay(500).Wait();
                }
            }).Start();

            var result = await task;
            
            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine($" {"OK".Color(Color.GreenYellow)}");
                return true;
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(await result.Content.ReadAsStringAsync()); 
            Console.WriteLine($" {"FAIL".Color(Color.Red)}");
            Console.WriteLine($"{":thought_balloon:".Emoji()} [{$"{(int)result.StatusCode}".Color(Color.Orange)}] {error.Message.Color(Color.Red)}");
            return false;
        }

        public async Task<bool> Exist(string id)
        {
            Console.Write($"{":thought_balloon:".Emoji()} PROPFIND '{Name}/{id}'...".Color(Color.DimGray));

            var task = Endpoint
                .WithTimeout(30)
                .AllowAnyHttpStatus()
                .AppendPathSegment($"/@/{id}")
                .SendAsync(new HttpMethod("PROPFIND"));

            new Task(() =>
            {
                while (!task.IsCompleted)
                {
                    Console.Write(".".Color(Color.Gray));
                    Task.Delay(500).Wait();
                }
            }).Start();

            var request = await task;

            if (request.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine(" OK".Color(Color.GreenYellow));

                return true;
            }
            Console.WriteLine($" {request.StatusCode}".Color(Color.Red));
            return false;
        }

        private string EnsureFolders()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(path, ".rune/packages");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }


        public class ErrorResponse
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}