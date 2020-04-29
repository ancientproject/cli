namespace rune.etc
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;

    public class RuneTask
    {
        public static async ValueTask Fire(Action target) =>
            await Fire<int>(async () =>
            {
                await Task.Delay(1);
                try
                {
                    target();
                    return 0;
                }
                catch (Exception e)
                {
                    return 1;
                }
            }, _ => _ == 0);

        public static async ValueTask<T> Fire<T>(Func<T> target, Func<T, bool> successPredicate) =>
            await Fire<T>(async () =>
            {
                await Task.Delay(1);
                return target();
            }, successPredicate);

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