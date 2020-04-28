namespace rune.cmd
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;
    using Ancient.ProjectSystem;
    using cli;
    using etc;
    using Internal;

    public class ClearCommand : RuneCommand<ClearCommand>
    {
        internal override CommandLineApplication Setup()
        {
            var app = new CommandLineApplication
            {
                Name = "rune clear",
                FullName = "Clear all deps.",
                Description = "Clearing deps in current project."
            };


            app.HelpOption("-h|--help");
            var cmd = new ClearCommand();
            app.OnExecute(() => cmd.Execute());
            return app;
        }

        public int Execute()
        {
            try
            {
                Indexer.FromLocal().UseLock().DropDeps();
                Console.WriteLine($"{":leaves:".Emoji()} clearing deps {"success".Nier(0).Color(Color.GreenYellow)}!");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"{":fallen_leaf:".Emoji()} clearing deps {"fail".Nier(1).Color(Color.Red)}!");
                return 1;
            }
            
        }
    }
}