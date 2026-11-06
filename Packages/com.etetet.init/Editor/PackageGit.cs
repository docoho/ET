using System;
using System.Collections.Generic;
using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 对应每个包根目录下 <c>packagegit.json</c> 配置文件的反序列化数据模型。
    /// 记录了包的唯一编号（<see cref="Id"/>）、包名、Git 依赖表以及脚本引用关系。
    /// </summary>
    [BsonIgnoreExtraElements]
    public class PackageGit
    {
        /// <summary>包的唯一数字编号（与 <c>PackageType</c> 中的常量保持一致）。</summary>
        public int Id;
        /// <summary>包名称（如 <c>cn.etetet.core</c>）。</summary>
        public string Name;
        /// <summary>Git 包依赖字典（Key: 包名, Value: Git 地址或版本）。</summary>
        public Dictionary<string, string> GitDependencies;
        /// <summary>脚本分层引用配置字典。</summary>
        public Dictionary<string, string[]> ScriptsReferences;
    }

    /// <summary>
    /// <see cref="PackageGit"/> 配置文件加载辅助类。
    /// </summary>
    public static class PackageGitHelper
    {
        /// <summary>
        /// 从指定路径读取并反序列化 <c>packagegit.json</c> 文件。
        /// </summary>
        /// <param name="packageJsonPath"><c>packagegit.json</c> 的完整或相对文件路径。</param>
        /// <returns>反序列化后的 <see cref="PackageGit"/> 实例。</returns>
        public static PackageGit Load(string packageJsonPath)
        {
            if (!File.Exists(packageJsonPath))
            {
                throw new Exception($"not found packagegit.json: {packageJsonPath}, retry refresh unity!");
            }
            
            string packageJsonContent = File.ReadAllText(packageJsonPath);

            PackageGit packageGit = BsonSerializer.Deserialize<PackageGit>(packageJsonContent);
            return packageGit;
        }
    }
}