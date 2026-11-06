using System;
using System.Collections.Generic;
using System.Linq;
using ET.Client;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 技能编辑器主窗口（菜单路径：ET/Spell/Spell Editor），提供按主技能聚合展示整条 Spell/Buff 链路、直接编辑属性、跳转行为树以及一键复制技能链的功能。
    /// </summary>
    public sealed class SpellEditorWindow : EditorWindow
    {
        private SpellEditorAssetIndex assetIndex;
        private SpellEditorBuildResult buildResult;
        private List<SpellEditorIssue> issues = new();
        private int selectedMainSpellId;
        private Vector2 scroll;
        private Vector2 mainSpellListScroll;
        private Vector2 spellTableScroll;
        private Vector2 buffTableScroll;
        private bool needsRebuild;
        private float mainSpellPanelWidth = 260f;
        private bool resizingMainSpellPanel;
        private UnityEngine.Object selectedAsset;
        private int renameId;
        private int copyMainSpellId;
        private int newMainSpellId;
        private string mainSpellFilter = string.Empty;
        private Type[] effectNodeTypes;
        private Type[] costNodeTypes;
        private Type[] targetSelectorTypes;
        private Dictionary<string, Sprite> iconSpritesByName;

        [MenuItem(SpellEditorConstants.MenuPath)]
        public static void Open()
        {
            SpellEditorWindow window = GetWindow<SpellEditorWindow>();
            window.titleContent = new GUIContent(SpellEditorConstants.WindowTitle);
            window.minSize = new Vector2(960, 540);
            window.Show();
        }

        private void OnEnable()
        {
            this.RefreshIndex();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                this.RefreshIndex();
            }

            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                SpellEditorAssetOperations.SaveAssets();
                this.RefreshIndex();
            }

            if (GUILayout.Button("导出配置", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SpellEditorAssetOperations.ExportConfig();
                this.RefreshIndex();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (this.assetIndex == null)
            {
                EditorGUILayout.HelpBox("资产索引未初始化。", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            this.DrawMainSpellList();
            this.DrawMainSpellResizeHandle();
            EditorGUILayout.BeginVertical();
            this.DrawAssetToolbar();

            this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
            foreach (SpellEditorIssue issue in this.issues)
            {
                EditorGUILayout.HelpBox(issue.Message, issue.Severity);
            }

            EditorGUILayout.LabelField($"资产索引：Spell {this.assetIndex.Spells.Count} / Buff {this.assetIndex.Buffs.Count}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"当前链路：Spell {this.buildResult?.Spells.Count ?? 0} / Buff {this.buildResult?.Buffs.Count ?? 0}", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("SpellConfig 表", EditorStyles.boldLabel);
            this.spellTableScroll = EditorGUILayout.BeginScrollView(this.spellTableScroll, GUILayout.Height(this.position.height * 0.42f));
            this.DrawSpellTable();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("BuffConfig 表", EditorStyles.boldLabel);
            this.buffTableScroll = EditorGUILayout.BeginScrollView(this.buffTableScroll, GUILayout.Height(this.position.height * 0.34f));
            this.DrawBuffTable();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (this.needsRebuild)
            {
                this.needsRebuild = false;
                this.RebuildGraph();
                this.Repaint();
            }
        }

        private void DrawMainSpellList()
        {
            if (this.assetIndex == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(this.mainSpellPanelWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("主技能", EditorStyles.boldLabel);
            this.mainSpellFilter = EditorGUILayout.TextField(this.mainSpellFilter ?? string.Empty, EditorStyles.toolbarSearchField);
            this.DrawCreateMainSpellControls();

            List<int> mainSpellIds = this.assetIndex.Spells.Keys
                    .Where(SpellEditorConstants.IsMainSpell)
                    .OrderBy(x => x)
                    .Where(this.MatchMainSpellFilter)
                    .ToList();

            EditorGUILayout.LabelField($"显示 {mainSpellIds.Count}", EditorStyles.miniLabel);
            this.mainSpellListScroll = EditorGUILayout.BeginScrollView(
                this.mainSpellListScroll,
                false,
                true,
                GUILayout.ExpandHeight(true));
            if (mainSpellIds.Count == 0)
            {
                EditorGUILayout.HelpBox("没有匹配的主技能。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (int mainSpellId in mainSpellIds)
            {
                this.assetIndex.Spells.TryGetValue(mainSpellId, out SpellScriptableObject asset);
                string label = this.FormatMainSpellLabel(mainSpellId, asset);
                GUIStyle style = new GUIStyle(mainSpellId == this.selectedMainSpellId ? EditorStyles.toolbarButton : EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true), GUILayout.Height(24)))
                {
                    if (mainSpellId != this.selectedMainSpellId)
                    {
                        this.selectedMainSpellId = mainSpellId;
                        this.copyMainSpellId = SpellEditorAssetOperations.FindNextMainSpellId(this.assetIndex, this.selectedMainSpellId);
                        this.SelectAsset(null);
                        this.RebuildGraph();
                    }

                    if (asset != null)
                    {
                        Selection.activeObject = asset;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCreateMainSpellControls()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("新主Id", EditorStyles.miniLabel, GUILayout.Width(44));
            this.newMainSpellId = EditorGUILayout.IntField(this.newMainSpellId, GUILayout.MinWidth(70));
            if (GUILayout.Button("新建", EditorStyles.miniButton, GUILayout.Width(48)))
            {
                this.CreateMainSpell();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMainSpellResizeHandle()
        {
            Rect rect = GUILayoutUtility.GetRect(5f, 5f, GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                this.resizingMainSpellPanel = true;
                evt.Use();
            }

            if (this.resizingMainSpellPanel && evt.type == EventType.MouseDrag)
            {
                this.mainSpellPanelWidth = Mathf.Clamp(this.mainSpellPanelWidth + evt.delta.x, 180f, 520f);
                this.Repaint();
                evt.Use();
            }

            if (this.resizingMainSpellPanel && evt.type == EventType.MouseUp)
            {
                this.resizingMainSpellPanel = false;
                evt.Use();
            }
        }

        private bool MatchMainSpellFilter(int mainSpellId)
        {
            if (string.IsNullOrWhiteSpace(this.mainSpellFilter))
            {
                return true;
            }

            if (!this.assetIndex.Spells.TryGetValue(mainSpellId, out SpellScriptableObject asset) || asset == null)
            {
                return mainSpellId.ToString().Contains(this.mainSpellFilter.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            string filter = this.mainSpellFilter.Trim();
            SpellConfig config = asset.SpellConfig;
            return mainSpellId.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrEmpty(config.Desc) && config.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(config.IconName) && config.IconName.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(asset.name) && asset.name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        private string FormatMainSpellLabel(int mainSpellId, SpellScriptableObject asset)
        {
            string desc = asset?.SpellConfig.Desc;
            return string.IsNullOrEmpty(desc) ? mainSpellId.ToString() : $"{mainSpellId}  {desc}";
        }

        private void RefreshIndex()
        {
            this.assetIndex = SpellEditorAssetIndex.Build();
            int firstMain = this.assetIndex.Spells.Keys.Where(SpellEditorConstants.IsMainSpell).OrderBy(x => x).FirstOrDefault();
            if (this.selectedMainSpellId == 0 || !this.assetIndex.Spells.ContainsKey(this.selectedMainSpellId))
            {
                this.selectedMainSpellId = firstMain;
            }

            this.EnsureSelectionValid();
            if (this.copyMainSpellId == 0 || !SpellEditorAssetOperations.IsSpellCreationIdAvailable(this.assetIndex, this.copyMainSpellId))
            {
                this.copyMainSpellId = SpellEditorAssetOperations.FindNextMainSpellId(this.assetIndex, this.selectedMainSpellId);
            }

            if (this.newMainSpellId == 0 || !SpellEditorAssetOperations.IsSpellCreationIdAvailable(this.assetIndex, this.newMainSpellId))
            {
                this.newMainSpellId = SpellEditorAssetOperations.FindNextMainSpellId(this.assetIndex, this.selectedMainSpellId);
            }

            this.RebuildGraph();
            this.Repaint();
        }

        private void RebuildGraph()
        {
            this.buildResult = this.assetIndex != null && this.selectedMainSpellId != 0
                    ? SpellEditorGraphBuilder.Build(this.assetIndex, this.selectedMainSpellId)
                    : new SpellEditorBuildResult();
            this.issues = this.assetIndex != null && this.buildResult != null
                    ? SpellEditorValidator.Validate(this.assetIndex, this.buildResult)
                    : new List<SpellEditorIssue>();
        }

        private void DrawAssetToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            bool hasMain = this.assetIndex.Spells.TryGetValue(this.selectedMainSpellId, out SpellScriptableObject mainAsset);
            if (GUILayout.Button("新建子技能", EditorStyles.toolbarButton, GUILayout.Width(88)) && hasMain)
            {
                int spellId = SpellEditorAssetOperations.FindNextSubSpellId(this.assetIndex, this.selectedMainSpellId);
                if (spellId == 0)
                {
                    EditorUtility.DisplayDialog("新建失败", $"主技能组 {this.selectedMainSpellId} 没有空闲子技能 Id。", "确定");
                }
                else
                {
                    string directory = SpellEditorAssetOperations.GetAssetDirectory(mainAsset);
                    SpellEditorAssetOperations.CreateSpellAsset(new SpellConfig { Id = spellId }, directory);
                    SpellEditorAssetOperations.CreateBuffAsset(spellId, directory);
                    this.RefreshIndex();
                }
            }

            GUILayout.Space(8);
            if (this.selectedAsset == null)
            {
                GUILayout.Label("未选择资产", EditorStyles.miniLabel, GUILayout.Width(96));
            }
            else
            {
                GUILayout.Label($"选中 {this.selectedAsset.name}", EditorStyles.miniLabel, GUILayout.Width(120));
                GUILayout.Label("目标Id", EditorStyles.miniLabel, GUILayout.Width(44));
                this.renameId = EditorGUILayout.IntField(this.renameId, GUILayout.Width(80));
                if (GUILayout.Button("改Id", EditorStyles.toolbarButton, GUILayout.Width(56)))
                {
                    int oldId = SpellEditorAssetOperations.GetAssetId(this.selectedAsset);
                    bool wasMain = this.selectedAsset is SpellScriptableObject && oldId == this.selectedMainSpellId;
                    if (SpellEditorAssetOperations.RenameAssetId(this.assetIndex, this.selectedAsset, this.renameId))
                    {
                        if (wasMain)
                        {
                            this.selectedMainSpellId = this.renameId;
                        }

                        this.RefreshIndex();
                    }
                }

                if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(56)))
                {
                    string path = AssetDatabase.GetAssetPath(this.selectedAsset);
                    if (EditorUtility.DisplayDialog("删除资产", $"确认删除 {path}？", "删除", "取消"))
                    {
                        SpellEditorAssetOperations.DeleteAsset(this.selectedAsset);
                        this.SelectAsset(null);
                        this.RefreshIndex();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("复制链到主Id", EditorStyles.miniLabel, GUILayout.Width(84));
            this.copyMainSpellId = EditorGUILayout.IntField(this.copyMainSpellId, GUILayout.Width(80));
            if (GUILayout.Button("复制链", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                if (SpellEditorAssetOperations.CopySpellChain(this.assetIndex, this.buildResult, this.selectedMainSpellId, this.copyMainSpellId))
                {
                    this.selectedMainSpellId = this.copyMainSpellId;
                    this.SelectAsset(null);
                    this.RefreshIndex();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpellTable()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("选", GUILayout.Width(24));
            GUILayout.Label("Id", GUILayout.Width(70));
            GUILayout.Label("类型", GUILayout.Width(60));
            GUILayout.Label("Desc", GUILayout.Width(180));
            GUILayout.Label("IconName", GUILayout.Width(280));
            GUILayout.Label("CD", GUILayout.Width(70));
            GUILayout.Label("Damage", GUILayout.Width(80));
            GUILayout.Label("Cost", GUILayout.Width(160));
            GUILayout.Label("TargetSelector", GUILayout.Width(180));
            GUILayout.Label("资产", GUILayout.Width(360));
            GUILayout.Label("来源", GUILayout.Width(240));
            EditorGUILayout.EndHorizontal();

            if (this.buildResult == null)
            {
                return;
            }

            foreach (SpellEditorSpellRow row in this.buildResult.Spells.ToArray())
            {
                EditorGUILayout.BeginHorizontal();
                if (row.Asset == null)
                {
                    GUILayout.Label(string.Empty, GUILayout.Width(24));
                    GUILayout.Label(row.Id.ToString(), GUILayout.Width(70));
                    GUILayout.Label("Missing", GUILayout.Width(60));
                    if (GUILayout.Button($"创建 Spell {row.Id}", EditorStyles.miniButton, GUILayout.Width(180)))
                    {
                        this.CreateMissingSpell(row.Id);
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }

                    GUILayout.Label(string.Empty, GUILayout.Width(280));
                    GUILayout.Label(string.Empty, GUILayout.Width(70));
                    GUILayout.Label(string.Empty, GUILayout.Width(80));
                    GUILayout.Label(string.Empty, GUILayout.Width(160));
                    GUILayout.Label(string.Empty, GUILayout.Width(180));
                    GUILayout.Label(string.Empty, GUILayout.Width(360));
                    GUILayout.Label(this.FormatSources(row.Sources), GUILayout.Width(240));
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                SpellConfig config = row.Asset.SpellConfig;
                this.DrawSelectionToggle(row.Asset);
                GUILayout.Label(config.Id.ToString(), GUILayout.Width(70));
                GUILayout.Label(row.IsMain ? "主技能" : "子技能", GUILayout.Width(60));
                EditorGUI.BeginChangeCheck();
                string desc = EditorGUILayout.TextField(config.Desc ?? string.Empty, GUILayout.Width(180));
                if (EditorGUI.EndChangeCheck())
                {
                    config.Desc = string.IsNullOrEmpty(desc) ? null : desc;
                    EditorUtility.SetDirty(row.Asset);
                }

                this.DrawIconNameCell(row.Asset, config);

                EditorGUI.BeginChangeCheck();
                int cd = EditorGUILayout.IntField(config.CD, GUILayout.Width(70));
                int damage = EditorGUILayout.IntField(config.DamageMultiplier, GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                {
                    config.CD = cd;
                    config.DamageMultiplier = damage;
                    EditorUtility.SetDirty(row.Asset);
                    this.needsRebuild = true;
                }

                this.DrawRootCell(row.Asset, config.Cost, "Cost", 160, this.GetCostNodeTypes(), root => config.Cost = (CostNode)root);
                this.DrawRootCell(row.Asset, config.TargetSelector, "TargetSelector", 180, this.GetTargetSelectorTypes(), root => config.TargetSelector = (TargetSelector)root);

                this.DrawAssetField(row.Asset, GUILayout.Width(360));
                GUILayout.Label(this.FormatSources(row.Sources), GUILayout.Width(240));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIconNameCell(SpellScriptableObject asset, SpellConfig config)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(280));
            Sprite currentSprite = this.ResolveIconSprite(config.IconName);
            EditorGUI.BeginChangeCheck();
            Sprite nextSprite = (Sprite)EditorGUILayout.ObjectField(currentSprite, typeof(Sprite), false, GUILayout.Width(130));
            if (EditorGUI.EndChangeCheck())
            {
                config.IconName = nextSprite != null ? nextSprite.name : null;
                EditorUtility.SetDirty(asset);
                this.Repaint();
            }

            EditorGUI.BeginChangeCheck();
            string iconName = EditorGUILayout.TextField(config.IconName ?? string.Empty, GUILayout.Width(112));
            if (EditorGUI.EndChangeCheck())
            {
                config.IconName = string.IsNullOrEmpty(iconName) ? null : iconName;
                EditorUtility.SetDirty(asset);
                this.Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private Sprite ResolveIconSprite(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return null;
            }

            this.EnsureIconSpriteIndex();
            return this.iconSpritesByName.TryGetValue(iconName, out Sprite sprite) ? sprite : null;
        }

        private void EnsureIconSpriteIndex()
        {
            if (this.iconSpritesByName != null)
            {
                return;
            }

            this.iconSpritesByName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    if (sprite != null && !this.iconSpritesByName.ContainsKey(sprite.name))
                    {
                        this.iconSpritesByName.Add(sprite.name, sprite);
                    }
                }

                Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (mainSprite != null && !this.iconSpritesByName.ContainsKey(mainSprite.name))
                {
                    this.iconSpritesByName.Add(mainSprite.name, mainSprite);
                }
            }
        }

        private void DrawRootCell(UnityEngine.Object asset, BTRoot root, string label, float width, Type[] candidateTypes, Action<BTRoot> assignRoot)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            if (root == null)
            {
                if (GUILayout.Button($"创建 {label}", EditorStyles.miniButton, GUILayout.Width(width)))
                {
                    this.ShowCreateRootMenu(asset, candidateTypes, assignRoot);
                }

                EditorGUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button(this.FormatRoot(root), EditorStyles.miniButton, GUILayout.Width(width - 28)))
            {
                SpellEditorAssetOperations.OpenBehaviorTree(asset, root);
            }

            if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                if (EditorUtility.DisplayDialog("清空行为树", $"确认清空 {label}？", "清空", "取消"))
                {
                    assignRoot(null);
                    EditorUtility.SetDirty(asset);
                    this.needsRebuild = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowCreateRootMenu(UnityEngine.Object asset, Type[] candidateTypes, Action<BTRoot> assignRoot)
        {
            if (candidateTypes.Length == 0)
            {
                EditorUtility.DisplayDialog("创建失败", "没有可创建的行为树类型。", "确定");
                return;
            }

            GenericMenu menu = new();
            foreach (Type type in candidateTypes)
            {
                Type rootType = type;
                menu.AddItem(new GUIContent(rootType.Name), false, () =>
                {
                    if (Activator.CreateInstance(rootType) is not BTRoot root)
                    {
                        EditorUtility.DisplayDialog("创建失败", $"无法创建行为树类型：{rootType.Name}", "确定");
                        return;
                    }

                    assignRoot(root);
                    EditorUtility.SetDirty(asset);
                    this.needsRebuild = true;
                    this.Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private Type[] GetCostNodeTypes()
        {
            return this.costNodeTypes ??= this.GetRootTypes(typeof(CostNode));
        }

        private Type[] GetTargetSelectorTypes()
        {
            return this.targetSelectorTypes ??= this.GetRootTypes(typeof(TargetSelector));
        }

        private Type[] GetRootTypes(Type baseType)
        {
            List<Type> types = new();
            if (!baseType.IsAbstract && !baseType.ContainsGenericParameters)
            {
                types.Add(baseType);
            }

            types.AddRange(TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(type => type is { IsAbstract: false } && !type.ContainsGenericParameters));

            return types.OrderBy(type => type.Name).ToArray();
        }

        private void DrawBuffTable()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("选", GUILayout.Width(24));
            GUILayout.Label("Id", GUILayout.Width(70));
            GUILayout.Label("Desc", GUILayout.Width(180));
            GUILayout.Label("Duration", GUILayout.Width(80));
            GUILayout.Label("TickTime", GUILayout.Width(80));
            GUILayout.Label("MaxStack", GUILayout.Width(80));
            GUILayout.Label("Stack", GUILayout.Width(70));
            GUILayout.Label("Overlay", GUILayout.Width(120));
            GUILayout.Label("Notice", GUILayout.Width(120));
            GUILayout.Label("Flags", GUILayout.Width(220));
            GUILayout.Label("Effects", GUILayout.Width(360));
            GUILayout.Label("资产", GUILayout.Width(360));
            GUILayout.Label("来源", GUILayout.Width(240));
            EditorGUILayout.EndHorizontal();

            if (this.buildResult == null)
            {
                return;
            }

            foreach (SpellEditorBuffRow row in this.buildResult.Buffs.ToArray())
            {
                EditorGUILayout.BeginHorizontal();
                if (row.Asset == null)
                {
                    GUILayout.Label(string.Empty, GUILayout.Width(24));
                    GUILayout.Label(row.Id.ToString(), GUILayout.Width(70));
                    if (GUILayout.Button($"创建 Buff {row.Id}", EditorStyles.miniButton, GUILayout.Width(180)))
                    {
                        this.CreateMissingBuff(row.Id);
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }

                    GUILayout.Label(string.Empty, GUILayout.Width(80));
                    GUILayout.Label(string.Empty, GUILayout.Width(80));
                    GUILayout.Label(string.Empty, GUILayout.Width(80));
                    GUILayout.Label(string.Empty, GUILayout.Width(70));
                    GUILayout.Label(string.Empty, GUILayout.Width(120));
                    GUILayout.Label(string.Empty, GUILayout.Width(120));
                    GUILayout.Label(string.Empty, GUILayout.Width(220));
                    GUILayout.Label(string.Empty, GUILayout.Width(360));
                    GUILayout.Label(string.Empty, GUILayout.Width(360));
                    GUILayout.Label(this.FormatSources(row.Sources), GUILayout.Width(240));
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                BuffConfig config = row.Asset.BuffConfig;
                this.DrawSelectionToggle(row.Asset);
                GUILayout.Label(config.Id.ToString(), GUILayout.Width(70));
                EditorGUI.BeginChangeCheck();
                string desc = EditorGUILayout.TextField(config.Desc ?? string.Empty, GUILayout.Width(180));
                int duration = EditorGUILayout.IntField(config.Duration, GUILayout.Width(80));
                int tickTime = EditorGUILayout.IntField(config.TickTime, GUILayout.Width(80));
                int maxStack = EditorGUILayout.IntField(config.MaxStack, GUILayout.Width(80));
                int stack = EditorGUILayout.IntField(config.Stack, GUILayout.Width(70));
                OverLayRuleType overlay = (OverLayRuleType)EditorGUILayout.EnumPopup(config.OverLayRuleType, GUILayout.Width(120));
                NoticeType notice = (NoticeType)EditorGUILayout.EnumPopup(config.NoticeType, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    config.Desc = string.IsNullOrEmpty(desc) ? null : desc;
                    config.Duration = duration;
                    config.TickTime = tickTime;
                    config.MaxStack = maxStack;
                    config.Stack = stack;
                    config.OverLayRuleType = overlay;
                    config.NoticeType = notice;
                    EditorUtility.SetDirty(row.Asset);
                    this.needsRebuild = true;
                }

                this.DrawFlagsCell(row.Asset, config);
                this.DrawEffectsCell(row.Asset, config);
                this.DrawAssetField(row.Asset, GUILayout.Width(360));
                GUILayout.Label(this.FormatSources(row.Sources), GUILayout.Width(240));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAssetField(UnityEngine.Object asset, params GUILayoutOption[] options)
        {
            EditorGUILayout.ObjectField(asset, asset != null ? asset.GetType() : typeof(UnityEngine.Object), false, options);
        }

        private void DrawFlagsCell(BuffScriptableObject asset, BuffConfig config)
        {
            string label = string.IsNullOrEmpty(this.FormatFlags(config)) ? "无 Flags" : this.FormatFlags(config);
            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(220)))
            {
                this.ShowFlagsMenu(asset, config);
            }
        }

        private void ShowFlagsMenu(BuffScriptableObject asset, BuffConfig config)
        {
            config.Flags ??= new HashSet<BuffFlags>();
            GenericMenu menu = new();
            foreach (BuffFlags flag in Enum.GetValues(typeof(BuffFlags)).Cast<BuffFlags>().OrderBy(x => (int)x))
            {
                if (flag == BuffFlags.None)
                {
                    continue;
                }

                BuffFlags currentFlag = flag;
                bool selected = config.Flags.Contains(currentFlag);
                menu.AddItem(new GUIContent(currentFlag.ToString()), selected, () => this.ToggleFlag(asset, config, currentFlag));
            }

            menu.AddSeparator(string.Empty);
            if (config.Flags.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("清空"));
            }
            else
            {
                menu.AddItem(new GUIContent("清空"), false, () =>
                {
                    config.Flags.Clear();
                    EditorUtility.SetDirty(asset);
                    this.Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private void ToggleFlag(BuffScriptableObject asset, BuffConfig config, BuffFlags flag)
        {
            config.Flags ??= new HashSet<BuffFlags>();
            if (!config.Flags.Add(flag))
            {
                config.Flags.Remove(flag);
            }

            EditorUtility.SetDirty(asset);
            this.Repaint();
        }

        private void DrawEffectsCell(BuffScriptableObject asset, BuffConfig config)
        {
            config.Effects ??= new List<EffectNode>();
            string effectsLabel = this.FormatEffects(config);
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(effectsLabel), EditorStyles.popup, GUILayout.Width(360), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUI.Button(rect, effectsLabel, EditorStyles.popup))
            {
                PopupWindow.Show(
                    rect,
                    new SpellEditorEffectsPopupContent(
                        config,
                        this.GetEffectNodeTypes(),
                        effectType => this.AddEffect(asset, config, effectType),
                        effect => SpellEditorAssetOperations.OpenBehaviorTree(asset, effect),
                        effectIndex => this.DeleteEffect(asset, config, effectIndex)));
            }
        }

        private Type[] GetEffectNodeTypes()
        {
            return this.effectNodeTypes ??= TypeCache.GetTypesDerivedFrom<EffectNode>()
                    .Where(type => type is { IsAbstract: false } && !type.ContainsGenericParameters)
                    .OrderBy(type => type.Name)
                    .ToArray();
        }

        private void AddEffect(BuffScriptableObject asset, BuffConfig config, Type effectType)
        {
            if (Activator.CreateInstance(effectType) is not EffectNode effect)
            {
                EditorUtility.DisplayDialog("添加失败", $"无法创建 EffectNode: {effectType.Name}", "确定");
                return;
            }

            config.Effects ??= new List<EffectNode>();
            config.Effects.Add(effect);
            config.effectDict = null;
            EditorUtility.SetDirty(asset);
            this.needsRebuild = true;
            this.Repaint();
        }

        private void DeleteEffect(BuffScriptableObject asset, BuffConfig config, int effectIndex)
        {
            if (config.Effects == null || effectIndex < 0 || effectIndex >= config.Effects.Count)
            {
                return;
            }

            config.Effects.RemoveAt(effectIndex);
            config.effectDict = null;
            EditorUtility.SetDirty(asset);
            this.needsRebuild = true;
            this.Repaint();
        }

        private void DrawSelectionToggle(UnityEngine.Object asset)
        {
            bool selected = this.selectedAsset == asset;
            bool next = GUILayout.Toggle(selected, string.Empty, GUILayout.Width(24));
            if (next != selected)
            {
                this.SelectAsset(next ? asset : null);
            }
        }

        private void SelectAsset(UnityEngine.Object asset)
        {
            this.selectedAsset = asset;
            this.renameId = SpellEditorAssetOperations.GetAssetId(asset);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void EnsureSelectionValid()
        {
            if (this.selectedAsset == null)
            {
                this.renameId = 0;
                return;
            }

            string path = AssetDatabase.GetAssetPath(this.selectedAsset);
            if (string.IsNullOrEmpty(path))
            {
                this.SelectAsset(null);
            }
        }

        private void CreateMissingSpell(int spellId)
        {
            if (!this.assetIndex.Spells.TryGetValue(this.selectedMainSpellId, out SpellScriptableObject mainAsset))
            {
                EditorUtility.DisplayDialog("创建失败", "当前主技能资产不存在。", "确定");
                return;
            }

            string directory = SpellEditorAssetOperations.GetAssetDirectory(mainAsset);
            SpellEditorAssetOperations.CreateSpellAsset(spellId, directory);
            this.RefreshIndex();
        }

        private void CreateMissingBuff(int buffId)
        {
            if (!this.assetIndex.Spells.TryGetValue(this.selectedMainSpellId, out SpellScriptableObject mainAsset))
            {
                EditorUtility.DisplayDialog("创建失败", "当前主技能资产不存在。", "确定");
                return;
            }

            string directory = SpellEditorAssetOperations.GetAssetDirectory(mainAsset);
            SpellEditorAssetOperations.CreateBuffAsset(buffId, directory);
            this.RefreshIndex();
        }

        private void CreateMainSpell()
        {
            if (this.newMainSpellId <= 0)
            {
                EditorUtility.DisplayDialog("新建失败", "主技能 Id 必须大于 0。", "确定");
                return;
            }

            if (!SpellEditorConstants.IsMainSpell(this.newMainSpellId))
            {
                EditorUtility.DisplayDialog("新建失败", "主技能 Id 必须是 10 的倍数。", "确定");
                return;
            }

            if (this.assetIndex.Spells.ContainsKey(this.newMainSpellId))
            {
                EditorUtility.DisplayDialog("新建失败", $"主技能 {this.newMainSpellId} 已存在。", "确定");
                return;
            }

            if (this.assetIndex.Buffs.ContainsKey(this.newMainSpellId))
            {
                EditorUtility.DisplayDialog("新建失败", $"同 Id Buff {this.newMainSpellId} 已存在，无法创建主技能。", "确定");
                return;
            }

            int mainSpellId = this.newMainSpellId;
            string directory = SpellEditorAssetOperations.GetNewMainSpellDirectory(mainSpellId);
            SpellScriptableObject asset = SpellEditorAssetOperations.CreateSpellAsset(new SpellConfig { Id = mainSpellId }, directory);
            if (asset == null)
            {
                return;
            }

            SpellEditorAssetOperations.CreateBuffAsset(mainSpellId, directory);
            this.selectedMainSpellId = mainSpellId;
            this.SelectAsset(asset);
            Selection.activeObject = asset;
            this.RefreshIndex();
        }

        private string FormatRoot(BTRoot root)
        {
            if (root == null)
            {
                return string.Empty;
            }

            int nodeCount = CountNodes(root);
            return $"{root.GetType().Name} ({nodeCount})";
        }

        private string FormatEffects(BuffConfig config)
        {
            if (config.Effects == null || config.Effects.Count == 0)
            {
                return "Effects: 无";
            }

            return $"Effects ({config.Effects.Count}): {string.Join(", ", config.Effects.Select(x => x != null ? x.GetType().Name : "null"))}";
        }

        private string FormatFlags(BuffConfig config)
        {
            return config.Flags == null || config.Flags.Count == 0
                    ? string.Empty
                    : string.Join(", ", config.Flags.OrderBy(x => (int)x).Select(x => x.ToString()));
        }

        private string FormatSources(List<SpellEditorSourceInfo> sources)
        {
            return sources == null || sources.Count == 0 ? string.Empty : string.Join("; ", sources.Select(x => x.ToString()));
        }

        private static int CountNodes(BTNode node)
        {
            if (node == null)
            {
                return 0;
            }

            int count = 1;
            if (node.Children == null)
            {
                return count;
            }

            foreach (BTNode child in node.Children)
            {
                count += CountNodes(child);
            }

            return count;
        }
    }
}
