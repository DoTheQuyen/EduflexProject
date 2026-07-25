using System.ComponentModel;
using System.Reflection;

namespace ShareService.Enums
{
    public static class EnumExtensions
    {
        public static bool Is<TEnum>(this string? value, TEnum enumValue) where TEnum : struct, Enum =>
            string.Equals(value, enumValue.ToString(), StringComparison.Ordinal);

        public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct, Enum
        {
            var field = typeof(TEnum).GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? enumValue.ToString();
        }
    }
}