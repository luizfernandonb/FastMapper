using System;
using System.Collections.Generic;
using System.Text;

namespace LuizStudios.Configuration
{
    public sealed class FastMapperConfiguration
    {
        public bool IgnorePropertiesCase { get; set; }

        public int InstancesArraySize { get; set; } = 4;
    }
}
