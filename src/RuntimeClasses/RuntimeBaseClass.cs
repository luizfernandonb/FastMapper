using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FastMapperInRuntime_Assembly")]

namespace LuizStudios.RuntimeClasses
{
    /// <summary>
    /// Class that will be the parent class of the class that will be created at runtime by Reflection.Emit.
    /// </summary>
    internal abstract class RuntimeBaseClass
    {
        /// <summary>
        /// Method that will map the source object to the destination object.
        /// </summary>
        internal abstract object MakeMap(object source);
    }
}
