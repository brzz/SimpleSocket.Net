using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CsharpTest;

namespace SimpleSocket.Net
{
    class Program
    {
        static void RecF(Socket Remote, byte[] Data, int RecInt)
        {
            Console.WriteLine(Encoding.UTF8.GetString(Data, 0, RecInt));
        }

        static void reTry(Exception e, SocketHelp SocketThis)
        {
            try
            {
                System.Threading.Thread.Sleep(1000);
                SocketThis.ReOpen();
            }
            catch { }
        }

        static void Main(string[] args)
        {
#region Server Demo
            IPAddress addr = IPAddress.Parse("127.0.0.1");
            IPEndPoint endp = new IPEndPoint(addr, 60231);
            SocketHelp S = new SocketHelp(endp, SocketHType.Server);

            S.onPacketRec += RecF;
            S.onRecErr = reTry;



            S.Open();
            Console.ReadKey();
#endregion

#region Client Demo
            IPAddress addr2 = IPAddress.Parse("127.0.0.1");
            IPEndPoint endp2 = new IPEndPoint(addr, 60231);
            SocketHelp C = new SocketHelp(endp, SocketHType.Client);

            S.onPacketRec += RecF;
            S.onRecErr = reTry;


            S.Open();
            Console.ReadKey();
#endregion
        }
    }
}
