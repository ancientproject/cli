namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;
    using Newtonsoft.Json;

    public class SchemeCommand : RuneCommand<SchemeCommand>
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune scheme",
                FullName = "Ancient device-mapper initializer",
                Description = "Initializes empty map file for Ancient VM Devices"
            };
            app.HelpOption("-h|--help");
            var cmd = new SchemeCommand();
            app.OnExecute(() => cmd.Execute());

            return app;
        }

        public int Execute()
        {
            var dir = Directory.GetCurrentDirectory();
            var scheme = Path.Combine(dir, "device.scheme");

            if(new FileInfo(scheme).Exists)
            {
                Console.WriteLine($"{":dizzy:".Emoji()} '{"device.scheme".Color(Color.Gray)}' {"already".Nier(0).Color(Color.Red)} exist.");
                return 1;
            }

            var sc = new DeviceScheme();

            sc.scheme.Add("memory", "0x0");
            sc.scheme.Add("bios", "0x45");
            
            File.WriteAllText(scheme, JsonConvert.SerializeObject(sc, Formatting.Indented));
            Console.WriteLine($"{":dizzy:".Emoji()} {"Success".Nier().Color(Color.GreenYellow)} write device scheme to '{"./device.scheme".Color(Color.Gray)}'");
            return 0;
        }
    }
}