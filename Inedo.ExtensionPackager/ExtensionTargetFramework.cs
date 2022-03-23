using System;

namespace Inedo.ExtensionPackager
{
    [Flags]
    internal enum ExtensionTargetFramework
    {
        None = 0,
        Net452 = 1,
        Net50 = 2,
        Net60 = 4
    }
}
