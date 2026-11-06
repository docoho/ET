using System.Collections.Generic;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// GM命令单个参数输入项UI实体，维护参数信息及枚举下拉框选项映射字典。
    /// </summary>
    public partial class GMParamItemComponent : Entity
    {
        public GMParamInfo ParamInfo;
        public List<Dropdown.OptionData> OptionList;
        public Dictionary<string, string> OptionDic;
    }
}