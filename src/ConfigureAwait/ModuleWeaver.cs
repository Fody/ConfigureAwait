using System;
using System.Linq;
using Mono.Cecil;

namespace ConfigureAwait
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public Action<string> LogWarning { get; set; }

        public Action<string> LogError { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public IAssemblyResolver AssemblyResolver { get; set; }

        public string[] DefineConstants { get; set; }

        public void Execute()
        {
        }
    }
}