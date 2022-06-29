using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EnsureThat;
using LuizStudios.RuntimeClasses;

namespace LuizStudios.FastMapper
{
    /// <summary>
    /// Main class of <see cref="FastMapper"/>, it contains all public methods.
    /// </summary>
    public static class FastMapper
    {
        private const string FastMapperInRuntime = nameof(FastMapperInRuntime);

        /*private readonly static string _fastMapperRuntimeAssemblyName;
        private readonly static string _fastMapperRuntimeClassName;*/

        private static object[] _instances;
        private static string[] _instancesIndexes;

        private static string _instancesIndexesLastExecuted;
        private static int _instancesIndexLastExecuted;

        static FastMapper()
        {
            _instances = _instances ?? new object[4];
            _instancesIndexes = _instancesIndexes ?? new string[4];

            /*_fastMapperRuntimeAssemblyName = _fastMapperRuntimeAssemblyName ?? $"{FastMapperLibraryName}_Assembly";
            _fastMapperRuntimeClassName = _fastMapperRuntimeClassName ?? $"{FastMapperLibraryName}_Class";*/

            /*
             * The default value is -1 because if a bug or something unexpected happens and the value of this variable remains at 0,
             * maybe it will point to an index of the _instancesIndexes list that is not the right one
             */
            _instancesIndexLastExecuted = _instancesIndexLastExecuted == 0 ? -1 : _instancesIndexLastExecuted;
        }

        public static void Bind<TSource, TTarget>() where TSource : class where TTarget : class
        {
            Bind(typeof(TSource), typeof(TTarget));
        }

        public static void Bind(Type source, Type target)
        {
            Ensure.That(source).IsNotNull();
            Ensure.That(target).IsNotNull();

            var sourceTypeName = source.Name;
            var targetTypeName = target.Name;

            if (_instancesIndexes.Any(_instanceIndex => _instanceIndex != null && _instanceIndex.Contains(targetTypeName)))
            {
                throw new InvalidOperationException($"Only call the \"FastMapper.Bind<{sourceTypeName}, {targetTypeName}>();\" method once. " +
                                                    $"(Maybe you wanted to do it the other way around? In this case: \"FastMapper.Bind<{targetTypeName}, {sourceTypeName}>();\")");
            }

            var runtimeTypeBaseClass = typeof(RuntimeBaseClass<>);

            var ilProvider = new ILProvider($"{FastMapperInRuntime}_Assembly", $"{FastMapperInRuntime}_Module");
            ilProvider.CreateClass($"{FastMapperInRuntime}_Class", runtimeTypeBaseClass.MakeGenericType(target));
            ilProvider.CreateMethod(runtimeTypeBaseClass.GetTypeInfo().DeclaredMethods.Single().Name, target, new[] { typeof(object) }, mapMethodIlWriter =>
            {
                mapMethodIlWriter.Emit(OpCodes.Newobj, target.GetConstructor(Type.EmptyTypes));

                foreach (var destProperty in target.GetProperties()) // BindingFlags.Instance | BindingFlags.Public
                {
                    var srcProperty = source.GetProperty(destProperty.Name);
                    if (srcProperty != null)
                    {
                        mapMethodIlWriter.Emit(OpCodes.Dup);
                        mapMethodIlWriter.Emit(OpCodes.Ldarg_0);

                        mapMethodIlWriter.EmitCall(OpCodes.Call, srcProperty.GetGetMethod(), Type.EmptyTypes);
                        mapMethodIlWriter.EmitCall(OpCodes.Call, destProperty.GetSetMethod(), Type.EmptyTypes);
                    }
                }

                mapMethodIlWriter.Emit(OpCodes.Ret);
            });

            /*var asmGen = new AssemblyGenerator();
            asmGen.GenerateAssembly(newType.Assembly, @"C:\Users\luizf\Desktop\assembly2.dll");*/

            var instancesIndex = 0;

            var instancesLength = _instances.Length;
            for (instancesIndex = 0; instancesIndex < instancesLength; instancesIndex++)
            {
                ref var instance = ref _instances[instancesIndex];
                if (instance == null)
                {
                    instance = Activator.CreateInstance(ilProvider.CreateType());

                    break;
                }

                // Last element of the array
                if ((instancesIndex + 1) == instancesIndex)
                {
                    //Array.Resize(ref _instances, );

                    // Restart for = -1 -> 0
                    instancesIndex = -1;
                }
            }

            var instanceIndexesLength = _instancesIndexes.Length;
            for (var instancesIndexesIndex = 0; instancesIndexesIndex < instanceIndexesLength; instancesIndexesIndex++)
            {
                ref var instanceIndex = ref _instancesIndexes[instancesIndexesIndex];
                if (instanceIndex == null)
                {
                    instanceIndex = $"{targetTypeName}{instancesIndex}";

                    break;
                }

                // Last element of the array
                if ((instancesIndexesIndex + 1) == instanceIndexesLength)
                {
                    //Array.Resize(ref _instances, );

                    // Restart for = -1 -> 0
                    instancesIndexesIndex = -1;
                }
            }
        }

#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static TTarget MapTo<TTarget>(this object src) where TTarget : class
        {
            var targetTypeName = typeof(TTarget).Name;

            RuntimeBaseClass<TTarget> runtimeBaseClass;

            // More faster than string.Equals
            if (string.CompareOrdinal(targetTypeName, _instancesIndexesLastExecuted) == 0)
            {
                runtimeBaseClass = (RuntimeBaseClass<TTarget>)_instances[_instancesIndexLastExecuted];

                return runtimeBaseClass.MakeMap(src);
            }

            foreach (var instancesIndex in _instancesIndexes)
            {
                if (instancesIndex.Contains(targetTypeName))
                {
                    // We take the last character because index is always the last character
                    _instancesIndexLastExecuted = instancesIndex[instancesIndex.Length - 1] - '0'; // Convert char to int without memory allocation
                    _instancesIndexesLastExecuted = targetTypeName;

                    runtimeBaseClass = (RuntimeBaseClass<TTarget>)_instances[_instancesIndexLastExecuted];

                    return runtimeBaseClass.MakeMap(src);
                }
            }

            var sourceTypeName = src.GetType().Name;

            throw new InvalidOperationException($"The mapping between object \"{sourceTypeName}\" and \"{targetTypeName}\" has not been defined. Use the \"FastMapper.Bind<{sourceTypeName}, {targetTypeName}>();\" method before using this method.");
        }
    }
}
