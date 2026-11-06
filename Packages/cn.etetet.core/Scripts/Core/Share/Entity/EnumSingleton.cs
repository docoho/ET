using System;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// 静态常量枚举映射单例基类。
    /// <para>
    /// 【初学者指南】
    /// 在模块化 Package 架构中，不同包都可以通过 <c>public static partial class SceneType</c> 扩展自己的 <c>const int</c> 枚举值（C# 原生 <c>enum</c> 不支持 <c>partial</c> 跨包扩展）。
    /// <see cref="EnumSingleton{T}"/> 通过反射读取静态类中所有的 <c>int</c> 常量字段，建立“数值 &lt;-&gt; 名称字符串”的双向映射（<see cref="DoubleMap{K,V}"/>）。
    /// </para>
    /// </summary>
    [AllowInstance]
    public class EnumSingleton<T>: Singleton<T> where T: EnumSingleton<T>
    {
        /// <summary>存储 int 枚举值与字段名称字符串的双向映射字典</summary>
        protected readonly DoubleMap<int, string> enumValueString = new();
        
        /// <summary>
        /// 扫描目标类型中的所有公共静态 <c>int</c> 字段并构建双向映射表。
        /// </summary>
        protected void Init(Type type)
        {
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (fieldInfo.FieldType != typeof(int))
                {
                    continue;
                }
                this.enumValueString.Add((int)fieldInfo.GetValue(null), fieldInfo.Name);	
            }
        }
        
        /// <summary>
        /// 根据枚举整数值获取对应的字段名称字符串。
        /// </summary>
        public string GetStringByValue(int value)
        {
            return this.enumValueString.GetValueByKey(value);
        }
        
        /// <summary>
        /// 根据字段名称字符串获取对应的枚举整数值。
        /// </summary>
        public int GetValueByName(string name)
        {
            return this.enumValueString.GetKeyByValue(name);
        }

        /// <summary>
        /// 获取全部枚举值与名称的双向映射表。
        /// </summary>
        public DoubleMap<int, string> GetAllValues()
        {
            return this.enumValueString;
        }
    }
}
