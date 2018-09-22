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
        
        //public static void SetKeepAlive(ulong keepalive_time, ulong keepalive_interval)
        //{
        //    //mill-seconds
        //    int bytes_per_long = 32 / 8;
        //    byte[] keep_alive = new byte[3 * bytes_per_long];
        //    ulong[] input_params = new ulong[3];
        //    int i1;
        //    int bits_per_byte = 8;

        //    if (keepalive_time == 0 || keepalive_interval == 0)
        //        input_params[0] = 0;
        //    else
        //        input_params[0] = 1;
        //    input_params[1] = keepalive_time;
        //    input_params[2] = keepalive_interval;
        //    for (i1 = 0; i1 < input_params.Length; i1++)
        //    {
        //        keep_alive[i1 * bytes_per_long + 3] = (byte)(input_params[i1] >> ((bytes_per_long - 1) * bits_per_byte) & 0xff);
        //        keep_alive[i1 * bytes_per_long + 2] = (byte)(input_params[i1] >> ((bytes_per_long - 2) * bits_per_byte) & 0xff);
        //        keep_alive[i1 * bytes_per_long + 1] = (byte)(input_params[i1] >> ((bytes_per_long - 3) * bits_per_byte) & 0xff);
        //        keep_alive[i1 * bytes_per_long + 0] = (byte)(input_params[i1] >> ((bytes_per_long - 4) * bits_per_byte) & 0xff);
        //    }
        //    //LogTrace.Trace(this, "Keep Alive Bits: {0}", ByteArrayFormater.HexDump(keep_alive));
        //    //NetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, keep_alive);
        //}

        static void RecF(Socket Remote, byte[] Data, int RecInt)
        {
            Console.WriteLine(Encoding.UTF8.GetString(Data, 0, RecInt));
        }

        static void Accpet(Socket Remote)
        {
            //byte[] KA = { 1,0,0,0,16,39,0,0,152,58,0,0};
            //Remote.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, KA);
            Remote.SetSocketKeepAliveValues(10000, 1000);
        }
        static void reTry(Exception e, SocketHelp SocketThis, Socket Remote)
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
            //byte[] KA = { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //SetKeepAlive(10000, 15000);
            #region Server Demo
            IPAddress addr = IPAddress.Parse("0.0.0.0");
            IPEndPoint endp = new IPEndPoint(addr, 60231);
            SocketHelp S = new SocketHelp(endp, SocketHType.Server);

            S.onAccept += Accpet;
            S.onPacketRec += RecF;
            S.onRecErr = reTry;



            S.Open();
            Console.ReadKey();
            #endregion

#if  StartClient
            #region Client Demo
            IPAddress addr2 = IPAddress.Parse("127.0.0.1");
            IPEndPoint endp2 = new IPEndPoint(addr, 60231);
            SocketHelp C = new SocketHelp(endp, SocketHType.Client);

            S.onPacketRec += RecF;
            S.onRecErr = reTry;


            S.Open();
            Console.ReadKey();
            #endregion
#endif
        }
    }
}
