namespace rune.etc
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using registry;

    internal enum RegistryType
    {
        github,
        runic
    }

    public class Registry
    {
        public static IRegistry By(string uri)
        {
            var rex = new Regex(@"(?<type>github|runic)\+?(?<url>https\:[\/.\w]+)?").Match(uri);
            var url = rex.Groups["url"].Value;
            switch (Enum.Parse<RegistryType>($"{rex.Groups["type"].Value}"))
            {
                case RegistryType.github:
                    return new GitHubOrgRegistry(url);
                case RegistryType.runic:
                    return new RunicRegistry();
                default:
                    throw new NotSupportedException();
            }
        }
    }
    public interface IRegistry
    {
        Task<bool> Exist(string id);
        Task<(Assembly assembly, byte[] raw, RuneSpec spec)> Fetch(string id);

        Task<bool> Publish(FileInfo pkg, RuneSpec spec);
    }
}