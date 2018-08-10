using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Threading;
using System.Net;
using System.Net.Sockets;


using System.Diagnostics;



namespace ServicoServidorSIS
{

    public class ServerUDP
    {
        public static string serverUDP;
        public static string portaUDP;

        public static EventLog LogEventos;


        public static void StartListening()
        {
            LogaMsg("ServidorUDP.StartListening excutando porta:" + portaUDP);
            try
            {

                UdpClient udpClient = new UdpClient(Convert.ToInt32(portaUDP));
                while (true)
                {
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    //lbConnections.Items.Add(RemoteIpEndPoint.Address.ToString() + ":" + returnData.ToString());

                    LogaMsg("ServidorUDP IP:" + RemoteIpEndPoint.Address.ToString() + " recebeu: " + returnData.ToString());
                }

            }
            catch
            {
                LogaMsg("Erro em: ServidorUDP.StartListening");

            }

        }

        public static void sendUDP(string buffer)
        {
            UdpClient udpClient = new UdpClient();

            udpClient.Connect(serverUDP, Convert.ToInt32(portaUDP));
            Byte[] senddata = Encoding.ASCII.GetBytes(buffer);

            udpClient.Send(senddata, senddata.Length);

            LogaMsg("sendUDP-->Servidor:" + serverUDP + " buffer: " + buffer);

        }


        public void serverThread()
        {
         }

        private static void LogaMsg(String sMsg)
        {
            LogEventos.WriteEntry(sMsg);
        }
    }
}


