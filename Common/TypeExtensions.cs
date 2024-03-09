using System.Runtime.InteropServices;

namespace Common;

public static class TypeExtensions
{
    public static bool IsNullable(this Type t)
        => t.BaseType == typeof(Nullable<>) || t.IsClass;
}