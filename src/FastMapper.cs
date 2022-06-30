using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EnsureThat;
using LuizStudios.Attributes;
using LuizStudios.Configuration;
using LuizStudios.IL;
using LuizStudios.RuntimeClasses;

namespace LuizStudios.FastMapper
{
    /// <summary>
    /// Main class of <see cref="FastMapper"/>, it contains all public methods.
    /// </summary>
    public static class FastMapper
    {
        private const string FastMapperInRuntime = nameof(FastMapperInRuntime);

        private const int ArraysDefaultSize = 4;

        private static object[] _instances;
        private static string[] _instancesIndexes;

        // Properties to be set and used in the MapTo(...) method
        private static string _instancesTypeNameLastExecuted;
        private static int _instancesIndexLastExecuted;

        private static readonly Type _typeOfObject = typeof(object);

        private static readonly ILProvider _ilProvider;

        static FastMapper()
        {
            _ilProvider = new ILProvider($"{FastMapperInRuntime}_Assembly", $"{FastMapperInRuntime}_Module");

            /*
             * The default value is -1 because if a bug or something unexpected happens and the value of this variable remains at 0,
             * maybe it will point to an index of the _instancesIndexes list that is not the right one
             */
            _instancesIndexLastExecuted = _instancesIndexLastExecuted == 0 ? -1 : _instancesIndexLastExecuted;
        }

        public static void Bind<TSource, TTarget>() where TSource : class where TTarget : class
        {
            InternalBind(typeof(TSource), typeof(TTarget));
        }

        public static void Bind<TSource, TTarget>(Action<FastMapperConfiguration> configuration) where TSource : class where TTarget : class
        {


            InternalBind(typeof(TSource), typeof(TTarget), null);
        }

        /*public static void Bind(Type source, Type target)
        {
            InternalBind(source, target, null);
        }

        public static void Bind(Type source, Type target, Action<FastMapperConfiguration> configuration)
        {
            InternalBind(source, target, configuration);
        }*/

        private static void InternalBind(Type source, Type target, FastMapperConfiguration config = null)
        {
            Ensure.That(source).IsNotNull();
            Ensure.That(target).IsNotNull();

            _instances = _instances ?? new object[config == null ? ArraysDefaultSize : config.InstancesArraySize];
            _instancesIndexes = _instancesIndexes ?? new string[config == null ? ArraysDefaultSize : config.InstancesArraySize];

            var sourceTypeName = source.Name;
            var targetTypeName = target.Name;

            if (_instancesIndexes.Any(_instanceIndex => _instanceIndex != null && _instanceIndex.Contains(targetTypeName)))
            {
                throw new InvalidOperationException($"Only call the \"FastMapper.Bind<{sourceTypeName}, {targetTypeName}>();\" method once. " +
                                                    $"(Maybe you wanted to do it the other way around? In this case: \"FastMapper.Bind<{targetTypeName}, {sourceTypeName}>();\")");
            }

            _ilProvider.CreateClass($"{FastMapperInRuntime}_Class", typeof(RuntimeBaseClass));
            _ilProvider.CreateMethod("MakeMap", _typeOfObject, new[] { _typeOfObject }, makeMapMethodIlWriter =>
            {
                makeMapMethodIlWriter.Emit(OpCodes.Newobj, target.GetConstructor(Type.EmptyTypes));

                var propertiesFlags = BindingFlags.Instance | BindingFlags.Public;

                if (config != null && config.IgnorePropertiesCase)
                {
                    propertiesFlags |= BindingFlags.IgnoreCase;
                }

                foreach (var targetProperty in target.GetProperties(propertiesFlags))
                {
                    var srcProperty = source.GetProperty(targetProperty.Name);
                    if (srcProperty != null)
                    {
                        var ignoreProperty = srcProperty.GetCustomAttribute<FastMapperIgnoreAttribute>();
                        if (ignoreProperty != null)
                        {
                            // Set new value
                        }

                        makeMapMethodIlWriter.Emit(OpCodes.Dup);
                        makeMapMethodIlWriter.Emit(OpCodes.Ldarg_1);

                        makeMapMethodIlWriter.EmitCall(OpCodes.Call, srcProperty.GetGetMethod(), Type.EmptyTypes);
                        makeMapMethodIlWriter.EmitCall(OpCodes.Call, targetProperty.GetSetMethod(), Type.EmptyTypes);
                    }
                }

                makeMapMethodIlWriter.Emit(OpCodes.Ret);
            });

            var instancesIndex = 0;

            var instancesLength = _instances.Length;
            for (instancesIndex = 0; instancesIndex < instancesLength; instancesIndex++)
            {
                ref var instance = ref _instances[instancesIndex];
                if (instance == null)
                {
                    instance = Activator.CreateInstance(_ilProvider.CreateType());

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
        public static TTarget MapTo<TTarget>(this object source) where TTarget : class
        {
            return (TTarget)MapTo(source, Activator.CreateInstance(typeof(TTarget)));
        }

#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static TTarget MapTo<TSource, TTarget>(TSource source) where TSource : class where TTarget : class
        {
            return (TTarget)MapTo(source, Activator.CreateInstance(typeof(TTarget)));
        }

#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static object MapTo(object source, object target)
        {
            Ensure.That(source).IsNotNull();
            Ensure.That(target).IsNotNull();

            var targetTypeName = target.GetType().Name;

            RuntimeBaseClass runtimeBaseClass;

            // More faster than string.Equals
            if (string.CompareOrdinal(targetTypeName, _instancesTypeNameLastExecuted) == 0)
            {
                runtimeBaseClass = (RuntimeBaseClass)_instances[_instancesIndexLastExecuted];

                return runtimeBaseClass.MakeMap(source);
            }

            foreach (var instancesIndex in _instancesIndexes)
            {
                if (instancesIndex.Contains(targetTypeName))
                {
                    // We take the last character because index is always the last character
                    _instancesIndexLastExecuted = instancesIndex[instancesIndex.Length - 1] - '0'; // Convert char to int without memory allocation
                    _instancesTypeNameLastExecuted = targetTypeName;

                    runtimeBaseClass = (RuntimeBaseClass)_instances[_instancesIndexLastExecuted];

                    return runtimeBaseClass.MakeMap(source);
                }
            }

            var sourceTypeName = source.GetType().Name;

            throw new InvalidOperationException($"The mapping between object \"{sourceTypeName}\" and \"{targetTypeName}\" has not been defined." +
                                                $"Use the \"FastMapper.Bind<{sourceTypeName}, {targetTypeName}>();\" method before using this method.");
        }

#if DEBUG
        public static Assembly GetAssemblyOfCreatedType()
        {
            return _ilProvider.GetAssemblyOfCreatedType();
        }
#endif
    }
}
