using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 技能配置表管理单例，存储并管理所有通过 BSON 反序列化加载的 SpellConfig。
    /// </summary>
    [ConfigProcess(ConfigType.Code)]
    public partial class SpellConfigCategory : Singleton<SpellConfigCategory>, IConfig
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, SpellConfig> _dataMap = new();

        public SpellConfigCategory(System.Collections.Generic.Dictionary<int, ET.SpellConfig> dataMap)
        {
            _dataMap = dataMap;
        }
        
        public void Add(SpellConfig spellConfig)
        {
            this._dataMap.Add(spellConfig.Id, spellConfig);
        }

        public SpellConfig Get(int id)
        {
            this._dataMap.TryGetValue(id, out SpellConfig item);
            return item;
        }

        public bool Contain(int id)
        {
            return this._dataMap.ContainsKey(id);
        }

        public IReadOnlyDictionary<int, SpellConfig> GetAll()
        {
            return this._dataMap;
        }

        public void ResolveRef()
        {
            foreach (var _v in this._dataMap.Values)
            {
                _v.ResolveRef();
            }
            EndRef();
        }

        partial void EndRef();
    }

    [LabelText("施法类型")]
    public enum CastTimeType
    {
        [LabelText("普通攻击")]
        Attack,

        [LabelText("读条")]
        Cast,

        [LabelText("吟唱")]
        Channeling,
    }

    /// <summary>
    /// 单个主动技能的配置数据，包含技能基础信息、消耗行为树（CostNode）和目标选择行为树（TargetSelector）。
    /// 技能释放成功后的具体效果由同 ID 的 BuffConfig 承载。
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public partial class SpellConfig: Object
    {
        [ReadOnly]
        [BoxGroup("技能信息")]
        [LabelText("技能 ID")]
        public int Id;
        
        [BoxGroup("技能信息")]
        [LabelText("描述")]
        public string Desc;

        [BoxGroup("技能信息")]
        [LabelText("图标资源名")]
        public string IconName;
        
        [BoxGroup("技能信息")]
        [LabelText("技能CD（毫秒）")]
        public int CD;

        [BoxGroup("技能信息")]
        [LabelText("伤害系数（百分比）")]
        public int DamageMultiplier = 100;
        
        [LabelText("消耗")]
        public CostNode Cost;

#if UNITY
        [UnityEngine.SerializeReference]
#endif
        [LabelText("目标选择")]
        public TargetSelector TargetSelector;

        public  void ResolveRef()
        {
            EndRef();
        }
        
        partial void EndRef();
    }
}
