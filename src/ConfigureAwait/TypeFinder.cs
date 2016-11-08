using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace ConfigureAwait
{
    internal class TypeFinder
    {
        private readonly ModuleDefinition moduleDefinition;
        private readonly IEnumerable<TypeDefinition> msCoreTypes;

        public TypeFinder(IAssemblyResolver assemblyResolver, ModuleDefinition moduleDefinition)
        {
            this.moduleDefinition = moduleDefinition;
            var msCoreLibDefinition = assemblyResolver.Resolve("mscorlib");
            msCoreTypes = msCoreLibDefinition.MainModule.ExportedTypes.Select(s => s.Resolve()).Concat(msCoreLibDefinition.MainModule.Types);
        }

        public TypeDefinition GetMSCorLibTypeDefinition(string typeName)
        {
            return msCoreTypes.First(x => x.FullName == typeName);
        }

        public TypeReference GetMSCorLibTypeReference(string typeName)
        {
            var typeDefinition = GetMSCorLibTypeDefinition(typeName);
            return moduleDefinition.ImportReference(typeDefinition);
        }
    }
}