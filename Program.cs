using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ozeki.Media;
using Ozeki.VoIP;

namespace MediaServices
{
    class Program
    {

        static void Main(string[] args)
        {

            Console.Write("Please enter a SIP ID: ");
            var sipID = Console.ReadLine();
            Console.Write("Please enter SIP Address: ");
            var sipAddress = Console.ReadLine();
            Console.Write("Please enter a port to use: ");
            var port = Console.ReadLine();


            VoIPHandler voIP = new VoIPHandler(sipID, sipAddress, Int32.Parse(port), "192.168.64.3");

            Console.Write("Would you like to make a call? ");
            if (Console.ReadLine().Equals("y"))
            {
                Console.Write("Please enter a SIP ID: ");
                var sipIDToCall = Console.ReadLine();
                Console.Write("Please enter SIP Address: ");
                var sipAddressToCall = Console.ReadLine();
                Console.Write("Please enter remote port number: ");
                var portToCall = Console.ReadLine();
                voIP.makeCall("192.168.64.3", portToCall, new SIPAddress(sipIDToCall, sipAddressToCall), MediaType.MP3);
            }



            //Block main thread
            BlockExit();
        }

        private static void BlockExit()
        {
            while (true)
            {
                Thread.Sleep(10);
            }
        }


    }
}
