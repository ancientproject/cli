namespace rune.etc
{
    using System;
    using System.IO;

    public static class Dirs
    {
        public static DirectoryInfo RootFolder =>
            new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rune"));
        public static FileInfo ConfigFile =>
            new FileInfo(Path.Combine(RootFolder.FullName, "@.ini"));
        public static DirectoryInfo VMFolder 
            => new DirectoryInfo(Path.Combine(RootFolder.FullName, "vm"));
        public static DirectoryInfo CompilerFolder
            => new DirectoryInfo(Path.Combine(RootFolder.FullName, "acc"));


        public static void Ensure()
        {
            if(!RootFolder.Exists)
                RootFolder.Create();
            if (!VMFolder.Exists)
                VMFolder.Create();
            if (!CompilerFolder.Exists)
                CompilerFolder.Create();
        }
    }
}