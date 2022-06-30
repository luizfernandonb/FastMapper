using System;

namespace LuizStudios.Attributes
{
    /// <summary>
    /// Attribute that indicates that the property will be ignored when mapping between two objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class FastMapperIgnoreAttribute : Attribute
    {
        public object DefaultValue { get; set; }

        public FastMapperIgnoreAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }
    }
}
