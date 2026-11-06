using MongoDB.Bson;

namespace ET
{
    /// <summary>
    /// ET10 框架所有自定义核心类型（Entity、System、Handler、Message 等）的最顶层抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 1. 为什么标注 <see cref="EnableClassAttribute"/>？
    ///    ET 为了遵循严格的 ECS 数据/逻辑分离架构，默认通过 Roslyn 分析器禁止随意定义普通 class。
    ///    继承自 <see cref="ET.Object"/> 的类自带 <c>[EnableClass]</c> 豁免权，允许在框架体系内合法定义派生类。
    /// 2. 提供了开箱即用的 <see cref="ToJson"/> 与 <see cref="ToBson"/> 序列化辅助方法，方便日志打印、调试与存档。
    /// </remarks>
    [EnableClass]
    public abstract class Object
    {
        /// <summary>
        /// 将当前对象转换为 JSON 字符串表示（常用于 IDE 调试器悬停查看与日志输出）。
        /// </summary>
        public override string ToString()
        {
            // 这里不能用MongoHelper.ToJson，因为单步调试会调用ToString来显示数据
            // 如果MongoHelper.ToJson会调用BeginInit,就出大事了
            // return MongoHelper.ToJson(this);
            return ((object)this).ToJson();
        }
        
        /// <summary>
        /// 使用 <see cref="MongoHelper"/> 将对象序列化为 JSON 字符串（会触发 <c>ISupportInitialize</c> 钩子）。
        /// </summary>
        public string ToJson()
        {
            return MongoHelper.ToJson(this);
        }
        
        /// <summary>
        /// 使用 <see cref="MongoHelper"/> 将对象序列化为 BSON 二进制字节数组。
        /// </summary>
        public byte[] ToBson()
        {
            return MongoHelper.Serialize(this);
        }
    }
}