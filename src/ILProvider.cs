using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LuizStudios.FastMapper
{
    // Provides some methods from Reflection.Emit library.
    internal sealed class ILProvider
    {
        private Type _typeCreated;

        // Builders
        private TypeBuilder _typeBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        private MethodInfo _methodInfo;

        public ILProvider(string assemblyName, string moduleName)
        {
            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect)
                                            .DefineDynamicModule(moduleName);
        }

        // Creates a new class in ModuleBuilder.
        public void CreateClass(string name, Type parent = null)
        {
            _typeBuilder = _moduleBuilder.DefineType(name,
                                                     TypeAttributes.NotPublic |
                                                     TypeAttributes.Class |
                                                     TypeAttributes.AnsiClass |
                                                     TypeAttributes.BeforeFieldInit |
                                                     TypeAttributes.Sealed |
                                                     TypeAttributes.AutoLayout,
                                                     parent);
        }

        // Creates a method in TypeBuilder.
        public void CreateMethod(string name, Type @return, Type[] parameters, Action<ILGenerator> ilCode)
        {
            var mapMethod = _typeBuilder.DefineMethod(name,
                                                      MethodAttributes.Virtual | MethodAttributes.Assembly | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot,
                                                      @return,
                                                      parameters);

            ilCode(mapMethod.GetILGenerator());
        }

        // Creates the assembly and return the type created.
        public Type CreateType()
        {
            if (_typeCreated == null)
            {
                return _typeCreated = _typeBuilder.CreateTypeInfo().AsType();
            }

            return _typeCreated;
        }
        /*
                public MethodInfo GetCreatedMethod(string name)
                {
                    if (_methodInfo == null)
                    {
                        return _methodInfo = GetCreatedType().GetMethod(name);
                    }

                    return _methodInfo;
                }*/

        /*#if DEBUG
            ilProvider.SaveAssembly();
        #endif*/

        /*#if DEBUG
                // Save the generated assembly in dll.
                public void SaveAssembly()
                {
                    var generator = new AssemblyGenerator();
                    generator.GenerateAssembly(GetCreatedType().Assembly, $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $@"{FastMapperLibraryName}.GeneratedMapperAtRuntime.dll")}");
                }
        #endif*/
    }
}
