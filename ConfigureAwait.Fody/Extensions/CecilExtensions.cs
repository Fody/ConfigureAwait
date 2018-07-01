using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConfigureAwait.Fody.Extensions
{
    internal static class CecilExtensions
    {
        public static bool IsIAsyncStateMachine(this TypeDefinition typeDefinition)
        {
            if (typeDefinition == null || !typeDefinition.HasInterfaces)
                return false;

            return typeDefinition.Interfaces
                .Any(x => x.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine");
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
        {
            if (provider == null || !provider.HasCustomAttributes)
                return false;

            return provider.CustomAttributes
                .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }


        public static void InsertBefore(this ILProcessor processor, Instruction target,
            params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
                processor.InsertBefore(target, instruction);
        }

        public static TypeDefinition GetAsyncStateMachineType(this ICustomAttributeProvider provider)
        {
            if (provider == null || !provider.HasCustomAttributes)
                return null;

            return (TypeDefinition) provider.CustomAttributes
                .FirstOrDefault(a =>
                    a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                ?.ConstructorArguments[0].Value;
        }

        public static CustomAttribute GetConfigureAwaitAttribute(this ICustomAttributeProvider value)
        {
            return value.CustomAttributes.FirstOrDefault(
                a => a.AttributeType.FullName == "Fody.ConfigureAwaitAttribute");
        }

        public static GenericInstanceType MakeGenericInstanceType(this TypeReference typeReference,
            IList<TypeReference> arguments)
        {
            return TypeReferenceRocks.MakeGenericInstanceType(typeReference, arguments.ToArray());
        }
    }
}