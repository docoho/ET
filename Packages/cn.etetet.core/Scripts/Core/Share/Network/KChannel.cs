using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ET
{
	/// <summary>
	/// 基于 KCP 可靠 UDP 协议的网络通道实现。
	/// <remarks>
	/// 核心职责：
	/// 1. 维护单条 KCP 连接的握手（每 300ms 重发 <c>SYN</c> 直至收到 <c>ACK</c> 或超时）、状态机更新（<see cref="Kcp.Update"/> / <see cref="Kcp.Check"/>）与断开通知（<c>FIN</c>）；
	/// 2. 支持针对外网（<see cref="ServiceType.Outer"/>，较小窗口与 MTU=470 降低丢包重传代价）与内网（<see cref="ServiceType.Inner"/>，大窗口 1024 与 MTU=1400 提升吞吐）差异化调优 KCP 参数；
	/// 3. 实现超过 <see cref="MaxKcpMessageSize"/>（10000 字节）的超大消息应用层自动分片发送与接收重组。
	/// </remarks>
	/// </summary>
	public class KChannel : AChannel
	{
		/// <summary>
		/// 单次送入底层 KCP 的最大消息分片字节数（超出此大小时由 <see cref="KChannel"/> 先发送 8 字节分片头再分段写入 KCP）。
		/// </summary>
		private const int MaxKcpMessageSize = 10000;
		
		private readonly KService Service;

		/// <summary>
		/// 底层纯 C# KCP 协议状态机实例。
		/// </summary>
		private Kcp kcp { get; set; }

		/// <summary>
		/// 连接握手尚未完成（<see cref="IsConnected"/> == false）期间暂存的待发送消息队列。
		/// </summary>
		private readonly Queue<MemoryBuffer> waitSendMessages = new();
		
		/// <summary>
		/// 通道创建时的相对时间戳（毫秒，相对于 <see cref="KService.TimeNow"/>）。
		/// </summary>
		public readonly uint CreateTime;

		/// <summary>
		/// 本端 KCP 连接号（即 <c>(uint)this.Id</c>）。
		/// </summary>
		public uint LocalConn
		{
			get
			{
				return (uint)this.Id; 
			}
			private set
			{
				this.Id = value;
			}
		}
		
		/// <summary>
		/// 远端 KCP 连接号（握手成功后确定，用于防伪造包校验及底层 KCP 会话标识）。
		/// </summary>
		public uint RemoteConn { get; set; }

		private readonly byte[] sendCache = new byte[2 * 1024];
		
		/// <summary>
		/// 标记当前 KCP 通道是否已完成握手并建立连接。
		/// </summary>
		public bool IsConnected { get; set; }

		/// <summary>
		/// 客户端真实 IP 地址字符串（经过 Router 转发时由 SYN 包携带透传）。
		/// </summary>
		public string RealAddress { get; set; }
		
		/// <summary>
		/// 正在接收组装中的消息内存缓冲区（大包分片接收时跨多次 Receive 持续写入）。
		/// </summary>
		private MemoryBuffer readMemory;
		/// <summary>
		/// 当前大包分片还剩余多少字节尚未接收完毕（0 表示当前未处于分片重组状态）。
		/// </summary>
		private int needReadSplitCount;

		/// <summary>
		/// 根据内网/外网服务类型初始化 KCP 的极速模式、窗口大小、MTU 和最小 RTO。
		/// </summary>
		private void InitKcp()
		{
			switch (this.Service.ServiceType)
			{
				case ServiceType.Inner:
					this.kcp.SetNoDelay(1, 1, 2, 1);
					this.kcp.SetWindowSize(1024, 1024);
					this.kcp.SetMtu(1400); // 默认1400
					this.kcp.SetMinrto(30);
					break;
				case ServiceType.Outer:
					this.kcp.SetNoDelay(1, 1, 2, 1);
					this.kcp.SetWindowSize(256, 256);
					this.kcp.SetMtu(470);
					this.kcp.SetMinrto(30);
					break;
			}

		}
		
		// connect
		/// <summary>
		/// 构造主动发起连接的客户端 KCP 通道，并立即发送首包 SYN 握手请求。
		/// </summary>
		public KChannel(uint localConn, IPEndPoint remoteEndPoint, KService kService)
		{
			this.Service = kService;
			this.LocalConn = localConn;
			this.ChannelType = ChannelType.Connect;
			
			Log.Info($"channel create: {this.LocalConn} {remoteEndPoint} {this.ChannelType}");
			
			
			this.RemoteAddress = remoteEndPoint;
			this.CreateTime = kService.TimeNow;

			this.Connect(this.CreateTime);

		}

		// accept
		/// <summary>
		/// 构造服务端收到 SYN 后被动接受的 KCP 通道。
		/// </summary>
		public KChannel(uint localConn, uint remoteConn, IPEndPoint remoteEndPoint, KService kService)
		{
			this.Service = kService;
			this.ChannelType = ChannelType.Accept;
			
			Log.Info($"channel create: {localConn} {remoteConn} {remoteEndPoint} {this.ChannelType}");
			this.LocalConn = localConn;
			this.RemoteConn = remoteConn;
			this.RemoteAddress = remoteEndPoint;
			this.kcp = new Kcp(this.RemoteConn, this.Output);
			this.InitKcp();
			
			this.CreateTime = kService.TimeNow;
		}
	

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			uint localConn = this.LocalConn;
			uint remoteConn = this.RemoteConn;
			Log.Info($"channel dispose: {localConn} {remoteConn} {this.Error}");
			
			long id = this.Id;
			this.Id = 0;
			this.Service.Remove(id);

			try
			{
				if (this.Error != ErrorCore.ERR_PeerDisconnect)
				{
					this.Service.Disconnect(localConn, remoteConn, this.Error, this.RemoteAddress, 3);
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}

			this.kcp = null;
		}

		public void HandleConnnect()
		{
			// 如果连接上了就不用处理了
			if (this.IsConnected)
			{
				return;
			}

			this.kcp = new Kcp(this.RemoteConn, this.Output);
			this.InitKcp();

			Log.Info($"channel connected: {this.LocalConn} {this.RemoteConn} {this.RemoteAddress} {this.waitSendMessages.Count}");
			this.IsConnected = true;
			while (true)
			{
				if (this.waitSendMessages.Count <= 0)
				{
					break;
				}
				
				MemoryBuffer buffer = this.waitSendMessages.Dequeue();
				this.Send(buffer);
			}
		}

		private long lastConnectTime = long.MaxValue;

		/// <summary>
		/// 发送请求连接消息
		/// </summary>
		private void Connect(uint timeNow)
		{
			try
			{
				if (this.IsConnected)
				{
					return;
				}
				
				// 300毫秒后再次update发送connect请求
				if (timeNow < this.lastConnectTime + 300)
				{
					this.Service.AddToUpdate(300, this.Id);
					return;
				}
				
				// 10秒连接超时
				if (timeNow > this.CreateTime + KService.ConnectTimeoutTime)
				{
					Log.Error($"kChannel connect timeout: {this.Id} {this.RemoteConn} {timeNow} {this.CreateTime} {this.ChannelType} {this.RemoteAddress}");
					this.OnError(ErrorCore.ERR_KcpConnectTimeout);
					return;
				}
				
				byte[] buffer = sendCache;
				buffer.WriteTo(0, KcpProtocalType.SYN);
				buffer.WriteTo(1, this.LocalConn);
				buffer.WriteTo(5, this.RemoteConn);
				this.Service.Transport.Send(buffer, 0, 9, this.RemoteAddress, this.ChannelType);
				// 这里很奇怪 调用socket.LocalEndPoint会动到this.RemoteAddressNonAlloc里面的temp，这里就不仔细研究了
				Log.Info($"kchannel connect {this.LocalConn} {this.RemoteConn} {this.RealAddress}");

				this.lastConnectTime = timeNow;

				this.Service.AddToUpdate(300, this.Id);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_SocketCantSend);
			}
		}

		public void Update(uint timeNow)
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			// 如果还没连接上，发送连接请求
			if (!this.IsConnected && this.ChannelType == ChannelType.Connect)
			{
				this.Connect(timeNow);
				return;
			}

			if (this.kcp == null)
			{
				return;
			}
			
			try
			{
				this.kcp.Update(timeNow);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_SocketError);
				return;
			}

			uint nextUpdateTime = this.kcp.Check(timeNow);
			this.Service.AddToUpdate(nextUpdateTime, this.Id);
		}

		public void HandleRecv(byte[] date, int offset, int length)
		{
			if (this.IsDisposed)
			{
				return;
			}

			this.kcp.Input(date.AsSpan(offset, length));
			this.Service.AddToUpdate(0, this.Id);
			while (true)
			{
				if (this.IsDisposed)
				{
					break;
				}
				int n = this.kcp.PeekSize();
				if (n < 0)
				{
					break;
				}
				if (n == 0)
				{
					this.OnError((int)SocketError.NetworkReset);
					return;
				}

				if (this.needReadSplitCount > 0) // 说明消息分片了
				{
					byte[] buffer = readMemory.GetBuffer();
					int count = this.kcp.Receive(buffer.AsSpan((int)(this.readMemory.Length - this.needReadSplitCount), n));
					this.needReadSplitCount -= count;
					if (n != count)
					{
						Log.Error($"kchannel read error1: {this.LocalConn} {this.RemoteConn}");
						this.OnError(ErrorCore.ERR_KcpReadNotSame);
						return;
					}
					
					if (this.needReadSplitCount < 0)
					{
						Log.Error($"kchannel read error2: {this.LocalConn} {this.RemoteConn}");
						this.OnError(ErrorCore.ERR_KcpSplitError);
						return;
					}
										
					// 没有读完
					if (this.needReadSplitCount != 0)
					{
						continue;
					}
				}
				else
				{
					this.readMemory = this.Service.Fetch(n);
					this.readMemory.SetLength(n);
					this.readMemory.Seek(0, SeekOrigin.Begin);
					
					byte[] buffer = readMemory.GetBuffer();
					
					int count = this.kcp.Receive(buffer.AsSpan(0, n));
					if (n != count)
					{
						break;
					}
					
					// 判断是不是分片
					if (n == 8)
					{
						int headInt = BitConverter.ToInt32(this.readMemory.GetBuffer(), 0);
						if (headInt == 0)
						{
							this.needReadSplitCount = BitConverter.ToInt32(readMemory.GetBuffer(), 4);
							if (this.needReadSplitCount <= MaxKcpMessageSize)
							{
								Log.Error($"kchannel read error3: {this.needReadSplitCount} {this.LocalConn} {this.RemoteConn}");
								this.OnError(ErrorCore.ERR_KcpSplitCountError);
								return;
							}
							this.readMemory.SetLength(this.needReadSplitCount);
							this.readMemory.Seek(0, SeekOrigin.Begin);
							continue;
						}
					}
				}
				
				MemoryBuffer memoryBuffer = this.readMemory;
				this.readMemory = null;
				
				memoryBuffer.Seek(0, SeekOrigin.Begin);
				
				this.OnRead(memoryBuffer);
			}
		}

		private void Output(byte[] bytes, int count)
		{
			if (this.IsDisposed)
			{
				return;
			}
			try
			{				
				// 没连接上 kcp不往外发消息, 其实本来没连接上不会调用update，这里只是做一层保护
				if (!this.IsConnected)
				{
					return;
				}
				
				if (count == 0)
				{
					Log.Error($"output 0");
					return;
				}

				bytes.WriteTo(0, KcpProtocalType.MSG);
				// 每个消息头部写下该channel的id;
				bytes.WriteTo(1, this.LocalConn);
				this.Service.Transport.Send(bytes, 0, count + 5, this.RemoteAddress, this.ChannelType);
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

        private void KcpSend(MemoryBuffer memoryStream)
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			int count = (int) (memoryStream.Length - memoryStream.Position);

			// 超出maxPacketSize需要分片
			
			if (count <= MaxKcpMessageSize)
			{
				this.kcp.Send(memoryStream.GetBuffer().AsSpan((int)memoryStream.Position, count));
			}
			else
			{
				// 先发分片信息
				this.sendCache.WriteTo(0, 0);
				this.sendCache.WriteTo(4, count);
				this.kcp.Send(this.sendCache.AsSpan(0, 8));

				// 分片发送
				int alreadySendCount = 0;
				while (alreadySendCount < count)
				{
					int leftCount = count - alreadySendCount;
					
					int sendCount = leftCount < MaxKcpMessageSize? leftCount: MaxKcpMessageSize;
					
					this.kcp.Send(memoryStream.GetBuffer().AsSpan((int)memoryStream.Position + alreadySendCount, sendCount));
					
					alreadySendCount += sendCount;
				}
			}

			this.Service.AddToUpdate(0, this.Id);
		}
		
		public void Send(MemoryBuffer memoryBuffer)
		{
			if (!this.IsConnected)
			{
				this.waitSendMessages.Enqueue(memoryBuffer);
				return;
			}
			if (this.kcp == null)
			{
				throw new Exception("kchannel connected but kcp is zero!");
			}
			
			// 检查等待发送的消息，如果超出最大等待大小，应该断开连接
			int n = (int)this.kcp.WaitSendCount;
			int maxWaitSize = 0;
			switch (this.Service.ServiceType)
			{
				case ServiceType.Inner:
					maxWaitSize = Kcp.InnerMaxWaitSize;
					break;
				case ServiceType.Outer:
					maxWaitSize = Kcp.OuterMaxWaitSize;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			if (n > maxWaitSize)
			{
				Log.Error($"kcp wait snd too large: {n}: {this.LocalConn} {this.RemoteConn}");
				this.OnError(ErrorCore.ERR_KcpWaitSendSizeTooLarge);
				return;
			}
			this.KcpSend(memoryBuffer);
			this.Service.Recycle(memoryBuffer);
		}
		
		private void OnRead(MemoryBuffer memoryStream)
		{
			try
			{
				this.Service.ReadCallback(this.Id, memoryStream);
			}
			catch (Exception e)
			{
				Log.Error(e);
				this.OnError(ErrorCore.ERR_PacketParserError);
			}
		}
		
		public void OnError(int error)
		{
			long channelId = this.Id;
			this.Service.Remove(channelId, error);
			this.Service.ErrorCallback(channelId, error);
		}
	}
}
