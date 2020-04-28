namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;
    using static System.Console;

    public class RemoveCommand : RuneCommand<RemoveCommand>
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune remove",
                FullName = "Remove device package.",
                Description = "Remove device package from ancient project"
            };


            app.HelpOption("-h|--help");
            var package = app.Argument("<package>", "package name");
            var cmd = new RemoveCommand();
            app.OnExecute(() => cmd.Execute(package.Value));
            return app;
        }

        public int Execute(string id)
        {
            if (!Indexer.FromLocal().UseLock().Exist(id))
            {
                WriteLine($"{":loudspeaker:".Emoji()} '{$"{id}".Color(Color.Gray)}' {"not".Nier(0).Color(Color.Red)} found.");
                return 1;
            }
            Indexer.FromLocal().UseLock().GetVersion(id, out var version).RevDep(id);
            WriteLine($"{":loudspeaker:".Emoji()} remove '{$"{id}-{version}".Color(Color.Gray)}' {"success".Nier(0).Color(Color.GreenYellow)}.");
            return 0;
        }
    }
}