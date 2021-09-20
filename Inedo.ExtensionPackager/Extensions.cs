using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;

namespace Inedo.ExtensionPackager
{
    internal static class Extensions
    {
        public static bool TryGetCustomAttribute(this AssemblyDefinition assembly, string fullName, [NotNullWhen(true)] out CustomAttribute? customAttribute)
        {
            customAttribute = null;

            if (assembly.HasCustomAttributes)
            {
                foreach (var attr in assembly.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == fullName)
                    {
                        customAttribute = attr;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
