namespace rune.etc
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;

    public class RuneTask
    {
        public static async ValueTask<T> Fire<T>(Func<Task<T>> target, Func<T, bool> successPredicate)
        {
            var task = target();

            new Task(() =>
            {
                while (!task.IsCompleted)
                {
                    Console.Write(".".Color(Color.Gray));
                    Task.Delay(500).Wait();
                }
            }).Start();

            var result = await task;


            Console.WriteLine(successPredicate(result) ? 
                $" OK".Color(Color.GreenYellow) : 
                $" FAIL".Color(Color.Red));

            return result;
        }
    }
}