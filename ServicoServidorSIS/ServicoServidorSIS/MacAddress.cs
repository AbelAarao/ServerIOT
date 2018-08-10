using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.NetworkInformation;

using System.IO;

using System.Runtime.InteropServices;

using System.Management;

using System.Net.Sockets;

namespace ServicoServidorSIS
{
    public  class MacAddress
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

        public static System.Net.NetworkInformation.PhysicalAddress  GetMacFromIP(System.Net.IPAddress IP)
        {

            if (IP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)

                throw new ArgumentException("suppoerts just IPv4 addresses");


            Int32 addrInt = IpToInt(IP);

            Int32 srcAddrInt = IpToInt(IP);


            byte[] mac = new byte[6]; // 48 bit

            uint length = (uint)mac.Length;

            int reply = SendARP(addrInt, srcAddrInt, mac, ref length);


            if (reply != 0)

                throw new System.ComponentModel.Win32Exception(reply);


            return new System.Net.NetworkInformation.PhysicalAddress(mac); ;

        }


        private static Int32 IpToInt(System.Net.IPAddress IP)
        {

            byte[] bytes = IP.GetAddressBytes();

            return BitConverter.ToInt32(bytes, 0);

        }

        public static string LeMacAddress(System.Net.IPAddress lIP)
        {

            return GetMacFromIP(lIP).ToString();
        }





    }
}
