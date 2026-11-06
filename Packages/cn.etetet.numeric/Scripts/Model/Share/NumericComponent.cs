using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    /// <summary>
    /// 单位数值变化事件参数结构体。
    /// </summary>
    public struct NumbericChange
    {
        /// <summary>
        /// 数值发生变化的单位引用。
        /// </summary>
        public EntityRef<Unit> Unit { get; set; }

        /// <summary>
        /// 发生变化的一级属性类型。
        /// </summary>
        public NumericType NumericType {get; set;}

        /// <summary>
        /// 变化前的旧数值。
        /// </summary>
        public long Old  {get; set;}

        /// <summary>
        /// 变化后的新数值。
        /// </summary>
        public long New  {get; set;}
    }
    
    /// <summary>
    /// 扩展 <see cref="Unit"/>，提供直达 <see cref="ET.NumericComponent"/> 的快捷属性引用（配合 <see cref="DisableGetComponentAttribute"/> 避免字典查找开销）。
    /// </summary>
    public partial class Unit
    {
        private EntityRef<NumericComponent> numericComponent;

        /// <summary>
        /// 获取或设置当前单位挂载的数值组件。
        /// </summary>
        public NumericComponent NumericComponent
        {
            get
            {
                return this.numericComponent;
            }
            set
            {
                this.numericComponent = value;
            }
        }
    }

    /// <summary>
    /// 单位数值属性组件（以键值对字典形式存储单位的所有一级与二级属性，支持跨进程传送与 Bson 持久化）。
    /// </summary>
    [DisableGetComponent]
    [ComponentOf(typeof (Unit))]
    public class NumericComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        /// <summary>
        /// 存储所有 <see cref="NumericType"/> 及其对应 long 值的字典。
        /// </summary>
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<NumericType, long> NumericDic = new();
    }
}
