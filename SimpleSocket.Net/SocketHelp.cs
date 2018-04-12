using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

#pragma warning disable 0649


namespace CsharpTest
{
    public enum SocketHType
    {
        Server,
        Client
    }

    class SocketHelp
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

        public delegate void _onRecErr(Exception e, SocketHelp SocketThis);
        public _onRecErr onRecErr;

        public delegate void _onSendErr(Exception e, SocketHelp SocketThis);
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
                        onRecErr.Invoke(e, this);
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
                            this.onSendErr.Invoke(e, this);
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
                                this.onSendErr.Invoke(e, this);
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
        private static void _ServerAccept(Object arg)
        {
            SocketHelp _this = (SocketHelp)arg;

            _this.ClientList = new List<ClientStatus>();

            _this.SocketThis = 
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _this.SocketThis.Bind(_this.endp);
            _this.SocketThis.Listen(20);

            while (_this._Open)
            {
                ClientStatus c = 
                    new ClientStatus(_this.SocketThis.Accept(), new Thread(ClientRec));
                c._this = _this;
                _this.ClientList.Add(c);
                c.clientThread.Start(c);
            }
        }

        private static void ClientRec(object _ClientStatus)
        {
            ClientStatus CS = (ClientStatus)_ClientStatus;

            byte[] result = new byte[1572];

            while (true)
            {
                try
                {
                    int receiveNumber = CS.clientSocket.Receive(result);
                    if(CS._this.onPacketRec != null)
                        CS._this.onPacketRec.Invoke
                            (CS.clientSocket, result, receiveNumber);
                }
                catch (Exception e)
                {
                    if (CS._this.onRecErr != null)
                        CS._this.onRecErr.Invoke(e, CS._this);

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
                    _this.onRecErr.Invoke(e, _this);
            }



            byte[] buffer = new byte[1572];
            int recByte = 0;
            while (_this._Open)
            {
                try
                {
                    recByte = _this.SocketThis.Receive(buffer);
                    if (recByte < 1) throw new SocketException();
                    if (_this.onPacketRec != null)
                        _this.onPacketRec.Invoke(_this.SocketThis, buffer, recByte);
                    recByte = 0;
                    //threadclient.Start();
                }
                catch (Exception e)
                {
                    if (_this.onRecErr != null)
                        _this.onRecErr.Invoke(e, _this);
                }
            }
        }

#endregion

    }
}
