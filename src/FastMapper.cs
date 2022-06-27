using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Lokad.ILPack;

namespace LuizStudios.FastMapper
{

    /// <summary>
    /// Main class of <see cref="FastMapper"/>, it contains all public methods.
    /// </summary>
    public static class FastMapper
    {




        public abstract class TestMapper
        {
            public abstract object MakeMap(object source);
        }

        private const string FastMapperLibraryName = "FastMapper";

        private static string _fastMapperAssemblyName = $"{FastMapperLibraryName}_Assembly";
        private static string _fastMapperClassName = $"{FastMapperLibraryName}_Class";


        private static object[] _instances;
        private static string[] _instancesIndexes;

        private static string _objectInstancesLastExecuted;
        private static int _objectInstancesIndexLastExecuted = -1;

        private const int ArrayDefaultCapacity = 4;

        static FastMapper()
        {

            _instances = _instances ?? new object[ArrayDefaultCapacity];
            _instancesIndexes = _instancesIndexes ?? new string[ArrayDefaultCapacity];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Bind<TSource, TDestination>() where TSource : class where TDestination : class
        {
            var srcType = typeof(TSource);
            var destType = typeof(TDestination);

            var srcTypeName = srcType.Name;
            var destTypeName = destType.Name;

            var to = $"MakeMap";

            /*if (_conversionMethods.Any(_conversionMethod => _conversionMethod != null && string.Equals(_conversionMethod.Method.Name, to, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Only call the \"FastMapper.Bind<{srcTypeName}, {destTypeName}>();\" method once. " +
                                                    $"(Maybe you wanted to do it the other way around? In this case: \"FastMapper.Bind<{destTypeName}, {srcTypeName}>();\")");
            }*/

            var ilProvider = new ILProvider(FastMapperLibraryName, $"{FastMapperLibraryName}_Module");
            ilProvider.CreateClass("TestMapperToTarget", typeof(TestMapper));
            ilProvider.CreateMethod(to, typeof(object), new[] { typeof(object) }, mapMethodIlWriter =>
            {
                mapMethodIlWriter.Emit(OpCodes.Newobj, destType.GetConstructor(Type.EmptyTypes));

                foreach (var destProperty in destType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var srcProperty = srcType.GetProperty(destProperty.Name);
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

            var newType = ilProvider.GetCreatedType();

            var asmGen = new AssemblyGenerator();
            asmGen.GenerateAssembly(newType.Assembly, @"C:\Users\luizf\Desktop\assembly2.dll");



            var instanceIndex = 0;

            var instanceLength = _instances.Length;
            for (instanceIndex = 0; instanceIndex < instanceLength; instanceIndex++)
            {
                ref var _conversionMethod = ref _instances[instanceIndex];
                if (_conversionMethod == null)
                {
                    _conversionMethod = Activator.CreateInstance(newType);

                    break;
                }

                // Last element of the array
                if ((instanceIndex + 1) == instanceLength)
                {
                    //FastMapperExtensions.Array.IncreaseCapacity(ref _conversionMethods, conversionMethodsLength * 2);

                    // Restart for = -1 -> 0
                    instanceIndex = -1;
                }
            }

            var instanceIndexesLength = _instancesIndexes.Length;
            for (var i = 0; i < instanceIndexesLength; i++)
            {
                ref var toAndMethod = ref _instancesIndexes[i];
                if (toAndMethod == null)
                {
                    toAndMethod = $"{destTypeName}_{instanceIndex}";

                    break;
                }

                // Last element of the array
                if ((i + 1) == instanceIndexesLength)
                {
                    //FastMapperExtensions.Array.IncreaseCapacity(ref _conversionMethodIndexes, conversionMethodIndexesLength * 2);

                    // Restart for = -1 -> 0
                    i = -1;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static TDestination MapTo<TDestination>(this object src) where TDestination : class
        {

            /*var destTypeName = typeof(TDestination).Name;

            if (_mappers.TryGetValue(destTypeName, out object type))
            {
                var mapper = (TestMapper)type;
                return (TDestination)mapper.MakeMap(src);
            }
*/
            var destTypeName = typeof(TDestination).Name;

            if (string.CompareOrdinal(destTypeName, _objectInstancesLastExecuted) == 0)
            {
                var mapper = (TestMapper)_instances[_objectInstancesIndexLastExecuted];

                return (TDestination)mapper.MakeMap(src);
            }

            foreach (var item in _instancesIndexes)
            {

                if (item.Contains(destTypeName))
                {


                    foreach (var typeNameAndIndexMethod in item)
                    {
                        if (char.IsDigit(typeNameAndIndexMethod))
                        {
                            _objectInstancesIndexLastExecuted = typeNameAndIndexMethod - '0';
                            _objectInstancesLastExecuted = destTypeName;

                            var mapper = (TestMapper)_instances[_objectInstancesIndexLastExecuted];

                            return (TDestination)mapper.MakeMap(src);
                        }


                    }
                }
            }

            throw new InvalidOperationException();// $"The mapping between object \"{srcTypeName}\" and \"{destTypeName}\" has not been defined. Use the \"FastMapper.Bind<{srcTypeName}, {destTypeName}>();\" method before using this method.");
        }
    }
}
