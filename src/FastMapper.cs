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

        private static Dictionary<string, object> _mappers;


        public abstract class TestMapper
        {
            public abstract object MakeMap(object source);
        }

        private const string FastMapperLibraryName = "FastMapper";

        private static string _fastMapperAssemblyName = $"{FastMapperLibraryName}_Assembly";
        private static string _fastMapperClassName = $"{FastMapperLibraryName}_Class";


        /*private static Func<object, object>[] _conversionMethods;
        private static string[] _conversionMethodIndexes;*/

        /*private static string _toLastExecuted;
        private static int _conversionMethodIndexLastExecuted = -1;*/

        private const int ArrayDefaultCapacity = 4;

        static FastMapper()
        {
            _mappers = new Dictionary<string, object>();

            /*_conversionMethods = _conversionMethods ?? new Func<object, object>[ArrayDefaultCapacity];
            _conversionMethodIndexes = _conversionMethodIndexes ?? new string[ArrayDefaultCapacity];*/
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

            _mappers.Add(destTypeName, Activator.CreateInstance(newType));


            /*var conversionMethodIndex = 0;

            var conversionMethodsLength = _conversionMethods.Length;
            for (conversionMethodIndex = 0; conversionMethodIndex < conversionMethodsLength; conversionMethodIndex++)
            {
                ref var _conversionMethod = ref _conversionMethods[conversionMethodIndex];
                if (_conversionMethod == null)
                {
                    _conversionMethod = (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>), ilProvider.GetCreatedMethod());

                    break;
                }

                // Last element of the array
                if ((conversionMethodIndex + 1) == conversionMethodsLength)
                {
                    //FastMapperExtensions.Array.IncreaseCapacity(ref _conversionMethods, conversionMethodsLength * 2);

                    // Restart for = -1 -> 0
                    conversionMethodIndex = -1;
                }
            }

            var conversionMethodIndexesLength = _conversionMethodIndexes.Length;
            for (var i = 0; i < conversionMethodIndexesLength; i++)
            {
                ref var toAndMethod = ref _conversionMethodIndexes[i];
                if (toAndMethod == null)
                {
                    toAndMethod = $"{to}_{conversionMethodIndex}";

                    break;
                }

                // Last element of the array
                if ((i + 1) == conversionMethodIndexesLength)
                {
                    //FastMapperExtensions.Array.IncreaseCapacity(ref _conversionMethodIndexes, conversionMethodIndexesLength * 2);

                    // Restart for = -1 -> 0
                    i = -1;
                }
            }*/
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

            var destTypeName = typeof(TDestination).Name;

            if (_mappers.TryGetValue(destTypeName, out object type))
            {
                var mapper = (TestMapper)type;
                return (TDestination)mapper.MakeMap(src);
            }

            return default;
            /*var srcTypeName = src.GetType().Name;
            var destTypeName = typeof(TDestination).Name;

            var toAndMethod = $"{srcTypeName}To{destTypeName}_Method";

            return (TDestination)_conversionMethods[0](src);*/


            /*if (string.CompareOrdinal(toAndMethod, _toLastExecuted) == 0)
            {
                return (TDestination)_conversionMethods[_conversionMethodIndexLastExecuted](src);
            }

            foreach (var typeNameAndIndexMethod in _conversionMethodIndexes)
            {
                if (typeNameAndIndexMethod.Contains(toAndMethod))
                {
                    foreach (var typeNameAndIndexMethodChar in typeNameAndIndexMethod)
                    {
                        if (char.IsDigit(typeNameAndIndexMethodChar))
                        {
                            _conversionMethodIndexLastExecuted = typeNameAndIndexMethodChar - '0';
                            _toLastExecuted = toAndMethod;

                            return (TDestination)_conversionMethods[_conversionMethodIndexLastExecuted](src);
                        }
                    }
               */

            //throw new InvalidOperationException($"The mapping between object \"{srcTypeName}\" and \"{destTypeName}\" has not been defined. Use the \"FastMapper.Bind<{srcTypeName}, {destTypeName}>();\" method before using this method.");
        }
    }
}
