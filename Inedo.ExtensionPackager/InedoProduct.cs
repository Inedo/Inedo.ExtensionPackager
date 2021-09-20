using System;

namespace Inedo.ExtensionPackager
{
    /// <summary>
    /// Copied from Inedo.SDK.
    /// </summary>
    [Flags]
    internal enum InedoProduct
    {
        Unspecified = 0,
        BuildMaster = 1,
        ProGet = 2,
        Otter = 4
    }
}
