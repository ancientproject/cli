namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using ancient.runtime.tools;
    using cli;
    using etc;
    using Internal;

    public class ViewCommand : RuneCommand<ViewCommand>
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune view",
                FullName = "View file.",
                Description = "View file as hex table."
            };

            app.HelpOption("-h|--help");
            var cmd = new ViewCommand();
            var file = app.Argument("<file>", "file name");
            app.OnExecute(() => cmd.Execute(file.Value));

            return app;
        }

        public Task<int> Execute(string file)
        {
            if (file is null)
                return Fail($"<file> argument is null");

            var info = new FileInfo(file);

            if (!info.Exists)
            {
                Console.WriteLine($"{":page_with_curl:".Emoji()} '{info}' not {"found".Nier(0).Color(Color.Red)}.");
                return Fail();
            }

            Console.WriteLine(ByteArrayUtils.PrettyHexDump(File.ReadAllBytes(file)));
            return Success();
        }
    }
}