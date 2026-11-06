using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ET
{
    /// <summary>
    /// 单个网络消息与协议号（Opcode）的映射记录。
    /// </summary>
    internal class OpcodeInfo
    {
        /// <summary>消息类名称。</summary>
        public string Name;
        /// <summary>分配的整数协议号。</summary>
        public int Opcode;
    }

    /// <summary>
    /// Proto 协议文件转 C# 代码对外门面类。
    /// </summary>
    public static class Proto2CS
    {
        /// <summary>
        /// 导出指定包集合中的所有 <c>.proto</c> 协议文件为 C# 消息类。
        /// </summary>
        /// <param name="packages">目标包名集合（来自 <c>MainPackage.txt</c>）。</param>
        public static void Export(HashSet<string> packages)
        {
            InnerProto2CS.Proto2CS(packages);
        }
    }

    /// <summary>
    /// Proto 协议解析与 C# 代码生成核心实现类。
    /// 扫描目标包下 <c>Proto/</c> 目录中的 <c>{ProtoName}_{C|S|CS}_{StartOpcode}.proto</c> 文件，
    /// 解析 message 定义、基类接口注释（如 <c>// IRequest</c>）、<c>// ResponseType</c> 注解以及字段类型，
    /// 生成带有 <c>[MemoryPackable]</c>、<c>[Message]</c>、对象池复用（<c>Create</c>/<c>Dispose</c>）及 <see cref="Opcode"/> 常量的 C# 代码到 <c>cn.etetet.proto/CodeMode/Model/{Client|Server|ClientServer}</c>。
    /// </summary>
    public static partial class InnerProto2CS
    {
        private static string clientMessagePath;
        private static string serverMessagePath;
        private static string clientServerMessagePath;
        private static readonly char[] splitChars = [' ', '\t'];
        private static readonly List<OpcodeInfo> msgOpcode = [];

        /// <summary>
        /// 执行全量 Proto 扫描、清理旧生成文件并重新生成 C# 协议代码。
        /// </summary>
        /// <param name="targetPackages">参与导出的目标包集合。</param>
        public static void Proto2CS(HashSet<string> targetPackages)
        {
            // 强制调用一下mongo，避免mongo库被裁剪
            MongoHelper.ToJson(1);

            msgOpcode.Clear();

            // 直接扫描 Packages 目录获取包信息
            string packagesDir = Path.Combine("./", "Packages");
            string protoPackageDir = Path.Combine(packagesDir, "cn.etetet.proto");

            if (!Directory.Exists(protoPackageDir))
            {
                throw new Exception($"Proto package not found: {protoPackageDir}");
            }

            clientMessagePath = Path.Combine(protoPackageDir, "CodeMode/Model/Client");
            serverMessagePath = Path.Combine(protoPackageDir, "CodeMode/Model/Server");
            clientServerMessagePath = Path.Combine(protoPackageDir, "CodeMode/Model/ClientServer");

            Console.WriteLine($"Will export proto from {targetPackages.Count} packages:");
            foreach (var pkg in targetPackages.OrderBy(x => x))
            {
                Console.WriteLine($"  - {pkg}");
            }

            // 清理旧文件
            Console.WriteLine("Cleaning generated files...");
            CleanGeneratedFiles(clientMessagePath);
            CleanGeneratedFiles(serverMessagePath);
            CleanGeneratedFiles(clientServerMessagePath);

            List<(string, string)> list = new ();

            // 扫描所有 cn.etetet.* 包目录
            foreach (string packageDir in Directory.GetDirectories(packagesDir, "cn.etetet.*"))
            {
                string packageName = Path.GetFileName(packageDir);

                // 过滤包
                if (!targetPackages.Contains(packageName))
                {
                    continue;
                }

                string moduleName = packageName.Replace("cn.etetet.", "");
                string protoDir = Path.Combine(packageDir, "Proto");

                if (!Directory.Exists(protoDir))
                {
                    continue;
                }

                foreach (var f in FileHelper.GetAllFiles(protoDir, "*.proto"))
                {
                    list.Add((f, moduleName));
                }
            }

            // 按文件名约定 {protoName}_{C|S|CS}_{startOpcode}.proto 逐个转换
            foreach ((string s, string module) in list)
            {
                if (!s.EndsWith(".proto"))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(s);
                string[] ss2 = fileName.Split('_');
                string protoName = ss2[0];
                string cs = ss2[1];
                int startOpcode = int.Parse(ss2[2]);
                ProtoFile2CS(s, module, protoName, cs, startOpcode);
            }

            Console.WriteLine($"Generated {list.Count} proto files.");
        }

        /// <summary>
        /// 删除指定目录下的所有 .cs 文件
        /// </summary>
        private static void CleanGeneratedFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, "*.cs"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Warning: Failed to delete {file}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 将单个 <c>.proto</c> 文件解析并生成为 C# 源码文件，按 <paramref name="cs"/> 标志（包含 'C' 或 'S'）输出到对应的 CodeMode 目录。
        /// </summary>
        /// <param name="path"><c>.proto</c> 文件路径。</param>
        /// <param name="module">所属模块包简称。</param>
        /// <param name="protoName">协议名。</param>
        /// <param name="cs">端侧标识（"C" 表示客户端+服务端共享外网协议，"S" 表示仅服务端内部协议）。</param>
        /// <param name="startOpcode">该协议文件的起始 Opcode 编号。</param>
        private static void ProtoFile2CS(string path, string module, string protoName, string cs, int startOpcode)
        {
            msgOpcode.Clear();

            string proto = path;
            string s = File.ReadAllText(proto);

            StringBuilder sb = new();
            sb.Append("using MemoryPack;\n");
            sb.Append("using System.Collections.Generic;\n\n");
            sb.Append($"namespace ET\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            bool isPoolMessage = false;
            string msgName = "";
            string responseType = "";
            StringBuilder sbDispose = new();
            Regex responseTypeRegex = ResponseTypeRegex();
            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();
                if (string.IsNullOrEmpty(newline))
                {
                    continue;
                }

                // 解析 message 上方的 // ResponseType XxxResponse 注解
                if (responseTypeRegex.IsMatch(newline))
                {
                    responseType = responseTypeRegex.Replace(newline, string.Empty);
                    responseType = responseType.Trim().Split(' ')[0].TrimEnd('\r', '\n');
                    continue;
                }

                if (!isMsgStart && newline.StartsWith("//"))
                {
                    if (newline.StartsWith("///"))
                    {
                        sb.Append("\t/// <summary>\n");
                        sb.Append($"\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t/// </summary>\n");
                    }
                    else
                    {
                        sb.Append($"\t// {newline.TrimStart('/', ' ')}\n");
                    }

                    continue;
                }

                // 解析 message 声明行及其行尾注释中的接口继承列表（如 message C2G_Ping // ISessionRequest）
                if (newline.StartsWith("message"))
                {
                    isMsgStart = true;
                    isPoolMessage = false;

                    string parentClass = "";
                    msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(["//"], StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }

                    isPoolMessage = MessageImplements(parentClass, "IPool");

                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    sb.Append($"\t[MemoryPackable]\n");
                    sb.Append($"\t[Message(Opcode.{msgName})]\n");
                    if (!string.IsNullOrEmpty(responseType))
                    {
                        sb.Append($"\t[ResponseType(nameof({responseType}))]\n");
                    }

                    sb.Append($"\tpublic partial class {msgName} : MessageObject");

                    if (parentClass is "IActorMessage" or "IActorRequest" or "IActorResponse")
                    {
                        sb.Append($", {parentClass}\n");
                    }
                    else if (parentClass != "")
                    {
                        sb.Append($", {parentClass}\n");
                    }
                    else
                    {
                        sb.Append('\n');
                    }

                    continue;
                }

                if (isMsgStart)
                {
                    if (newline.StartsWith('{'))
                    {
                        sbDispose.Clear();
                        sb.Append("\t{\n");
                        sb.Append($"\t\tpublic static {msgName} Create(bool isFromPool = false)\n\t\t{{\n\t\t\treturn ObjectPool.Fetch<{msgName}>(isFromPool);\n\t\t}}\n\n");
                        if (isPoolMessage)
                        {
                            sb.Append("\t\tpublic bool IsFromPool { get; set; }\n\n");
                        }

                        continue;
                    }

                    if (newline.StartsWith('}'))
                    {
                        isMsgStart = false;
                        responseType = "";

                        // 加了no dispose则自己去定义dispose函数，不要自动生成
                        if (!newline.Contains("// no dispose"))
                        {
                            sb.Append($"\t\tpublic override void Dispose()\n\t\t{{\n\t\t\tObjectPool.Recycle(this);\n\t\t}}\n");
                        }

                        sb.Append("\t}\n\n");
                        continue;
                    }

                    if (newline.StartsWith("//"))
                    {
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t\t/// </summary>\n");
                        continue;
                    }

                    string memberStr;
                    if (newline.Contains("//"))
                    {
                        string[] lineSplit = newline.Split("//");
                        memberStr = newline;
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {lineSplit[1].Trim()}\n");
                        sb.Append("\t\t/// </summary>\n");
                    }
                    else
                    {
                        memberStr = newline;
                    }

                    if (memberStr.StartsWith("map<"))
                    {
                        Map(sb, memberStr, sbDispose);
                    }
                    else if (memberStr.StartsWith("repeated"))
                    {
                        Repeated(sb, memberStr, sbDispose);
                    }
                    else
                    {
                        Members(sb, memberStr, sbDispose);
                    }
                }
            }

            // 在文件末尾生成静态部分类 Opcode 常量定义
            sb.Append("\tpublic static partial class Opcode" + "\n\t{\n");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.Append($"\t\tpublic const ushort {info.Name} = {info.Opcode};\n");
            }

            sb.Append("\t}\n");

            sb.Append('}');

            sb.Replace("\t", "    ");
            string result = sb.ToString();

            if (cs.Contains('C'))
            {
                GenerateCS(result, clientMessagePath, proto);
                GenerateCS(result, serverMessagePath, proto);
                GenerateCS(result, clientServerMessagePath, proto);
            }

            if (cs.Contains('S'))
            {
                GenerateCS(result, serverMessagePath, proto);
                GenerateCS(result, clientServerMessagePath, proto);
            }
        }

        /// <summary>
        /// 将生成的 C# 源码字符串写入目标目录下的 <c>.cs</c> 文件。
        /// </summary>
        private static void GenerateCS(string result, string path, string proto)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string csPath = Path.Combine(path, Path.GetFileNameWithoutExtension(proto) + ".cs");
            using FileStream txt = new(csPath, FileMode.Create, FileAccess.ReadWrite);
            using StreamWriter sw = new(txt);
            sw.Write(result);
        }

        /// <summary>
        /// 检查父类/接口声明字符串中是否包含指定的接口名称。
        /// </summary>
        private static bool MessageImplements(string parentClass, string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(parentClass))
            {
                return false;
            }

            return parentClass.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Any(s => s == interfaceName || s.EndsWith($".{interfaceName}") || s.EndsWith($"::{interfaceName}"));
        }

        /// <summary>
        /// 解析 <c>map&lt;K, V&gt;</c> 语法并生成 C# <see cref="Dictionary{TKey,TValue}"/> 属性及 <c>[MemoryPackOrder]</c> 特性。
        /// </summary>
        private static void Map(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            int start = newline.IndexOf('<') + 1;
            int end = newline.IndexOf('>');
            string types = newline.Substring(start, end - start);
            string[] ss = types.Split(',');
            string keyType = ConvertType(ss[0].Trim());
            string valueType = ConvertType(ss[1].Trim());
            string tail = newline[(end + 1)..];
            ss = tail.Trim().Replace(";", "").Split(' ');
            string v = ss[0];
            int n = int.Parse(ss[2]);

            sb.Append("\t\t[MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]\n");
            sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
            sb.Append($"\t\tpublic Dictionary<{keyType}, {valueType}> {v} {{ get; set; }} = new();\n");

            sbDispose.Append($"this.{v}.Clear();\n\t\t\t");
        }

        /// <summary>
        /// 解析 <c>repeated T</c> 语法并生成 C# <see cref="List{T}"/> 属性及 <c>[MemoryPackOrder]</c> 特性。
        /// </summary>
        private static void Repeated(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[1];
                type = ConvertType(type);
                string name = ss[2];
                int n = int.Parse(ss[4]);

                sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
                sb.Append($"\t\tpublic List<{type}> {name} {{ get; set; }} = new();\n\n");

                sbDispose.Append($"this.{name}.Clear();\n\t\t\t");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        /// <summary>
        /// 将 Proto 基础数据类型名映射为 C# 数据类型关键字。
        /// </summary>
        private static string ConvertType(string type)
        {
            return type switch
            {
                "int16" => "short",
                "int32" => "int",
                "bytes" => "byte[]",
                "uint32" => "uint",
                "long" => "long",
                "int64" => "long",
                "uint64" => "ulong",
                "uint16" => "ushort",
                _ => type
            };
        }

        /// <summary>
        /// 解析普通标量或自定义结构体成员字段并生成 C# 属性及 <c>[MemoryPackOrder]</c> 特性。
        /// </summary>
        private static void Members(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                string oldline = newline;
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[0];
                string name = ss[1];
                int n = int.Parse(ss[3]);
                string typeCs = ConvertType(type);

                sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
                sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}");
                
                if (oldline.Contains("new()"))
                {
                    sb.Append(" = new();");
                }

                sb.Append('\n');

                switch (typeCs)
                {
                    case "bytes":
                    {
                        break;
                    }
                    default:
                        sbDispose.Append($"this.{name} = default;\n\t\t\t");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        private static readonly Regex _responseTypeRegex = new Regex(@"//\s*ResponseType", RegexOptions.Compiled);
        private static Regex ResponseTypeRegex() => _responseTypeRegex;
    }
}
