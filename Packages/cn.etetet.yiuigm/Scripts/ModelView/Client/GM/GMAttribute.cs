using System;

namespace ET.Client
{
    /// <summary>
    /// GM特性
    /// </summary>
    public class GMAttribute : BaseAttribute
    {
        public int    GMType;  //命令类型
        public int    GMLevel; //命令等级
        public string GMName;  //命令名称
        public string GMDesc;  //命令描述

        public GMAttribute(int gmType, int gmLevel, string gmName, string gmDesc = "")
        {
            GMType  = gmType;
            GMLevel = gmLevel;
            GMName  = gmName;
            GMDesc  = gmDesc;
        }
    }

    /// <summary>
    /// 标记在 EGMType 常量字段上的GM分组页签显示名称特性。
    /// </summary>
    [EnableClass]
    public class GMGroupAttribute : Attribute
    {
        public string Name;

        public GMGroupAttribute(string name)
        {
            Name = name;
        }
    }
}