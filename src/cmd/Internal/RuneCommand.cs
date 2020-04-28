namespace rune.cmd.Internal
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;
    using cli;
    using etc;

    public abstract class RuneCommand<T> where T : RuneCommand<T>, new()
    {
        public static async Task<int> Run(string[] args)
        {
            var cmd = new T();

            var app = cmd.Setup();

            try
            {
                return await app.Execute(args);
            }
            catch (CommandParsingException parsingException)
            {
                return await Fail(parsingException.Message);
            }
            catch (Exception ex)
            {
                return await Fail(ex.ToString());
            }
        }

        internal abstract CommandLineApplication Setup();


        protected static Task<int> Success() => Task.FromResult(0);
        protected static Task<int> Fail() => Task.FromResult(1);
        protected static Task<int> Fail(int status) => Task.FromResult(status);
        protected static Task<int> Fail(string text)
        {
            Console.WriteLine($"{":x:".Emoji()} {text.Color(Color.Red)}");
            return Fail();
        }
        protected static Task<int> Success(string text)
        {
            Console.WriteLine($"{":heavy_check_mark:".Emoji()} {text.Color(Color.GreenYellow)}");
            return Success();
        }
    }
}