using System.Collections.Generic;
using System.Linq;
using ConfigureAwait.Fody.Extensions;
using ConfigureAwait.Fody.Settings;
using ConfigureAwait.Fody.Utilities;
using Fody;
using GlobalConfigureAwait.Extensions;
using Mono.Cecil;

namespace ConfigureAwait.Fody
{
    public class ModuleWeaver : BaseModuleWeaver, ITypeFinder
    {
        public override bool ShouldCleanReference => true;

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
            yield return "System.Threading.Tasks";
        }

        public override void Execute()
        {
            var assemblyLevelSettings = new AssemblyLevelSettings(ModuleDefinition.Assembly);
            if (assemblyLevelSettings.AssemblyConfigureAwait.HasValue)
                LogInfo(
                    $"Detected assembly wide configuration (ConfigureAwait({assemblyLevelSettings.AssemblyConfigureAwait.Value}))");

            var typeProvider = new TypeProvider(this);
            var typeReferenceProvider = new TypeReferenceProvider(typeProvider, ModuleDefinition);
            var asyncIlHelper = new AsyncIlHelper(typeProvider, typeReferenceProvider, ModuleDefinition);

            var types = ModuleDefinition.GetTypes().ToList();
            foreach (var type in types)
                ProcessType(assemblyLevelSettings, type, asyncIlHelper);
        }

        private void ProcessType(AssemblyLevelSettings assemblySettings, TypeDefinition type, AsyncIlHelper asyncIlHelper)
        {
            if (!type.HasMethods || type.IsCompilerGenerated() && type.IsIAsyncStateMachine())
                return;

            var typeSettings = new TypeLevelSettings(type, assemblySettings);
            foreach (var method in type.Methods)
            {
                var methodSettings = new MethodLevelSettings(method, typeSettings);
                var configureAwait = methodSettings.GetConfigureAwait();
                if (configureAwait == null)
                    continue;

                var asyncStateMachineType = method.GetAsyncStateMachineType();
                if (asyncStateMachineType == null)
                {
                    if (methodSettings.MethodConfigureAwait.HasValue)
                        LogWarning($"ConfigureAwaitAttribue applied to non-async method '{method.FullName}'.");
                    continue;
                }

                asyncIlHelper.AddAwaitConfigToAsyncMethod(asyncStateMachineType, configureAwait.Value);
            }
        }
    }
}