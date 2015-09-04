using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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

        private TypeFinder typeFinder;

        public void Execute()
        {
            LoggerFactory.LogInfo = LogInfo;
            LoggerFactory.LogWarn = LogWarning;
            LoggerFactory.LogError = LogError;

            typeFinder = new TypeFinder(AssemblyResolver, ModuleDefinition);

            var configureAwaitValue = (bool?)ModuleDefinition.Assembly.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;

            var types = ModuleDefinition.GetTypes()
                .ToList();

            foreach (var type in types)
            {
                ProcessType(configureAwaitValue, type);
            }

            RemoveAttributes(types);
            RemoveReference();
        }

        private void ProcessType(bool? assemblyConfigureAwaitValue, TypeDefinition type)
        {
            if (type.IsCompilerGenerated() && type.IsIAsyncStateMachine())
            {
                return;
            }

            var configureAwaitValue = (bool?)type.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;
            configureAwaitValue = configureAwaitValue ?? assemblyConfigureAwaitValue;

            foreach (var method in type.Methods)
            {
                var localConfigureAwaitValue = (bool?)method.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;
                var localConfigWasSet = localConfigureAwaitValue.HasValue;
                localConfigureAwaitValue = localConfigureAwaitValue ?? configureAwaitValue;
                if (localConfigureAwaitValue == null)
                    continue;

                var asyncStateMachineType = method.GetAsyncStateMachineType();
                if (asyncStateMachineType != null)
                {
                    AddAwaitConfigToAsyncMethod(asyncStateMachineType, localConfigureAwaitValue.Value);
                }
                else if (localConfigWasSet)
                {
                    LogWarning($"ConfigureAwaitAttribue applied to non-async method '{method.FullName}'.");
                    continue;
                }
            }
        }

        class ImportContext
        {
            private readonly TypeDefinition _stateMachineType;
            private readonly ModuleDefinition _module;

            private readonly TypeDefinition _configuredTaskAwaitableTypeDef;
            private readonly TypeDefinition _configuredTaskAwaiterTypeDef;
            private readonly TypeDefinition _gConfiguredTaskAwaitableTypeDef;
            private readonly TypeDefinition _gConfiguredTaskAwaiterTypeDef;
            private readonly TypeDefinition _gTask;
            private readonly TypeDefinition _task;

            private readonly TypeReference _configuredTaskAwaitableTypeRef;
            private readonly TypeReference _configuredTaskAwaiterTypeRef;

            private readonly MethodDefinition _gConfigureAwaitMethodDef;
            private readonly MethodDefinition _configureAwaitMethodDef;
            private readonly MethodDefinition _asyncStateMachineMoveNextMethod;


            public TypeDefinition StateMachineType
            {
                get
                {
                    return _stateMachineType;
                }
            }

            public MethodDefinition MoveNextMethod
            {
                get
                {
                    return _asyncStateMachineMoveNextMethod;
                }
            }

            

            /// <summary>
            /// Represent a context for a type match.
            /// </summary>
            public class BoundedMatchContext
            {
                public Func<TypeDefinition> MatchedTypeDef;
                public Func<TypeReference, TypeReference> ChangeMethodDeclaringType;

                public Func<TypeReference> ConfiguredTaskAwaiterTypeRef;
                public Func<TypeReference> ConfiguredTaskAwaitableTypeRef;
                public Func<MethodReference> ConfigureAwaitMethodRef;
                public Func<TypeReference, TypeReference> ChangeConfigureAwaitMethodDeclaringType;
            }


            public ImportContext(TypeFinder typeFinder, ModuleDefinition module, TypeDefinition type)
            {
                _module = module;
                _stateMachineType = type;

                _configuredTaskAwaitableTypeDef = typeFinder
                    .GetMSCorLibTypeDefinition<System.Runtime.CompilerServices.ConfiguredTaskAwaitable>();

                _configuredTaskAwaiterTypeDef = _configuredTaskAwaitableTypeDef.NestedTypes[0];

                _configuredTaskAwaitableTypeRef = ImportReference(_configuredTaskAwaitableTypeDef);
                _configuredTaskAwaiterTypeRef = ImportReference(_configuredTaskAwaiterTypeDef);

                _task = typeFinder
                    .GetMSCorLibTypeDefinition<System.Threading.Tasks.Task>();

                _configureAwaitMethodDef = _task
                    .Methods
                    .First(m => m.Name == nameof(System.Threading.Tasks.Task<bool>.ConfigureAwait));


                _gConfiguredTaskAwaitableTypeDef = typeFinder
                    .GetMSCorLibTypeDefinition(typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<>));

                _gConfiguredTaskAwaiterTypeDef = _gConfiguredTaskAwaitableTypeDef.NestedTypes[0];

                _gTask = typeFinder
                    .GetMSCorLibTypeDefinition(typeof(System.Threading.Tasks.Task<>));

                _gConfigureAwaitMethodDef = _gTask
                    .Methods
                    .First(m => m.Name == nameof(System.Threading.Tasks.Task.ConfigureAwait));


                // Modify MoveNext method
                _asyncStateMachineMoveNextMethod = type
                    .Methods
                    .First(m => m.Name == nameof(System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext));
                
            }

            //public Mono.Collections.Generic.Collection<VariableDefinition> Variables
            //{
            //    get { return _asyncStateMachineMoveNextMethod.Body.Variables; }
            //}

            //public Mono.Collections.Generic.Collection<Instruction> Instructions
            //{
            //    get { return _asyncStateMachineMoveNextMethod.Body.Instructions; }
            //}



            public MethodReference ImportReference(System.Reflection.MethodBase method)
            {
                return _module.ImportReference(method, StateMachineType);
            }
            public TypeReference ImportReference(TypeReference type)
            {
                return _module.ImportReference(type, StateMachineType);
            }
            public FieldReference ImportReference(FieldReference field)
            {
                return _module.ImportReference(field, StateMachineType);
            }
            public MethodReference ImportReference(MethodReference method)
            {
                return _module.ImportReference(method, StateMachineType);
            }
            public FieldReference ImportReference(System.Reflection.FieldInfo field)
            {
                return _module.ImportReference(field, StateMachineType);
            }
            public TypeReference ImportReference(Type type)
            {
                return _module.ImportReference(type, StateMachineType);
            }

            /// <summary>
            /// Bind a match context for a non generic type (<see cref="System.Runtime.CompilerServices.TaskAwaiter"/> or
            /// <see cref="System.Threading.Tasks.Task"/>)
            /// </summary>
            /// <param name="replacementTypeDef">
            /// The type that should be used as a replacement.
            /// This map to <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter"/> if the matched type is 
            /// <see cref="System.Runtime.CompilerServices.TaskAwaiter"/> or to 
            /// <see cref = "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" /> if the matched type is 
            /// <see cref="System.Threading.Tasks.Task"/>.
            /// </param>
            /// <returns></returns>
            private BoundedMatchContext BindMatchContext(TypeDefinition replacementTypeDef)
            {
                return new BoundedMatchContext()
                {
                    MatchedTypeDef = () => replacementTypeDef,
                    ChangeMethodDeclaringType = currentDeclaringTypeRef => currentDeclaringTypeRef,

                    ConfiguredTaskAwaiterTypeRef = () => _configuredTaskAwaiterTypeRef,
                    ConfiguredTaskAwaitableTypeRef = () => _configuredTaskAwaitableTypeRef,
                    ConfigureAwaitMethodRef = () => ImportReference(_configureAwaitMethodDef),
                    ChangeConfigureAwaitMethodDeclaringType = currentDeclaringTypeRef => currentDeclaringTypeRef
                };
            }


            /// <summary>
            /// Bind a match context for a non generic type (<see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/> or
            /// <see cref="System.Threading.Tasks.Task{T}"/>)
            /// </summary>
            /// <param name="replacementTypeDef">
            /// The type that should be used as a replacement.
            /// This map to <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable{T}.ConfiguredTaskAwaiter"/> if the matched type is 
            /// <see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/> or to
            /// <see cref = "System.Runtime.CompilerServices.ConfiguredTaskAwaitable{T}" /> if the matched type is 
            /// <see cref="System.Threading.Tasks.Task{T}"/>.
            /// </param>
            /// <returns></returns>
            private BoundedMatchContext BindMatchContext(TypeDefinition replacementTypeDef, GenericInstanceType genericType)
            {
                return new BoundedMatchContext()
                {
                    MatchedTypeDef = () => replacementTypeDef,

                    ChangeMethodDeclaringType = currentDeclaringTypeRef => ImportReference(
                        replacementTypeDef.MakeGenericInstanceType(genericType.GenericArguments)
                    ),

                    ConfiguredTaskAwaiterTypeRef = () => ImportReference(
                        _gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericType.GenericArguments)
                    ),
                    ConfiguredTaskAwaitableTypeRef = () => ImportReference(
                        _gConfiguredTaskAwaitableTypeDef.MakeGenericInstanceType(genericType.GenericArguments)
                    ),
                    ConfigureAwaitMethodRef = () => ImportReference(_gConfigureAwaitMethodDef),

                    ChangeConfigureAwaitMethodDeclaringType = currentTypeRef => ImportReference(
                        _gTask.MakeGenericInstanceType(genericType.GenericArguments)
                    )
                };
            }

            /// <summary>
            /// Invoke the action provided in <paramref name="apply"/> if <paramref name="type"/> match a 
            /// <see cref="System.Runtime.CompilerServices.TaskAwaiter"/> or a 
            /// <see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/>.
            /// The the lambda parameter will be bound to the appropriate implementation for the match 
            /// (Generic or non generic).
            /// If <paramref name="matchOnTask"/> is true then <paramref name="apply"/> is called also if
            /// the <paramref name="type"/> match a <see cref="System.Threading.Tasks.Task"/> or a 
            /// <see cref="System.Threading.Tasks.Task{T}"/> with the appropriate function bindings.
            /// </summary>
            /// <param name="type">The type that is tested for a match</param>
            /// <param name="apply">The application lambda that is called on a match</param>
            /// <param name="matchOnTask">If true indicate that the <paramref name="type"/> 
            /// is to be tested for a match against <see cref="System.Threading.Tasks.Task"/> or 
            /// <see cref="System.Threading.Tasks.Task{T}"/></param>
            public void Match(
                TypeReference type,
                Action<BoundedMatchContext> apply,
                bool matchOnTask = false
            )
            {
                var _this = this;
                if (!type.IsGenericInstance)
                {
                    if (type.IsType<System.Runtime.CompilerServices.TaskAwaiter>())
                    {
                        apply(BindMatchContext(_configuredTaskAwaiterTypeDef));
                    }
                    else if (matchOnTask && type.IsType<System.Threading.Tasks.Task>())
                    {
                        apply(BindMatchContext(_configuredTaskAwaitableTypeDef));
                    }
                }
                else
                {
                    var genericType = (GenericInstanceType)type;
                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    var openGenericTypeDefinition = type.Resolve();
                    if (openGenericTypeDefinition?.IsType(typeof(System.Runtime.CompilerServices.TaskAwaiter<>)) == true)
                    {
                        apply(BindMatchContext(_gConfiguredTaskAwaiterTypeDef, genericType));
                    }
                    else if (matchOnTask && openGenericTypeDefinition?.IsType(typeof(System.Threading.Tasks.Task<>)) == true)
                    {
                        apply(BindMatchContext(_gConfiguredTaskAwaitableTypeDef, genericType));
                    }
                }
            }
        }


        private void AddAwaitConfigToAsyncMethod(TypeDefinition stateMachineType, bool configureAwaitValue)
        {
            var localModuleContext = new ImportContext(
                typeFinder, 
                ModuleDefinition, 
                stateMachineType
            );

            // Get the MoveNextMethod body
            var body = localModuleContext.MoveNextMethod.Body;

            FixupFieldsTypes(localModuleContext);

            body.SimplifyMacros();

            InsertConfigureAwaitBeforeGetAwaiterCalls(localModuleContext, configureAwaitValue);

            FixupOperandsTypes(localModuleContext);

            body.OptimizeMacros();
        }

        /// <summary>
        /// Change field type from <see cref="System.Runtime.CompilerServices.TaskAwaiter"/> to 
        /// <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter"/>
        /// </summary>
        /// <param name="localModuleContext"></param>
        private static void FixupFieldsTypes(ImportContext localModuleContext)
        {
            foreach (var field in localModuleContext.StateMachineType.Fields)
            {
                localModuleContext.Match(
                    field.FieldType,
                    context => field.FieldType = context.ConfiguredTaskAwaiterTypeRef()
                );
            }
        }

        /// <summary>
        /// Analyze and change occurrences of <see cref="MethodReference"/>, 
        /// <see cref="FieldReference"/> and <see cref="TypeReference"/> if they refers to a 
        /// <see cref="System.Runtime.CompilerServices.TaskAwaiter"/> or 
        /// <see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/> to
        /// use <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter"/> 
        /// or <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable{T}.ConfiguredTaskAwaiter"/>
        /// </summary>
        /// <param name="localModuleContext"></param>
        private static void FixupOperandsTypes(ImportContext localModuleContext)
        {
            var instructions = localModuleContext.MoveNextMethod.Body.Instructions;

            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                instruction.
                    OnOperandType<MethodReference>(methodRef =>
                    {
                        if (FixupAwaitUnsafeOnCompleteGenericArgumentType(localModuleContext, methodRef))
                            return;

                        localModuleContext.Match(
                            methodRef.DeclaringType,
                            context =>
                            {
                                var newOperand = context
                                    .MatchedTypeDef()
                                    .Methods
                                    .FirstOrDefault(m => m.Name == methodRef.Name);

                                if (newOperand != null)
                                {
                                    var newOperandRef = localModuleContext.ImportReference(newOperand);
                                    newOperandRef.DeclaringType = context.ChangeMethodDeclaringType(newOperandRef.DeclaringType);
                                    instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                                }
                            },
                            matchOnTask: true
                        );
                        
                    })
                    ?.OnOperandType<FieldReference>(fieldReference =>
                    {
                        localModuleContext.Match(
                            fieldReference.FieldType,
                            context => fieldReference.FieldType = context.ConfiguredTaskAwaiterTypeRef()
                        );
                    })
                    ?.OnOperandType<TypeReference>(typeReference =>
                    {
                        localModuleContext.Match(
                            typeReference,
                            context => instruction.Operand = context.ConfiguredTaskAwaiterTypeRef()
                        );
                    });
            }
        }

        /// <summary>
        /// Change the TAwaiter generic argument type of 
        /// <see cref="System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted{TAwaiter, T}"/> from
        /// <see cref="System.Runtime.CompilerServices.TaskAwaiter"/> to 
        /// <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter"/> 
        /// or from <see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/> to 
        /// <see cref="System.Runtime.CompilerServices.ConfiguredTaskAwaitable{T}.ConfiguredTaskAwaiter"/>
        /// </summary>
        /// <param name="localModuleContext"></param>
        /// <param name="methodRef"></param>
        /// <returns></returns>
        private static bool FixupAwaitUnsafeOnCompleteGenericArgumentType(ImportContext localModuleContext, MethodReference methodRef)
        {
            // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
            // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
            if (methodRef.IsGenericInstance &&
                methodRef.Name == nameof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted))
            {
                var awaitUnsafeOnCompleted = (GenericInstanceMethod)methodRef;

                for (int j = 0; j < awaitUnsafeOnCompleted.GenericArguments.Count; j++)
                {
                    localModuleContext.Match(
                        awaitUnsafeOnCompleted.GenericArguments[j],
                        context => awaitUnsafeOnCompleted.GenericArguments[j] = context.ConfiguredTaskAwaiterTypeRef()
                    );
                }
                return true;
            }
            return false;
        }

        private static void InsertConfigureAwaitBeforeGetAwaiterCalls(ImportContext localModuleContext, bool configureAwaitValue)
        {
            var body = localModuleContext.MoveNextMethod.Body;
            var variables = body.Variables;
            var ilProcessor = body.GetILProcessor();

            var awaitAwaiterPair = new Dictionary<VariableDefinition, VariableDefinition>();
            var configureAwaitMethods = new Dictionary<VariableDefinition, MethodReference>();

            var i = 0;
            foreach (var variable in variables.ToArray())
            {
                localModuleContext.Match(
                    variable.VariableType,
                    (context) =>
                    {
                        variable.VariableType = context.ConfiguredTaskAwaiterTypeRef();
                        var awaitableVar = new VariableDefinition(context.ConfiguredTaskAwaitableTypeRef());
                        var configureAwaitMethod = context.ConfigureAwaitMethodRef();
                        configureAwaitMethod.DeclaringType = context.ChangeConfigureAwaitMethodDeclaringType(configureAwaitMethod.DeclaringType);

                        variables.Insert(i + 1, awaitableVar);
                        awaitAwaiterPair.Add(variable, awaitableVar);
                        configureAwaitMethods.Add(variable, configureAwaitMethod);
                    });

                i++;
            }

            // Insert ConfigureAwait call just before GetAwaiter call.
            foreach (var instruction in body.Instructions.Where(GetAwaiterSearch).ToList())
            {
                var variable = (VariableDefinition)instruction.Next.Operand;
                var awaitableVar = awaitAwaiterPair[variable];
                var configureAwait = configureAwaitMethods[variable];

                ilProcessor.InsertBefore(instruction,
                    Instruction.Create(configureAwaitValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), // true or false
                    Instruction.Create(OpCodes.Callvirt, configureAwait), // Call ConfigureAwait
                    Instruction.Create(OpCodes.Stloc, awaitableVar), // Store in variable
                    Instruction.Create(OpCodes.Ldloca, awaitableVar) // Load variable
                );
            }
        }

        private static bool GetAwaiterSearch(Instruction instruction)
        {
            var method = instruction.Operand as MethodReference;

            if (method != null)
                return (method.DeclaringType.IsType<System.Threading.Tasks.Task>() ||
                        method.DeclaringType.Resolve().IsType(typeof(System.Threading.Tasks.Task<>))) &&
                        method.Name == nameof(System.Threading.Tasks.Task.GetAwaiter);

            return false;
        }

        private void RemoveAttributes(List<TypeDefinition> types)
        {
            ModuleDefinition.Assembly.RemoveAllCustomAttributes();
            ModuleDefinition.RemoveAllCustomAttributes();
            foreach (var typeDefinition in types)
            {
                typeDefinition.RemoveAllCustomAttributes();

                foreach (var method in typeDefinition.Methods)
                {
                    method.RemoveAllCustomAttributes();
                }

                foreach (var property in typeDefinition.Properties)
                {
                    property.RemoveAllCustomAttributes();
                }
            }
        }

        private void RemoveReference()
        {
            var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "ConfigureAwait");
            if (referenceToRemove == null)
            {
                LogInfo("\tNo reference to 'ConfigureAwait.dll' found. References not modified.");
                return;
            }

            ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
            LogInfo("\tRemoving reference to 'ConfigureAwait.dll'.");
        }
    }

    

}