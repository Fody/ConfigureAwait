using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ConfigureAwait;
using Mono.Cecil;
using NUnit.Framework;

public static class AssemblyWeaver
{
    public static Assembly Assembly;

    static AssemblyWeaver()
    {
        BeforeAssemblyPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,  @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.dll"));
        var beforePdbPath = Path.ChangeExtension(BeforeAssemblyPath, "pdb");

#if (!DEBUG)
        BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
        beforePdbPath = beforePdbPath.Replace("Debug", "Release");
#endif
        AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
        var afterPdbPath = beforePdbPath.Replace(".pdb", "2.pdb");

        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
        if (File.Exists(beforePdbPath))
            File.Copy(beforePdbPath, afterPdbPath, true);

        var readerParameters = new ReaderParameters();
        var writerParameters = new WriterParameters();

        if (File.Exists(afterPdbPath))
        {
            readerParameters.ReadSymbols = true;
            writerParameters.WriteSymbols = true;
        }

        using (var moduleDefinition = ModuleDefinition.ReadModule(BeforeAssemblyPath, readerParameters))
        using (var defaultAssemblyResolver = new DefaultAssemblyResolver())
        {
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition,
                AssemblyResolver = defaultAssemblyResolver,
                LogInfo = LogInfo,
                LogWarning = LogWarning,
                LogError = LogError,
                DefineConstants = new[] {"DEBUG"} // Always testing the debug weaver
            };

            weavingTask.Execute();
            moduleDefinition.Write(AfterAssemblyPath, writerParameters);
        }

        Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

    public static string BeforeAssemblyPath;
    public static string AfterAssemblyPath;

    private static void LogInfo(string message)
    {
        Infos.Add(message);
    }

    private static void LogWarning(string message)
    {
        Warnings.Add(message);
    }

    private static void LogError(string message)
    {
        Errors.Add(message);
    }

    public static List<string> Infos = new List<string>();
    public static List<string> Warnings = new List<string>();
    public static List<string> Errors = new List<string>();
}