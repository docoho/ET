using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// 行为树编辑器节点字段反射辅助类，用于提取节点上带有指定特性（如 [BTInput]）的字符串字段值。
    /// </summary>
    public static class NodeFieldHelper
    {
        public static List<string> GetInputs(NodeView node, System.Type attribute)
        {
            List<string> inputs = new List<string>();
            FieldInfo[] fieldInfos = node.Node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btInputFields = fieldInfos.Where(field =>
                    field.GetCustomAttributes(attribute, false).Any());
            foreach (FieldInfo field in btInputFields)
            {
                string v = field.GetValue(node.Node) as string;
                inputs.Add(v);
            }
            return inputs;
        }
    }
}