using System;
using System.Linq;
using Mono.Cecil;

namespace ConfigureAwait.Fody.Utilities
{
    public class TypeProvider
    {
        public TypeProvider(ITypeFinder typeFinder)
        {
            ConfiguredTaskAwaitableDefinition =
                typeFinder.FindType("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
            ConfiguredTaskAwaiterDefinition = ConfiguredTaskAwaitableDefinition.NestedTypes.First();

            GenericConfiguredTaskAwaitableDefinition =
                typeFinder.FindType("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
            GenericConfiguredTaskAwaiterDefinition = GenericConfiguredTaskAwaitableDefinition.NestedTypes.First();

            TaskConfigureAwaitMethodDefinition =
                typeFinder.FindType("System.Threading.Tasks.Task").Methods
                    .First(x => x.Name == "ConfigureAwait");

            GenericTaskDefinition = typeFinder.FindType("System.Threading.Tasks.Task`1");
            GenericTaskConfigureAwaitMethodDefinition =
                GenericTaskDefinition.Methods.First(x => x.Name == "ConfigureAwait");
        }

        public TypeDefinition ConfiguredTaskAwaitableDefinition { get; }
        public TypeDefinition ConfiguredTaskAwaiterDefinition { get; }

        public TypeDefinition GenericConfiguredTaskAwaitableDefinition { get; }
        public TypeDefinition GenericConfiguredTaskAwaiterDefinition { get; }

        public MethodDefinition TaskConfigureAwaitMethodDefinition { get; }
        public TypeDefinition GenericTaskDefinition { get; }
        public MethodReference GenericTaskConfigureAwaitMethodDefinition { get; }
    }

    public interface ITypeFinder
    {
        Func<string, TypeDefinition> FindType { get; }
    }
}