#if ODIN_INSPECTOR
using System;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 包依赖项数据模型（既可表示“我依赖的包”，也可表示“依赖我的包”）
    /// </summary>
    [Serializable]
    [HideLabel]
    [HideReferenceObjectPicker]
    public class DependencyInfo
    {
        /// <summary>
        /// 关联包的名称
        /// </summary>
        [OdinSerialize]
        [ReadOnly]
        [LabelText("名称")]
        public string Name;

        [OdinSerialize]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("版本")]
        [OnValueChanged("OnVersionChanged")]
        private string version;

        /// <summary>
        /// 依赖记录的版本号字符串（设置时自动过滤非数字字符并计算长整型版本号）
        /// </summary>
        public string Version
        {
            get => version;
            set
            {
                version     = Regex.Replace(value, ETPackageVersionModule.Pattern, "");
                VersionLong = PackageHelper.GetVersionToLong(version);
            }
        }

        /// <summary>
        /// 所属自身包的名称
        /// </summary>
        [OdinSerialize]
        [HideInInspector]
        public string SelfName;

        /// <summary>
        /// 是否为反向依赖（true 表示“依赖我的包”，false 表示“我依赖的包”）
        /// </summary>
        [OdinSerialize]
        [HideInInspector]
        public bool DependenciesSelf;

        /// <summary>
        /// 转换后的长整型版本号（用于版本大小比较）
        /// </summary>
        [HideInInspector]
        public long VersionLong;

        private void OnVersionChanged()
        {
            Version = version;
        }

        #region 我依赖的版本不是最新时

        [Button("同步")]
        [GUIColor(0.7f, 0.4f, 0.8f)]
        [ShowIf("SyncVersionShowIf")]
        public void SyncVersion()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(Name);
            if (packageInfo == null)
            {
                return;
            }

            ETPackageVersionModule.Inst.ChageDependencies(packageInfo);
        }

        //同步目标的版本 当目标name的版本与当前版本不一致时显示同步按钮
        public bool SyncVersionShowIf()
        {
            if (DependenciesSelf)
            {
                return false;
            }

            if (string.IsNullOrEmpty(Name))
            {
                return false;
            }

            var packageInfo = ETPackageVersionModule.Inst?.GetPackageInfoData(Name);
            if (packageInfo == null)
            {
                return false;
            }

            return packageInfo.Version != Version;
        }

        #endregion

        #region 依赖我的版本不是最新时

        [Button("同步")]
        [GUIColor(0.4f, 0.8f, 1)]
        [ShowIf("SyncSelfVersionShowIf")]
        public void SyncSelfVersion()
        {
            if (string.IsNullOrEmpty(SelfName))
            {
                return;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(SelfName);
            if (packageInfo == null)
            {
                return;
            }

            ETPackageVersionModule.Inst.ChageDependencies(packageInfo);
        }

        //依赖我  但是我的版本已经升级了 他哪里的版本还是旧的时候显示同步按钮
        public bool SyncSelfVersionShowIf()
        {
            if (!DependenciesSelf)
            {
                return false;
            }

            var packageInfo = ETPackageVersionModule.Inst?.GetPackageInfoData(SelfName);
            if (packageInfo == null)
            {
                return false;
            }

            return packageInfo.Version != Version;
        }

        #endregion
    }
}
#endif