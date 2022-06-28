﻿
namespace LuizStudios.RuntimeClasses
{
    /// <summary>
    /// Class that will be the parent class of the class that will be created at runtime by Reflection.Emit.
    /// </summary>
    internal abstract class RuntimeBaseClass<TTarget>
    {
        /// <summary>
        /// Method that will map the source object to the destination object.
        /// </summary>
        internal abstract TTarget MakeMap(object source);
    }
}