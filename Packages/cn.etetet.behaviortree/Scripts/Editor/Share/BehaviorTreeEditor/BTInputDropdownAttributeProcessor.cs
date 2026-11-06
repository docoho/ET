#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// [BTInput] 特性的 Odin 自定义属性绘制器，在行为树编辑器中收集上游节点提供的匹配类型的 [BTOutput] 键名并渲染为可搜索下拉框。
    /// </summary>
    public sealed class BTInputDropdownAttributeProcessor : OdinAttributeDrawer<BTInput, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!BTInputDropdownHelper.ShouldUseInputDropdown(this.Property, this.Attribute))
            {
                this.CallNextDrawer(label);
                return;
            }

            BTInputDropdownHelper.OptionResult optionResult = BTInputDropdownHelper.GetOptionsForProperty(this.Property, this.Attribute);
            List<BTInputDropdownHelper.OptionItem> options = optionResult.Options;

            string currentValue = this.ValueEntry.SmartValue;
            string displayText = BTInputDropdownHelper.GetDisplayText(currentValue, this.Attribute.Type, options);

            Rect rect = EditorGUILayout.GetControlRect();
            Rect contentRect = EditorGUI.PrefixLabel(rect, label);
            GUIContent content = string.IsNullOrEmpty(displayText) ? new GUIContent("请选择") : new GUIContent(displayText);

            if (!EditorGUI.DropdownButton(contentRect, content, FocusType.Keyboard))
            {
                return;
            }

            string typeLabel = BTInputDropdownHelper.GetTypeLabel(this.Attribute.Type);
            List<string> values = options.Select(item => item.Value).ToList();
            string missingValue = BTInputDropdownHelper.GetMissingValue(currentValue, values);
            if (!string.IsNullOrEmpty(missingValue))
            {
                values.Add(missingValue);
            }

            bool hasOptions = values.Count > 0;
            if (!hasOptions)
            {
                string placeholder = $"(无可用输出，类型: {typeLabel})";
                values.Add(placeholder);
            }

            GenericSelector<string> selector = new("BTInput", false, value =>
            {
                if (!hasOptions && value.StartsWith("(无可用输出"))
                {
                    return value;
                }

                return BTInputDropdownHelper.GetDisplayText(value, this.Attribute.Type, options);
            }, values);

            if (hasOptions)
            {
                selector.SetSelection(currentValue);
            }

            selector.SelectionTree.Config.DrawSearchToolbar = true;
            selector.SelectionTree.Config.SearchToolbarHeight = 22;
            selector.SelectionConfirmed += selection =>
            {
                if (!hasOptions)
                {
                    return;
                }

                string selected = selection.FirstOrDefault();
                if (selected != null)
                {
                    this.ValueEntry.SmartValue = selected;
                }
            };

            selector.ShowInPopup();
        }
    }
}
#endif
