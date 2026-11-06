using System;
using System.IO;

namespace ET
{
	/// <summary>
	/// TCP 流式数据包解析状态机枚举。
	/// </summary>
	public enum ParserState
	{
		/// <summary>
		/// 等待读取包头长度字段（外网 2 字节，内网 4 字节）。
		/// </summary>
		PacketSize,
		/// <summary>
		/// 已读出包长，正在等待接收完整的包体载荷字节。
		/// </summary>
		PacketBody
	}

	/// <summary>
	/// TCP 粘包与半包流式解析器，配合 <see cref="CircularBuffer"/> 将连续的 TCP 字节流切分为一个个完整的 <see cref="MemoryBuffer"/> 消息包。
	/// <remarks>
	/// 长度前缀规则：
	/// - 内网（<see cref="ServiceType.Inner"/>）：4 字节 <see cref="int"/> 包长前缀（最大允许 <c>ushort.MaxValue * 16</c> 即约 1MB）；
	/// - 外网（<see cref="ServiceType.Outer"/>）：2 字节 <see cref="ushort"/> 包长前缀（最大允许 65535 字节）。
	/// </remarks>
	/// </summary>
	public class PacketParser
	{
		private readonly CircularBuffer buffer;
		private int packetSize;
		private ParserState state;
		private readonly AService service;
		private readonly byte[] cache = new byte[8];
		/// <summary>
		/// 内网 TCP 消息包头长度字段字节数（4 字节 int）。
		/// </summary>
		public const int InnerPacketSizeLength = 4;
		/// <summary>
		/// 外网 TCP 消息包头长度字段字节数（2 字节 ushort）。
		/// </summary>
		public const int OuterPacketSizeLength = 2;
		public MemoryBuffer MemoryBuffer;

		public PacketParser(CircularBuffer buffer, AService service)
		{
			this.buffer = buffer;
			this.service = service;
		}

		/// <summary>
		/// 尝试从 <see cref="CircularBuffer"/> 中解析出一个完整的数据包；若当前可读字节不足以构成完整包则返回 <c>false</c> 等待后续数据到达。
		/// </summary>
		public bool Parse(out MemoryBuffer memoryBuffer)
		{
			while (true)
			{
				switch (this.state)
				{
					case ParserState.PacketSize:
					{
						if (this.service.ServiceType == ServiceType.Inner)
						{
							if (this.buffer.Length < InnerPacketSizeLength)
							{
								memoryBuffer = null;
								return false;
							}

							this.buffer.Read(this.cache, 0, InnerPacketSizeLength);

							this.packetSize = BitConverter.ToInt32(this.cache, 0);
							if (this.packetSize > ushort.MaxValue * 16 || this.packetSize < Packet.MinPacketSize)
							{
								throw new Exception($"recv packet size error, 可能是外网探测端口: {this.packetSize}");
							}
						}
						else
						{
							if (this.buffer.Length < OuterPacketSizeLength)
							{
								memoryBuffer = null;
								return false;
							}

							this.buffer.Read(this.cache, 0, OuterPacketSizeLength);

							this.packetSize = BitConverter.ToUInt16(this.cache, 0);
							if (this.packetSize < Packet.MinPacketSize)
							{
								throw new Exception($"recv packet size error, 可能是外网探测端口: {this.packetSize}");
							}
						}

						this.state = ParserState.PacketBody;
						break;
					}
					case ParserState.PacketBody:
					{
						if (this.buffer.Length < this.packetSize)
						{
							memoryBuffer = null;
							return false;
						}

						memoryBuffer = this.service.Fetch(this.packetSize);
						this.buffer.Read(memoryBuffer, this.packetSize);
						//memoryStream.SetLength(this.packetSize - Packet.MessageIndex);

						memoryBuffer.Seek(0, SeekOrigin.Begin);

						this.state = ParserState.PacketSize;
						return true;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}