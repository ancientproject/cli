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

            var req = await RuneTask.Fire(() => Endpoint
                .WithTimeout(30)
                .AllowAnyHttpStatus()
                .AppendPathSegment($"/@/{id}")
                .GetAsync(), x => x.IsSuccessStatusCode);

            if (!req.IsSuccessStatusCode)
            {
                Console.WriteLine($"{":thought_balloon:".Emoji()} {"package".Nier()} '{Name}/{id}' failed fetch files.".Color(Color.Orange));
                return default;
            }

            var memory = new MemoryStream();
            await req.Content.CopyToAsync(memory);

            var result = await RunePackage.Unwrap(memory.ToArray());



            var packageFolder = Path.Combine(EnsureFolders(), $"{result.ID}/{result.Version}");
            var targetFile = Path.Combine(packageFolder, "target.rpkg");
            var extractFolder = new DirectoryInfo(Path.Combine(packageFolder, "target"));

            if (!Directory.Exists(packageFolder))
            {
                Console.Write($"{":thought_balloon:".Emoji()} Extract '{id}-{result.Version}'...".Color(Color.DimGray));
                Directory.CreateDirectory(packageFolder);
                File.WriteAllBytes(targetFile, result.Content.ToArray());
                ZipFile.ExtractToDirectory(targetFile, extractFolder.FullName);
                Console.WriteLine($".. OK".Color(Color.DimGray));
            }

           


            if (result.Spec.Type == RuneSpec.PackageType.Binary)
            {
                var targetDll = extractFolder.EnumerateFiles("*.dll", SearchOption.AllDirectories).First();
                var targetAssembly = Assembly.LoadFile(targetDll.FullName);
                return (targetAssembly, File.ReadAllBytes(targetDll.FullName), result.Spec);
            }
            else
            {
                var targetDll = extractFolder.EnumerateFiles("*.cs", SearchOption.AllDirectories).First();
                var raw = await CSharpCompile.BuildAsync(id, await File.ReadAllTextAsync(targetDll.FullName));
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
            
            var result = await RuneTask.Fire(() => Endpoint
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
                }), x => x.IsSuccessStatusCode);

            if (result.IsSuccessStatusCode)
                return true;
            
            var error = JsonConvert.DeserializeObject<ErrorResponse>(await result.Content.ReadAsStringAsync());
            Console.WriteLine($"{":thought_balloon:".Emoji()} [{$"{(int)result.StatusCode}".Color(Color.Orange)}] {error.Message.Color(Color.Red)}");
            return false;
        }

        public async Task<bool> Exist(string id)
        {
            Console.Write($"{":thought_balloon:".Emoji()} PROPFIND '{Name}/{id}'...".Color(Color.DimGray));

            var request = await RuneTask.Fire(() => Endpoint
                .WithTimeout(30)
                .AllowAnyHttpStatus()
                .AppendPathSegment($"/@/{id}")
                .SendAsync(new HttpMethod("PROPFIND")), x => x.IsSuccessStatusCode);

            if (request.StatusCode == HttpStatusCode.OK)
                return true;
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