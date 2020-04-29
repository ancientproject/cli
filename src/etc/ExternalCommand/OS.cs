namespace rune.etc.ExternalCommand
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    public static class OS
    {
        public static async ValueTask<ExecuteResult> FireAsync(string cmd)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await new Linux().Execute(cmd, TimeSpan.FromSeconds(2));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await new Windows().Execute(cmd, TimeSpan.FromSeconds(15));
            throw new Exception();
        }
    }
}