using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// Unity <c>Packages/packages-lock.json</c> 文件的反序列化数据模型（仅保留 <c>cn.etetet.*</c> 包信息）。
    /// </summary>
    public class PackagesLock
    {
        /// <summary>
        /// Unity 工程根目录路径。
        /// </summary>
        [BsonIgnore]
        public string unityDir;
        
        /// <summary>
        /// 包名到包详细信息的映射字典。
        /// </summary>
        public Dictionary<string, PackageInfo> dependencies = new();
    }
    
    /// <summary>
    /// 单个 UPM 包的描述与定位信息。
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// 完整包名（如 <c>cn.etetet.core</c>）。
        /// </summary>
        [BsonIgnore]
        public string name;
        /// <summary>
        /// 去除 <c>cn.etetet.</c> 前缀后的模块短名（如 <c>core</c>）。
        /// </summary>
        [BsonIgnore]
        public string module;
        
        public string version;
        public int depth;
        /// <summary>
        /// 包来源类型（如 <c>embedded</c>、<c>git</c>、<c>registry</c>）。
        /// </summary>
        public string source;
        public string hash;
        public string url;
        /// <summary>
        /// 解析出的该包在本地磁盘上的实际物理目录路径。
        /// </summary>
        public string dir;
        public Dictionary<string, string> dependencies = new();
    }
    
    /// <summary>
    /// ET UPM 包信息解析工具类，用于读取 <c>packages-lock.json</c> 并定位所有 <c>cn.etetet.*</c> 包的本地磁盘目录。
    /// </summary>
    public static class PackageHelper
    {
        /// <summary>
        /// 加载并解析指定 Unity 工程目录下的 <c>Packages/packages-lock.json</c>，筛选出所有 <c>cn.etetet.*</c> 包并计算其本地路径。
        /// </summary>
        public static PackagesLock LoadEtPackagesLock(string unityDir)
        {
            string s = File.ReadAllText(Path.Combine(unityDir, "Packages/packages-lock.json"));

            PackagesLock packagesLock = MongoHelper.FromJson<PackagesLock>(s);
            packagesLock.unityDir = unityDir;
            
            foreach (string k in packagesLock.dependencies.Keys.ToArray())
            {
                if (!k.StartsWith("cn.etetet"))
                {
                    packagesLock.dependencies.Remove(k);
                    continue;
                }

                PackageInfo packageInfo = packagesLock.dependencies[k];

                packageInfo.name = k;
                packageInfo.module = k.Replace("cn.etetet.", "");
                
                if (packageInfo.source == "embedded")
                {
                    packageInfo.dir = Path.Combine(unityDir, "Packages", packageInfo.version.Replace("file:", ""));
                }
                else if (packageInfo.source == "git")
                {
                    string p1 = Path.Combine(unityDir, "Library/PackageCache", k + "@" + packageInfo.hash.Substring(0, 10));
                    string p2 = Path.Combine(unityDir, "Packages", k + "@" + packageInfo.hash);
                    if (Directory.Exists(p1))
                    {
                        packageInfo.dir = p1;
                    }
                    else if (Directory.Exists(p2))
                    {
                        packageInfo.dir = p2;
                    }
                    else
                    {
                        throw new Exception($"not found package: {p1} {p2}");
                    }
                }
                else
                {
                    packageInfo.dir = Path.Combine(unityDir, "Library/PackageCache", k + "@" + packageInfo.version);
                }
            }
            
            return packagesLock;
        }
    }
}