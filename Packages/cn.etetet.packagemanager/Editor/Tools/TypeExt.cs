using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// Type 反射与数值类型判断扩展工具类
    /// </summary>
    public static class TypeExt
    {
        /// <summary>
        /// 基础数值类型到 NumericType 枚举的映射表
        /// </summary>
        private static readonly Dictionary<Type, NumericType> g_numTypeMap;

        static TypeExt()
        {
            var numTypeMap = new Dictionary<Type, NumericType>(11);
            numTypeMap[typeof(byte)]    = NumericType.Byte;
            numTypeMap[typeof(sbyte)]   = NumericType.SByte;
            numTypeMap[typeof(short)]   = NumericType.Int16;
            numTypeMap[typeof(int)]     = NumericType.Int32;
            numTypeMap[typeof(long)]    = NumericType.Int64;
            numTypeMap[typeof(ushort)]  = NumericType.UInt16;
            numTypeMap[typeof(uint)]    = NumericType.UInt32;
            numTypeMap[typeof(ulong)]   = NumericType.UInt64;
            numTypeMap[typeof(float)]   = NumericType.Float;
            numTypeMap[typeof(double)]  = NumericType.Double;
            numTypeMap[typeof(decimal)] = NumericType.Decimal;
            g_numTypeMap                = numTypeMap;
        }

        /// <summary>
        /// 获取指定 Type 对应的 NumericType 枚举值（非数值类型返回 NaN）
        /// </summary>
        public static NumericType GetNumericType(this Type type)
        {
            if (g_numTypeMap.TryGetValue(type, out NumericType value))
            {
                return value;
            }

            return NumericType.NaN;
        }

        /// <summary>
        /// 判断一个类型是否是数值类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumericType(this Type type)
        {
            return g_numTypeMap.ContainsKey(type);

            //也可以这样
            //return !o.IsClass && !o.IsInterface && o.GetInterfaces().Any(q => q == typeof(IFormattable));
        }

        /// <summary>
        /// 判断一个类型是否为可空数值类型（Nullable&lt;T&gt; 且 T 为数值类型）
        /// </summary>
        public static bool IsNullableNumericType(this Type type)
        {
            return type.Name.StartsWith("Nullable") && type.GetGenericArguments()[0].IsNumericType();
        }

        /// <summary>
        /// 得到字段值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GetFieldValue<T>(this Type type, string fieldName, out T value, object obj = null)
        {
            var field = type.GetField(fieldName);
            if (field == null)
            {
                value = default;
                return false;
            }

            value = (T)field.GetValue(obj);
            return true;
        }

        /// <summary>
        /// 通过反射设置指定字段的值
        /// </summary>
        public static bool SetFieldValue(this Type type,        string name, object value,
                                         object    inst = null, BindingFlags bindingArray =
                                                 BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
        {
            var field = type.GetField(name, bindingArray);
            if (field == null)
            {
                return false;
            }

            field.SetValue(inst, value);
            return true;
        }

        /// <summary>
        /// 通过反射获取指定属性的值
        /// </summary>
        public static object GetPropertyValue(this Type type, string name, object inst = null,
                                              BindingFlags bindingArray =
                                                      BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
        {
            var instProp = type.GetProperty(name, bindingArray);
            if (instProp == null)
            {
                return null;
            }

            return instProp.GetValue(inst);
        }

        /// <summary>
        /// 通过反射调用静态方法并返回强类型结果
        /// </summary>
        public static TReturn StaticCall<TReturn>(this Type type, string methodName, object[] param = null,
                                                  TReturn   defReturn = default)
        {
            var info = type.GetMethod(methodName, BindingFlags.Static |
                                      BindingFlags.Public |
                                      BindingFlags.NonPublic |
                                      BindingFlags.FlattenHierarchy);
            if (info == null)
            {
                return defReturn;
            }

            return (TReturn)info.Invoke(null, param);
        }
    }

    /// <summary>
    /// 基础数值类型枚举定义
    /// </summary>
    public enum NumericType
    {
        NaN,
        Byte,
        SByte,
        Int16,
        Int32,
        Int64,
        UInt16,
        UInt32,
        UInt64,
        Float,
        Double,
        Decimal
    }
}
