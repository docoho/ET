#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// [BTIntDropdown] 特性的 Odin 自定义属性绘制器，通过反射读取枚举单例中的键值映射并提供带搜索栏的下拉选择器。
    /// </summary>
    public sealed class BTIntDropdownAttributeDrawer : OdinAttributeDrawer<BTIntDropdownAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!BTNodeDrawer.IsDrawingInBehaviorTreeEditor)
            {
                this.CallNextDrawer(label);
                return;
            }

            List<OptionItem> options = GetOptions(this.Attribute?.EnumSingletonType);
            int currentValue = this.ValueEntry.SmartValue;

            Rect rect = EditorGUILayout.GetControlRect();
            Rect contentRect = EditorGUI.PrefixLabel(rect, label);
            GUIContent content = new(GetDisplayText(currentValue, options));

            if (!EditorGUI.DropdownButton(contentRect, content, FocusType.Keyboard))
            {
                return;
            }

            if (options.Count == 0)
            {
                return;
            }

            List<int> values = options.Select(option => option.Value).ToList();
            GenericSelector<int> selector = new("BTIntDropdown", false, value => GetDisplayText(value, options), values);

            selector.SetSelection(currentValue);
            selector.SelectionTree.Config.DrawSearchToolbar = true;
            selector.SelectionTree.Config.SearchToolbarHeight = 22;
            selector.SelectionConfirmed += selection =>
            {
                int selected = selection.FirstOrDefault();
                this.ValueEntry.SmartValue = selected;
            };

            selector.ShowInPopup();
        }

        private static List<OptionItem> GetOptions(Type enumSingletonType)
        {
            List<OptionItem> options = new();
            if (enumSingletonType == null)
            {
                return options;
            }

            EnsureEnumSingletonReady(enumSingletonType);

            MethodInfo getOptionsMethod = typeof(BTViewHelper).GetMethod("GetOptions", BindingFlags.Public | BindingFlags.Static);
            if (getOptionsMethod == null)
            {
                return options;
            }

            IEnumerable rawOptions;
            try
            {
                MethodInfo concreteMethod = getOptionsMethod.MakeGenericMethod(enumSingletonType);
                rawOptions = concreteMethod.Invoke(null, null) as IEnumerable;
            }
            catch
            {
                rawOptions = null;
            }

            if (rawOptions == null)
            {
                return GetOptionsFromEnumSingleton(enumSingletonType);
            }

            foreach (object option in rawOptions)
            {
                if (!TryReadValue(option, out int value))
                {
                    continue;
                }

                string text = TryReadText(option) ?? value.ToString();
                options.Add(new OptionItem(value, text));
            }

            return options;
        }

        private static void EnsureEnumSingletonReady(Type enumSingletonType)
        {
            if (enumSingletonType == null)
            {
                return;
            }

            try
            {
                PropertyInfo instanceProperty = enumSingletonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                object singleton = instanceProperty?.GetValue(null);
                if (singleton != null)
                {
                    return;
                }

                if (!typeof(ASingleton).IsAssignableFrom(enumSingletonType))
                {
                    return;
                }

                ASingleton created = Activator.CreateInstance(enumSingletonType) as ASingleton;
                if (created == null)
                {
                    return;
                }

                MethodInfo awakeMethod = enumSingletonType.GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                awakeMethod?.Invoke(created, null);

                World.Instance.AddSingleton(created);
            }
            catch
            {
                // 兜底逻辑失败时，保留原有流程，不中断 Inspector 绘制。
            }
        }

        private static List<OptionItem> GetOptionsFromEnumSingleton(Type enumSingletonType)
        {
            List<OptionItem> options = new();
            if (enumSingletonType == null)
            {
                return options;
            }

            PropertyInfo instanceProperty = enumSingletonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            object singleton = instanceProperty?.GetValue(null);
            if (singleton == null)
            {
                return options;
            }

            MethodInfo getAllValuesMethod = enumSingletonType.GetMethod("GetAllValues", BindingFlags.Public | BindingFlags.Instance);
            object doubleMap = getAllValuesMethod?.Invoke(singleton, null);
            if (doubleMap == null)
            {
                return options;
            }

            MethodInfo getAllMethod = doubleMap.GetType().GetMethod("GetAll", BindingFlags.Public | BindingFlags.Instance);
            object all = getAllMethod?.Invoke(doubleMap, null);
            if (all is not IEnumerable enumerable)
            {
                return options;
            }

            foreach (object pair in enumerable)
            {
                if (!TryReadValueFromPair(pair, out int value))
                {
                    continue;
                }

                string name = TryReadNameFromPair(pair) ?? value.ToString();
                options.Add(new OptionItem(value, $"{name} ({value})"));
            }

            options.Sort((a, b) => a.Value.CompareTo(b.Value));
            return options;
        }

        private static bool TryReadValueFromPair(object pair, out int value)
        {
            value = default;
            if (pair == null)
            {
                return false;
            }

            PropertyInfo keyProperty = pair.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            if (keyProperty == null)
            {
                return false;
            }

            object key = keyProperty.GetValue(pair);
            if (key == null)
            {
                return false;
            }

            value = Convert.ToInt32(key);
            return true;
        }

        private static string TryReadNameFromPair(object pair)
        {
            if (pair == null)
            {
                return null;
            }

            PropertyInfo valueProperty = pair.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty == null)
            {
                return null;
            }

            object name = valueProperty.GetValue(pair);
            return name?.ToString();
        }

        private static bool TryReadValue(object option, out int value)
        {
            value = default;
            if (option == null)
            {
                return false;
            }

            PropertyInfo valueProperty = option.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty != null)
            {
                object rawValue = valueProperty.GetValue(option);
                if (rawValue == null)
                {
                    return false;
                }

                value = Convert.ToInt32(rawValue);
                return true;
            }

            FieldInfo valueField = option.GetType().GetField("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueField == null)
            {
                return false;
            }

            object fieldValue = valueField.GetValue(option);
            if (fieldValue == null)
            {
                return false;
            }

            value = Convert.ToInt32(fieldValue);
            return true;
        }

        private static string TryReadText(object option)
        {
            if (option == null)
            {
                return null;
            }

            PropertyInfo textProperty = option.GetType().GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
            if (textProperty != null)
            {
                object textValue = textProperty.GetValue(option);
                if (textValue != null)
                {
                    return textValue.ToString();
                }
            }

            PropertyInfo nameProperty = option.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            if (nameProperty != null)
            {
                object nameValue = nameProperty.GetValue(option);
                if (nameValue != null)
                {
                    return nameValue.ToString();
                }
            }

            FieldInfo textField = option.GetType().GetField("Text", BindingFlags.Public | BindingFlags.Instance);
            if (textField != null)
            {
                object textValue = textField.GetValue(option);
                if (textValue != null)
                {
                    return textValue.ToString();
                }
            }

            FieldInfo nameField = option.GetType().GetField("Name", BindingFlags.Public | BindingFlags.Instance);
            if (nameField != null)
            {
                object nameValue = nameField.GetValue(option);
                if (nameValue != null)
                {
                    return nameValue.ToString();
                }
            }

            return null;
        }

        private static string GetDisplayText(int value, List<OptionItem> options)
        {
            OptionItem option = options.FirstOrDefault(item => item.Value == value);
            return option?.Text ?? value.ToString();
        }

        private sealed class OptionItem
        {
            public OptionItem(int value, string text)
            {
                this.Value = value;
                this.Text = text;
            }

            public int Value { get; }
            public string Text { get; }
        }
    }
}
#endif
