using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ET;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager.Requests;
using Debug = UnityEngine.Debug;

namespace Hibzz.DependencyResolver
{
    /// <summary>
    /// UPM 包依赖解析与本地化迁移工具。
    /// 监听 Unity Package Manager 的包注册事件，自动将下载到 <c>Library/PackageCache</c> 中的 <c>cn.etetet.*</c> 包迁移到项目根目录的 <c>Packages/</c> 目录下作为可编辑的本地包。
    /// </summary>
    [InitializeOnLoad]
    public class DependencyResolver
    {
        /// <summary>
        /// 调用 PowerShell 脚本（<c>MoveToPackages.ps1</c> 或 Unity 6 下的 <c>MoveToPackages_6.ps1</c>）将指定的包从 <c>Library/PackageCache</c> 移动到 <c>Packages</c> 目录。
        /// </summary>
        /// <param name="package">包名（如 <c>cn.etetet.core</c>）。</param>
        /// <param name="version">包版本号。</param>
        //[MenuItem("ET/MoveToPackage")]
        static void MoveToPackage(string package, string version)
        {
            string packageName = default;
            string moveFileName = default;
            #if UNITY_6000_0_OR_NEWER
            packageName = package;
            moveFileName = "MoveToPackages_6";
            #else 
            packageName =$"{package}@{version}";
            moveFileName = "MoveToPackages";
            #endif
            string dir = Path.Combine("Library/PackageCache", packageName);
            if (!Directory.Exists(dir))
            {
                return;
            }
        
            Debug.Log($"move package: {packageName}");
            Process process = ProcessHelper.PowerShell($"-NoExit -ExecutionPolicy Bypass -File ./Packages/com.etetet.init/{moveFileName}.ps1 {package} {version}", waitExit: true);
            process.WaitForExit();
        }
        
        /// <summary>
        /// 静态构造函数：在 Unity 编辑器加载时订阅 Package Manager 的包注册完成事件。
        /// </summary>
        static DependencyResolver()
        {
            Events.registeredPackages += OnPackagesRegistered;
        }

        // Invoked when the package manager completes registering new packages
        /// <summary>
        /// 当 Unity Package Manager 完成新包安装或包版本变更注册时的回调：将所有新增或变更的 <c>cn.etetet.*</c> 包迁移至本地 <c>Packages/</c> 目录。
        /// </summary>
        static void OnPackagesRegistered(PackageRegistrationEventArgs packageRegistrationInfo)
        {
            if (packageRegistrationInfo.added.Count == 0 && packageRegistrationInfo.changedFrom.Count == 0)
            {
                return;
            }
            
            Debug.Log($"Packages Registered: {string.Join(" ", packageRegistrationInfo.added.Select(x=>x.name))}");
            
            // loop through all of the added packages and get their git
            // dependencies and add it to the list that contains all the
            // dependencies that need to be installed
            foreach (var package in packageRegistrationInfo.added)
            {
                if (!package.name.StartsWith("cn.etetet."))
                {
                    continue;
                }
                MoveToPackage(package.name, package.version);
            }
            
            foreach (var package in packageRegistrationInfo.changedFrom)
            {
                if (!package.name.StartsWith("cn.etetet."))
                {
                    continue;
                }
                MoveToPackage(package.name, package.version);
            }
            
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 菜单项 <c>ET/Init/RepairDependencies</c>：手动扫描 <c>Library/PackageCache</c> 下残留的所有 <c>cn.etetet.*</c> 包并迁移至 <c>Packages/</c> 目录。
        /// </summary>
        [MenuItem("ET/Init/RepairDependencies")]
        static void RepairDependencies()
        {
            foreach (var directory in Directory.GetDirectories("Library/PackageCache", "cn.etetet.*"))
            {
                string baseName = Path.GetFileName(directory);
                if (!baseName.StartsWith("cn.etetet."))
                {
                    continue;
                }
                
                string[] ss = baseName.Split("@");
                string packageName = ss[0];
                #if UNITY_6000_0_OR_NEWER
                string version = "";
                #else 
                string version = ss[1];
                #endif

                MoveToPackage(packageName, version);
            }
            
            AssetDatabase.Refresh();
            
            Debug.Log($"repaire package finish");
        }
    }
}
