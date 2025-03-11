using System;

namespace Mavercloud.PDF.Helpers
{
    /// <summary>
    /// 常用公共操作
    /// </summary>
    public static class Common {
        /// <summary>
        /// 获取类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static Type GetType<T>() {
            var type = typeof( T );
            return Nullable.GetUnderlyingType( type ) ?? type;
        }

        public static Type GetType(Type t)
        {
            return Nullable.GetUnderlyingType(t) ?? t;
        }

        /// <summary>
        /// 换行符
        /// </summary>
        public static string Line => Environment.NewLine;
    }
}
