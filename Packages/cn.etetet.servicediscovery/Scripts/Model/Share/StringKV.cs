using System.Collections;
using System.Collections.Generic;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    /// <summary>
    /// 支持 MemoryPack 序列化与 MongoDB BSON 序列化的字符串键值对字典封装类（广泛用于服务发现 Metadata 与过滤条件）。
    /// </summary>
    [EnableClass]
    [MemoryPackable]
    public partial class StringKV : IEnumerable<KeyValuePair<string, string>>
    {
        /// <summary>内部存储的字符串键值字典</summary>
        [MemoryPackOrder(0)]
        [MemoryPackInclude]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<string, string> data = new();

        [MemoryPackConstructor]
        public StringKV() { }

        /// <summary>拷贝构造函数</summary>
        public StringKV(StringKV other)
        {
            this.data = new Dictionary<string, string>(other.data);
        }

        public override string ToString()
        {
            return MongoHelper.ToJson(this.data);
        }

        // ✅ 这个方法让集合初始化器语法可用
        public void Add(string key, string value) => this.data.Add(key, value);
        public void Add(KeyValuePair<string, string> item) => this.data[item.Key] = item.Value;

        // ✅ 实现 IEnumerable，让集合初始化器合法
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this.data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ✅ 一些常用封装（可选）
        public string this[string key]
        {
            get => this.data[key];
            set => this.data[key] = value;
        }

        public bool TryGetValue(string key, out string value)
        {
            return this.data.TryGetValue(key, out value);
        }

        public bool ContainsKey(string key) => this.data.ContainsKey(key);
        public bool Remove(string key) => this.data.Remove(key);
        public void Clear() => this.data.Clear();
        public int Count => this.data.Count;
    }
}
