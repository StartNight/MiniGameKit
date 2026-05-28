using System;
using System.Reflection;

namespace MiniGameKit.Editor
{
    internal static class ReflectionTypeUtility
    {
        public static Type FindType(string fullTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullTypeName);
                if (t != null)
                    return t;
            }

            return null;
        }

        public static FieldInfo FindInstanceField(Type type, string name) =>
            type?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
