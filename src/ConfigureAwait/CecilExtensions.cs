using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConfigureAwait
{
    internal static class CecilExtensions
    {
        public static bool IsAsyncStateMachine(this ICustomAttributeProvider provider)
        {
            if (provider == null || !provider.HasCustomAttributes)
                return false;

            return provider.CustomAttributes
                .Any(a => a.AttributeType.FullName == typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute).FullName);
        }

        public static bool IsIAsyncStateMachine(this TypeDefinition typeDefinition)
        {
            if (typeDefinition == null || !typeDefinition.HasInterfaces)
                return false;

            return typeDefinition.Interfaces
                .Any(x => x.FullName == typeof(System.Runtime.CompilerServices.IAsyncStateMachine).FullName);
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
        {
            if (provider == null || !provider.HasCustomAttributes)
                return false;

            return provider.CustomAttributes
                .Any(a => a.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName);
        }

        public static void InsertBefore(this ILProcessor processor, Instruction target, params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
                processor.InsertBefore(target, instruction);
        }

        public static void InsertAfter(this ILProcessor processor, Instruction target, params Instruction[] instructions)
        {
            foreach (var instruction in instructions.Reverse())
                processor.InsertAfter(target, instruction);
        }

        public static void HideLineFromDebugger(this Instruction i, SequencePoint seqPoint)
        {
            if (seqPoint == null)
                return;

            HideLineFromDebugger(i, seqPoint.Document);
        }

        public static void HideLineFromDebugger(this Instruction i, Document doc)
        {
            if (doc == null)
                return;

            // This tells the debugger to ignore and step through
            // all the following instructions to the next instruction
            // with a valid SequencePoint. That way IL can be hidden from
            // the Debugger. See
            // http://blogs.msdn.com/b/abhinaba/archive/2005/10/10/479016.aspx
            i.SequencePoint = new SequencePoint(doc);
            i.SequencePoint.StartLine = 0xfeefee;
            i.SequencePoint.EndLine = 0xfeefee;
        }

        public static TypeDefinition GetAsyncStateMachineType(this ICustomAttributeProvider provider)
        {
            if (provider == null || !provider.HasCustomAttributes)
                return null;

            return (TypeDefinition)provider.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName == typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute).FullName)?.ConstructorArguments[0].Value;
        }

        public static Lazy<VariableDefinition> CreateVariable(this MethodBody body, TypeReference variableType)
        {
            return new Lazy<VariableDefinition>(() =>
            {
                var variable = new VariableDefinition(variableType);
                body.Variables.Add(variable);
                return variable;
            });
        }

        public static CustomAttribute GetConfigureAwaitAttribute(this ICustomAttributeProvider value)
        {
            return value.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Fody.ConfigureAwaitAttribute");
        }

        public static void RemoveAllCustomAttributes(this ICustomAttributeProvider definition)
        {
            var customAttributes = definition.CustomAttributes;

            var attributes = customAttributes.Where(x => x.AttributeType.Namespace == "Fody").ToArray();

            foreach (var attribute in attributes)
            {
                customAttributes.Remove(attribute);
            }
        }

        public static GenericInstanceType MakeGenericInstanceType(this TypeReference self, IEnumerable<TypeReference> arguments)
        {
            GenericInstanceType instance = new GenericInstanceType(self);
            foreach (var arg in arguments)
            {
                instance.GenericArguments.Add(arg);
            }
            return instance;
        }
        public static bool IsType(this TypeReference source, Type type)
        {
            return source.FullName == type.FullName;
        }
        public static bool IsType<T>(this TypeReference source)
        {
            return source.IsType(typeof(T));
        }

        public static Instruction OnOperandType<T>(this Instruction instruction, Action<T> action)
            where T : class
        {
            var operand = instruction.Operand as T;
            if(operand != null)
            {
                action(operand);
                return null;
            }
            return instruction;
        }
    }
}