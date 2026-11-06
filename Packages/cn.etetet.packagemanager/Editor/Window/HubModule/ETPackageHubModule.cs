#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// ET 官方与社区包仓库（Hub）浏览与安装管理模块
    /// </summary>
    [ETPackageMenu("库")]
    public class ETPackageHubModule : BasePackageToolModule
    {
        public static ETPackageHubModule Inst;

        [HideLabel]
        [HideIf("CheckUpdateAllEnd")]
        [ShowInInspector]
        [DisplayAsString(false, 100, TextAlignment.Center, true)]
        private const string m_CheckUpdateAllReqing = "请求所有包最新数据中...";

        /// <summary>
        /// 所有远端包数据是否已请求完毕
        /// </summary>
        public bool CheckUpdateAllEnd { get; private set; }

        /// <summary>
        /// 远端请求是否成功
        /// </summary>
        public bool RequestAllResult { get; private set; }

        /// <summary>
        /// 初始化模块：先检查本地包更新，再拉取 GitHub 上的 ET-Packages 列表并按分类构建子菜单
        /// </summary>
        public override void Initialize()
        {
            CheckUpdateAllEnd = false;
            RequestAllResult  = false;

            PackageHelper.CheckUpdateAll((result) =>
            {
                if (!result)
                {
                    UnityTipsHelper.ShowError("获取所有包最新数据失败！请检查网络或关闭工具后重试");
                    return;
                }

                PackageHubHelper.CheckUpdate((result2) =>
                {
                    if (!result2)
                    {
                        UnityTipsHelper.ShowError("获取所有包最新数据失败！请检查网络或关闭工具后重试");
                        return;
                    }

                    CheckUpdateAllEnd = true;
                    RequestAllResult  = true;
                    Inst              = this;
                    CreateCategory();
                });
            });
        }

        public override void OnDestroy()
        {
            PackageHubHelper.SaveAsset();
            Inst = null;
        }

        private void CreateCategory()
        {
            if (PackageHubHelper.PackageHubAsset == null) return;
            var allPackageData = PackageHubHelper.PackageHubAsset.AllPackageData?.Values;
            if (allPackageData is not { Count: > 0 })
            {
                UnityTipsHelper.Show("没有任何包数据！");
                return;
            }

            var packageList = new List<PackageHubData>();
            foreach (var package in allPackageData)
            {
                packageList.Add(package);
            }

            var allName   = EPackageCategoryType.All.ToString();
            var otherName = EPackageCategoryType.Other.ToString();
            CreateCategoryItem(allName, EPackageCategoryType.All, packageList);

            var allCategoryData = PackageHubHelper.GetNextCategoryData(packageList, 1);
            if (allCategoryData is { Count: > 0 })
            {
                var keyList = allCategoryData.Keys.ToList();
                keyList.Sort();

                foreach (var key in keyList)
                {
                    var categoryName = key;
                    if (categoryName == otherName) continue;
                    CreateCategoryItem(categoryName, EPackageCategoryType.Custom, allCategoryData[key]);
                }
            }

            if (allCategoryData.TryGetValue(otherName, out var value))
            {
                CreateCategoryItem(otherName, EPackageCategoryType.Other, value);
            }

            foreach (var menu in Tree.MenuItems)
            {
                if (menu.Name == ModuleName)
                {
                    foreach (var item in menu.ChildMenuItems)
                    {
                        if (item.Name == allName)
                        {
                            item.Select();
                            break;
                        }
                    }

                    break;
                }
            }
        }

        private void CreateCategoryItem(string categoryName, EPackageCategoryType categoryType, List<PackageHubData> categoryData)
        {
            var categoryPath = $"{ModuleName}/{categoryName}";
            var menuItem     = new TreeMenuItem<PackageCategoryModule>(AutoTool, Tree, categoryPath, EditorIcons.Folder);
            menuItem.UserData = new PackageCategoryData
            {
                CategoryType = categoryType,
                Category     = categoryName,
                CategoryPath = categoryPath,
                Layer        = 1,
                AllPackages  = categoryData,
            };
        }
    }
}
#endif