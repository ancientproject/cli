namespace rune.etc
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using ancient.runtime;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;

    public class CSharpCompile
    {
        public static async Task<byte[]> BuildAsync(string id, string code)
        {
            Console.Write($"{":thought_balloon:".Emoji()} Mount '{id}'...".Color(Color.DimGray));
            var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            async Task<EmitResult> _compile()
            {
                var dd = typeof(Enumerable).GetTypeInfo().Assembly.Location;
                var coreDir = Directory.GetParent(dd);
                var refs = new List<MetadataReference>();
                
                refs.AddRange(new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

                    MetadataReference.CreateFromFile($"{Path.Combine(coreDir.FullName, "netstandard.dll")}"),
                    MetadataReference.CreateFromFile($"{Path.Combine(coreDir.FullName, "System.Runtime.dll")}"),
                    MetadataReference.CreateFromFile($"{Path.Combine(coreDir.FullName, "System.Runtime.Extensions.dll")}"),
                    MetadataReference.CreateFromFile($"{Path.Combine(coreDir.FullName, "Microsoft.CSharp.dll")}"),

                    MetadataReference.CreateFromFile(typeof(ldx).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IDevice).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location)
                });

                var compilation = CSharpCompilation.Create($"{id}")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Debug))
                    .AddReferences(refs)
                    .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));
                await Task.Delay(1);
                return compilation.Emit(temp);
            }

            var result = await RuneTask.Fire(_compile, x => x.Success);

            if (result.Success)
                return File.ReadAllBytes(temp);
            foreach (var diagnostic in result.Diagnostics)
                Console.WriteLine(diagnostic.ToString().Color(Color.Red));
            return null;
        }
    }
}