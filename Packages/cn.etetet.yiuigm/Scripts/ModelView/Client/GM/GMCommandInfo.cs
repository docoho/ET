using System;
using System.Collections.Generic;

namespace ET.Client
{
    //GM实例信息
    /// <summary>
    /// 单条GM命令的运行时描述与实例封装。
    /// </summary>
    [EnableClass]
    public class GMCommandInfo
    {
        public int               GMType;        //命令类型
        public string            GMTypeName;    //命令名称
        public int               GMLevel;       //命令等级
        public string            GMName;        //命令名称
        public string            GMDesc;        //命令描述
        public List<GMParamInfo> ParamInfoList; //泛型参数类型
        public IGMCommand        Command;       //实例
    }
}