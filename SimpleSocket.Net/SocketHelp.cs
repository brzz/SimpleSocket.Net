using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

#pragma warning disable 0649

/// <summary>
/// 20180906 add keepalive on Accpet use
/// </summary>
namespace CsharpTest
{
    public static class SocketExtensions
    {
        public static void SetSocketKeepAliveValues(this Socket instance, int KeepAliveTime, int KeepAliveInterval)
        {
            //KeepAliveTime is ms
            //KeepAliveTime: default value is 2hr
            //KeepAliveInterval: default value is 1s and Detect 5 times

            //the native structure
            //struct tcp_keepalive {
            //ULONG onoff;
            //ULONG keepalivetime;
            //ULONG keepaliveinterval;
            //};

            int size = System.Runtime.InteropServices.Marshal.SizeOf(new uint());
            byte[] inOptionValues = new byte[size * 3]; // 4 * 3 = 12
            bool OnOff = true;

            BitConverter.GetBytes((uint)(OnOff ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)KeepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)KeepAliveInterval).CopyTo(inOptionValues, size * 2);

            instance.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
    }
    public enum SocketHType
    {
        Server,
        Client
    }

    public class SocketHelp
    {

        #region Common Var
        private IPEndPoint endp;    
        private SocketHType type;
        private bool _Open;

        public Thread CntrolThread = null;
        public Socket SocketThis = null;

        //声明了一个Delegate对象,参数是Socket S, byte[] D
        public delegate void _onPacketRec(Socket S, byte[] D, int revByte);
        public _onPacketRec onPacketRec;

        public delegate void _onRecErr(Exception e, SocketHelp SocketThis, Socket Remote);
        public _onRecErr onRecErr;

        public delegate void _onSendErr(Exception e, SocketHelp SocketThis, Socket Remote);
        public _onSendErr onSendErr;

        public SocketHelp(IPEndPoint endp, SocketHType type)
        {
            this.endp = endp;
            this.type = type;
        }
#endregion


        #region Common Function
        public void Open()
        {
            if(SocketThis != null)
            {
                return;
            }

            SocketThis = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _Open = false;
            switch (type)
            {
                case SocketHType.Server: ServerAccept(); break;
                case SocketHType.Client: ClientConnect(); break;
            }
        }
        
        public void ReOpen()
        {
            if(type == SocketHType.Client)
            {
                try
                {
                    SocketThis.Close();
                }
                catch { }
                SocketThis = null;
                SocketThis = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    this.SocketThis.Connect(endp);
                }
                catch (Exception e)
                {
                    if (onRecErr != null)
                        onRecErr.Invoke(e, this, this.SocketThis);
                }
            }
            else
            {
                //服务模式
                throw new NotImplementedException();
            }
            
        }

        public void Close()
        {
            if (_Open)
            {
                if (type == SocketHType.Client)
                {
                    try { SocketThis.Close(); } catch { }
                    SocketThis = null;
                    _Open = false;
                    CntrolThread = null;
                }
                else
                {
                    //服务模式
                    throw new NotImplementedException();
                }
            }
            else
            {
                //已关闭怎么办
                //throw new NotImplementedException();
            }
            
        }

        public bool SockSned(byte[] Data)
        {
            if (this.type == SocketHType.Client)
            {
                if (this._Open)
                {
                    try
                    {
                        SocketThis.Send(Data);
                        return true;
                    }
                    catch (Exception e)
                    {
                        if (this.onSendErr != null)
                            this.onSendErr.Invoke(e, this, this.SocketThis);
                    }

                }
                return false;
            }
            else
            {
                //Server Bordcast
                for(int i = 0; i< this.ClientList.Count; i++)
                {
                    if (this._Open)
                    {
                        try
                        {
                            this.ClientList[i].clientSocket.Send(Data);
                            //假装全部成功
                        }
                        catch (Exception e)
                        {
                            if (this.onSendErr != null)
                                this.onSendErr.Invoke(e, this, this.ClientList[i].clientSocket);
                        }
                    }
                }
                return true;
            }
        }

        #endregion


        #region ServerProcess

        private void ServerAccept()
        {
            if (CntrolThread == null)
            {
                this._Open = true;
                CntrolThread = new Thread(_ServerAccept);
                CntrolThread.IsBackground = true;
                CntrolThread.Start(this);
            }
        }

        public class ClientStatus
        {
            public Socket clientSocket;
            public Thread clientThread;
            public SocketHelp _this;

            public ClientStatus(Socket clientSocket, Thread clientThread)
            {
                this.clientSocket = clientSocket;
                this.clientThread = clientThread;
            }
        }

        public List<ClientStatus> ClientList;

        public delegate void _onAccept(Socket Remote);
        public _onAccept onAccept;
        private static void _ServerAccept(Object arg)
        {
            SocketHelp _this = (SocketHelp)arg;

            _this.ClientList = new List<ClientStatus>();

            _this.SocketThis.Bind(_this.endp);
            _this.SocketThis.Listen(20);

            Console.WriteLine("Port:{0} Bind Success Wait Client...", _this.endp.Port);

            while (_this._Open)
            {
                ClientStatus c = 
                    new ClientStatus(_this.SocketThis.Accept(), new Thread(ClientRec));

                c._this = _this;
                _this.ClientList.Add(c);

                c.clientThread.Start(c);
            }


            _this.ClientList.Clear();
            _this.ClientList = null;

            _this.SocketThis.Close();
        }

        private static void ClientRec(object _ClientStatus)
        {
            ClientStatus CS = (ClientStatus)_ClientStatus;

            if(CS._this.onAccept != null)
                CS._this.onAccept.Invoke(CS.clientSocket);//First Accept

            byte[] result = new byte[1024*8];

            while (true)
            {
                try
                {
                    int receiveNumber = CS.clientSocket.Receive(result);
                    if (receiveNumber == 0) throw new Exception("收到0字节数据");
                    if(CS._this.onPacketRec != null && receiveNumber > 0)
                        CS._this.onPacketRec.Invoke
                            (CS.clientSocket, result, receiveNumber);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (CS._this.onRecErr != null)
                        CS._this.onRecErr.Invoke(e, CS._this, CS.clientSocket);

                    //remove unuse client
                    CS.clientSocket.Close();
                    CS._this.ClientList.Remove(CS);
                    break;
                }

            }
        }
#endregion


        #region ClientProcess
        private void ClientConnect()
        {
            if (CntrolThread == null)
            {
                this._Open = true;
                CntrolThread = new Thread(ClientRecv);
                CntrolThread.IsBackground = true;
                CntrolThread.Start(this);
            }
        }

        private static void ClientRecv(Object arg)
        {
            SocketHelp _this = (SocketHelp)arg;

            try
            {
                _this.SocketThis.Connect(_this.endp);
            }
            catch (Exception e)
            {
                if (_this.onRecErr != null)
                    _this.onRecErr.Invoke(e, _this, _this.SocketThis);
            }



            byte[] buffer = new byte[1024*8];
            int recByte = 0;
            while (_this._Open && _this.onPacketRec!= null)
            {
                try
                {
                    recByte = _this.SocketThis.Receive(buffer);
                    if (recByte <= 0) throw new SocketException();
                    if (_this.onPacketRec != null && recByte > 0)
                        _this.onPacketRec.Invoke(_this.SocketThis, buffer, recByte);
                    recByte = 0;
                    //threadclient.Start();
                }
                catch (Exception e)
                {
                    if (_this.onRecErr != null)
                        _this.onRecErr.Invoke(e, _this, _this.SocketThis);
                    else _this._Open = false;
                }
            }
        }

        #endregion


        #region Get Free Port
        /// <summary>
        /// 获取第一个可用的端口号
        /// </summary>
        /// <returns></returns>
        public static int GetFirstAvailablePort()
        {
            int MAX_PORT = 59999; //系统tcp/udp端口数最大是65535            
            int BEGIN_PORT = 30000;//从这个端口开始检测

            Random rd = new Random();
            int n = rd.Next(BEGIN_PORT, MAX_PORT-((MAX_PORT - BEGIN_PORT)/2));
            for (int i = n; i < MAX_PORT; i++)
            {
                if (PortIsAvailable(i)) return i;
            }

            return -1;
        }

        /// <summary>
        /// 获取操作系统已用的端口号
        /// </summary>
        /// <returns></returns>
        public static IList PortIsUsed()
        {
            //获取本地计算机的网络连接和通信统计数据的信息
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            //返回本地计算机上的所有Tcp监听程序
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();

            //返回本地计算机上的所有UDP监听程序
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

            //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            IList allPorts = new ArrayList();
            foreach (IPEndPoint ep in ipsTCP) allPorts.Add(ep.Port);
            foreach (IPEndPoint ep in ipsUDP) allPorts.Add(ep.Port);
            foreach (TcpConnectionInformation conn in tcpConnInfoArray) allPorts.Add(conn.LocalEndPoint.Port);

            return allPorts;
        }

        /// <summary>
        /// 检查指定端口是否已用
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool PortIsAvailable(int port)
        {
            bool isAvailable = true;

            IList portUsed = PortIsUsed();

            foreach (int p in portUsed)
            {
                if (p == port)
                {
                    isAvailable = false; break;
                }
            }

            return isAvailable;
        }
        #endregion
    }
}

#pragma warning restore
