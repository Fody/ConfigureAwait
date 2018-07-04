using Mono.Cecil;

namespace ConfigureAwait.Fody.Utilities
{
    public class TypeReferenceProvider
    {
        public TypeReferenceProvider(TypeProvider typeProvider, ModuleDefinition moduleDefinition)
        {
            ConfiguredTaskAwaitableReference =
                moduleDefinition.ImportReference(typeProvider.ConfiguredTaskAwaitableDefinition);
            ConfiguredTaskAwaiterReference =
                moduleDefinition.ImportReference(typeProvider.ConfiguredTaskAwaiterDefinition);

            GenericConfiguredTaskAwaitableReference =
                moduleDefinition.ImportReference(typeProvider.GenericConfiguredTaskAwaitableDefinition);
            GenericConfiguredTaskAwaiterReference =
                moduleDefinition.ImportReference(typeProvider.GenericConfiguredTaskAwaiterDefinition);

            TaskConfigureAwaitMethodReference =
                moduleDefinition.ImportReference(typeProvider.TaskConfigureAwaitMethodDefinition);
            GenericTaskReference = moduleDefinition.ImportReference(typeProvider.GenericTaskDefinition);
            GenericTaskConfigureAwaitMethodReference =
                moduleDefinition.ImportReference(typeProvider.GenericTaskConfigureAwaitMethodDefinition);
        }

        public TypeReference ConfiguredTaskAwaitableReference { get; }
        public TypeReference ConfiguredTaskAwaiterReference { get; }

        public TypeReference GenericConfiguredTaskAwaitableReference { get; }
        public TypeReference GenericConfiguredTaskAwaiterReference { get; }

        public MethodReference TaskConfigureAwaitMethodReference { get; set; }
        public TypeReference GenericTaskReference { get; set; }
        public MethodReference GenericTaskConfigureAwaitMethodReference { get; set; }
    }
}