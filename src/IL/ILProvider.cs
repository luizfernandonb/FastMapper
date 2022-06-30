using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LuizStudios.IL
{
    // Provides some methods from Reflection.Emit library.
    internal sealed class ILProvider
    {
        private Type _typeCreated;

        // Builders
        private TypeBuilder _typeBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        internal ILProvider(string assemblyName, string moduleName)
        {
            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect)
                                            .DefineDynamicModule(moduleName);
        }

        /// <summary>
        /// Creates a new class in ModuleBuilder.
        /// </summary>
        internal void CreateClass(string name, Type parent = null)
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

        /// <summary>
        /// Creates a method in TypeBuilder. 
        /// </summary>
        internal void CreateMethod(string name, Type @return, Type[] parameters, Action<ILGenerator> ilCode)
        {
            var mapMethod = _typeBuilder.DefineMethod(name,
                                                      MethodAttributes.Virtual | MethodAttributes.Assembly | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot,
                                                      @return,
                                                      parameters);

            ilCode(mapMethod.GetILGenerator());
        }

        /// <summary>
        /// Creates the type and returns it.
        /// </summary>
        /// <returns></returns>
        internal Type CreateType()
        {
            if (_typeCreated == null)
            {
                return _typeCreated = _typeBuilder.CreateTypeInfo().AsType();
            }

            return _typeCreated;
        }

#if DEBUG
        /// <summary>
        /// Returns the assembly of the created type.
        /// </summary>
        internal Assembly GetAssemblyOfCreatedType()
        {
            return (Assembly)((dynamic)CreateType()).Assembly;
        }
#endif
    }
}
