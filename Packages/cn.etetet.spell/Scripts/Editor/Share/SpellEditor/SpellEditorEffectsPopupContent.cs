using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 技能编辑器中用于管理单个 BuffConfig.Effects 列表的弹出窗口内容（支持新增、打开行为树或删除 EffectNode）。
    /// </summary>
    internal sealed class SpellEditorEffectsPopupContent: PopupWindowContent
    {
        private readonly BuffConfig config;
        private readonly Type[] effectNodeTypes;
        private readonly Action<Type> addEffect;
        private readonly Action<EffectNode> openEffect;
        private readonly Action<int> deleteEffect;
        private Vector2 scroll;

        public SpellEditorEffectsPopupContent(
            BuffConfig config,
            Type[] effectNodeTypes,
            Action<Type> addEffect,
            Action<EffectNode> openEffect,
            Action<int> deleteEffect)
        {
            this.config = config;
            this.effectNodeTypes = effectNodeTypes;
            this.addEffect = addEffect;
            this.openEffect = openEffect;
            this.deleteEffect = deleteEffect;
        }

        public override Vector2 GetWindowSize()
        {
            int effectCount = this.config.Effects?.Count ?? 0;
            float height = 48f + Mathf.Max(1, effectCount) * 28f + 12f;
            return new Vector2(380f, Mathf.Clamp(height, 120f, 360f));
        }

        public override void OnGUI(Rect rect)
        {
            this.config.Effects ??= new List<EffectNode>();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Effects", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加 Effect", EditorStyles.toolbarButton, GUILayout.Width(104)))
            {
                this.ShowAddEffectMenu();
            }

            EditorGUILayout.EndHorizontal();

            if (this.config.Effects.Count == 0)
            {
                EditorGUILayout.HelpBox("当前 Buff 没有 Effect。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
            for (int i = 0; i < this.config.Effects.Count; i++)
            {
                this.DrawEffectRow(i, this.config.Effects[i]);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEffectRow(int effectIndex, EffectNode effect)
        {
            string effectName = effect != null ? effect.GetType().Name : "null";
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{effectIndex}: {effectName}", GUILayout.Width(230));
            using (new EditorGUI.DisabledScope(effect == null))
            {
                if (GUILayout.Button("打开", EditorStyles.miniButton, GUILayout.Width(52)))
                {
                    this.openEffect(effect);
                }
            }

            if (GUILayout.Button("删除", EditorStyles.miniButton, GUILayout.Width(52)))
            {
                if (EditorUtility.DisplayDialog("删除 Effect", $"确认删除 Effects[{effectIndex}] {effectName}？", "删除", "取消"))
                {
                    this.deleteEffect(effectIndex);
                    this.editorWindow?.Close();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddEffectMenu()
        {
            GenericMenu menu = new();
            HashSet<Type> existingTypes = this.config.Effects
                    .Where(effect => effect != null)
                    .Select(effect => effect.GetType())
                    .ToHashSet();

            foreach (Type type in this.effectNodeTypes)
            {
                Type effectType = type;
                GUIContent content = new(effectType.Name);
                if (existingTypes.Contains(effectType))
                {
                    menu.AddDisabledItem(content);
                    continue;
                }

                menu.AddItem(content, false, () =>
                {
                    this.addEffect(effectType);
                    this.editorWindow?.Close();
                });
            }

            if (this.effectNodeTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有可添加的 Effect"));
            }

            menu.ShowAsContext();
        }
    }
}
