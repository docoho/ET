using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// Unity 编辑器下的程序集编译与热重载工具类。
    /// 提供快捷键 <c>F6</c>（编译热更 DLL 并复制到 <see cref="Define.CodeDir"/>）与 <c>F7</c>（运行期热重载 <c>ET.Hotfix</c> / <c>ET.HotfixView</c>）。
    /// </summary>
    public static class AssemblyTool
    {
        /// <summary>
        /// Unity线程的同步上下文
        /// </summary>
        static SynchronizationContext unitySynchronizationContext { get; set; }

        /// <summary>
        /// 程序集名字数组
        /// </summary>
        public static readonly string[] DllNames = { "ET.Config", "ET.Hotfix", "ET.HotfixView", "ET.Model", "ET.ModelView" };

        /// <summary>
        /// 在编辑器加载时记录 Unity 主线程的 <see cref="SynchronizationContext"/>，以便在 PlayMode 下调用编译 API 时临时切换回主线程同步上下文。
        /// </summary>
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            unitySynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// 菜单和快捷键编译按钮（快捷键 <c>F6</c>）。
        /// </summary>
        [MenuItem("ET/Scripts/Compile _F6", false)]
        static void MenuItemOfCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，文件修改时间不准确
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            DoCompile();
        }

        /// <summary>
        /// 菜单和快捷键热重载按钮（快捷键 <c>F7</c>）：在编辑器运行状态下调用 <see cref="CodeLoader.Reload"/> 热更逻辑代码。
        /// </summary>
        [MenuItem("ET/Scripts/Reload _F7", false)]
        static void MenuItemOfReload()
        {
            if (Application.isPlaying)
            {
                CodeLoader.Instance?.Reload();
            }
        }

        /// <summary>
        /// 执行编译代码流程：
        /// 1. 强制刷新 AssetDatabase；
        /// 2. 执行 <c>ET.CodeMode.dll</c> 更新各包 <c>.asmref</c>；
        /// 3. 调用 Unity 内部脚本编译器编译 Player/Editor 脚本；
        /// 4. 将生成的 DLL/PDB 拷贝为 <c>.bytes</c> 资源文件放入 <see cref="Define.CodeDir"/>。
        /// </summary>
        public static void DoCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，编译出老代码
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={globalConfig.CodeMode}", ".", true);
            process.WaitForExit();

            bool isCompileOk = CompileDlls(globalConfig.EditorScripts);
            if (!isCompileOk)
            {
                return;
            }

            CopyHotUpdateDlls();

            Log.Info($"Compile Finish!");
        }

        /// <summary>
        /// 调用 Unity 内部 <c>PlayerBuildInterface.CompilePlayerScriptsInternal</c> 将工程脚本编译成 DLL 输出至 <see cref="Define.BuildOutputDir"/>。
        /// </summary>
        static bool CompileDlls(bool editorScript = false)
        {
            // 运行时编译需要先设置为UnitySynchronizationContext, 编译完再还原为CurrentContext
            SynchronizationContext lastSynchronizationContext = Application.isPlaying ? SynchronizationContext.Current : null;
            SynchronizationContext.SetSynchronizationContext(unitySynchronizationContext);

            bool isCompileOk = false;

            try
            {
                Directory.CreateDirectory(Define.BuildOutputDir);
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                ScriptCompilationSettings scriptCompilationSettings = new()
                {
                    group = group,
                    target = target,
                    extraScriptingDefines = new[] { "IS_COMPILING" },
                    options = EditorUserBuildSettings.development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None
                };

                MethodInfo compilePlayerScriptsInternalMethod = typeof(PlayerBuildInterface).GetMethod("CompilePlayerScriptsInternal",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ScriptCompilationSettings), typeof(string), typeof(bool) },
                    null);

                if (compilePlayerScriptsInternalMethod == null)
                {
                    Log.Error("Failed to find PlayerBuildInterface.CompilePlayerScriptsInternal method!");
                    return false;
                }

                var result = (ScriptCompilationResult)compilePlayerScriptsInternalMethod.Invoke(null,
                    new object[] { scriptCompilationSettings, Define.BuildOutputDir, editorScript });

                isCompileOk = result.assemblies.Count > 0;
                EditorUtility.ClearProgressBar();
            }
            finally
            {
                if (lastSynchronizationContext != null)
                {
                    SynchronizationContext.SetSynchronizationContext(lastSynchronizationContext);
                }
            }

            return isCompileOk;
        }

        /// <summary>
        /// 将编译生成的 DLL 与 PDB 文件加上 <c>.bytes</c> 后缀复制到 <see cref="Define.CodeDir"/> 目录，供 YooAsset 打包或 Reload 热重载读取。
        /// </summary>
        static void CopyHotUpdateDlls()
        {
            FileHelper.CleanDirectory(Define.CodeDir);

            if (!Directory.Exists(Define.CodeDir))
            {
                Directory.CreateDirectory(Define.CodeDir);
            }

            foreach (string dllName in DllNames)
            {
                string sourceDll = $"{Define.BuildOutputDir}/{dllName}.dll";
                string sourcePdb = $"{Define.BuildOutputDir}/{dllName}.pdb";
                if (!File.Exists(sourceDll))
                {
                    sourceDll = $"Bin/{dllName}.dll";
                }
                if (!File.Exists(sourcePdb))
                {
                    sourcePdb = $"Bin/{dllName}.pdb";
                }
                File.Copy(sourceDll, $"{Define.CodeDir}/{dllName}.dll.bytes", true);
                File.Copy(sourcePdb, $"{Define.CodeDir}/{dllName}.pdb.bytes", true);
            }

            AssetDatabase.Refresh();
        }
    }
}
