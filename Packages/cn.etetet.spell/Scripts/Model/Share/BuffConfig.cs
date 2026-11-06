using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// Buff 配置表管理单例，存储并管理所有通过 BSON 反序列化加载的 BuffConfig。
    /// </summary>
    [ConfigProcess(ConfigType.Code)]
    public partial class BuffConfigCategory : Singleton<BuffConfigCategory>, IConfig
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, BuffConfig> _dataMap = new();

        public BuffConfigCategory(System.Collections.Generic.Dictionary<int, ET.BuffConfig> dataMap)
        {
            _dataMap = dataMap;
        }
        
        public void Add(BuffConfig buffConfig)
        {
            this._dataMap.Add(buffConfig.Id, buffConfig);
        }

        public BuffConfig Get(int id)
        {
            this._dataMap.TryGetValue(id, out BuffConfig item);
            return item;
        }

        public bool Contain(int id)
        {
            return this._dataMap.ContainsKey(id);
        }

        public IReadOnlyDictionary<int, BuffConfig> GetAll()
        {
            return this._dataMap;
        }

        public void ResolveRef()
        {
            foreach (var kv in this._dataMap)
            {
                kv.Value.ResolveRef();
            }
            EndRef();
        }
        
        partial void EndRef();
    }

    /// <summary>
    /// 单个 Buff 的配置数据，包含基础持续时间、叠加规则、移除标记以及挂载的各种生命周期行为树效果节点（EffectNode）。
    /// </summary>
    [HideReferenceObjectPicker]
    [System.Serializable]
    public partial class BuffConfig : Object
    #if UNITY
            ,UnityEngine.ISerializationCallbackReceiver
    #endif
    {
        [ReadOnly]
        [LabelText("ID")]
        public int Id;

        [LabelText("描述")]
        public string Desc;

        [LabelText("持续时间")]
        public int Duration = 1000000000;

        [LabelText("Tick间隔时间")]
        public int TickTime;

        [LabelText("最大层数")]
        public int MaxStack = 1;

        [LabelText("层数")]
        public int Stack = 1;

        [LabelText("叠加规则类型")]
        public OverLayRuleType OverLayRuleType;

        [LabelText("移除条件")]
        public HashSet<BuffFlags> Flags = new();

        [LabelText("广播客户端类型")]
        public NoticeType NoticeType;

        #if UNITY
        [UnityEngine.SerializeReference]
        #endif
        [LabelText("  ")]
        //[HideReferenceObjectPicker]
        [BoxGroup("效果节点")]
        public List<EffectNode> Effects = new();

        #if UNITY
        [NonSerialized]
        [UnityEngine.HideInInspector]
        #endif
        [BsonIgnore]
        public Dictionary<Type, EffectNode> effectDict;

        public void OnBeforeSerialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }

        public void OnAfterDeserialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }

        /// <summary>
        /// 获取指定生命周期类型的行为树根节点（例如 EffectServerBuffAdd、EffectServerBuffTick 等）。
        /// </summary>
        public T GetEffect<T>() where T : EffectNode
        {
            this.effectDict.TryGetValue(typeof(T), out EffectNode effectNode);
            return effectNode as T;
        }
        
        public void ResolveRef()
        {
            EndRef();
        }

        void EndRef()
        {
            OnAfterDeserialize();
        }
    }

    /// <summary>
    /// Buff 同 ID 重复添加时的叠加规则枚举。
    /// </summary>
    public enum OverLayRuleType
    {
        [LabelText("无")]
        None,

        [LabelText("层数")]
        AddStack,

        [LabelText("时间")]
        AddTime,

        [LabelText("替换")]
        Replace,
    }
}
