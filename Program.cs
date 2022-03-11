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
        static VoIPHandler voIP;

        static void Main(string[] args)
        {

            Console.Write("Please enter a SIP ID: ");
            var sipID = Console.ReadLine();
            Console.Write("Please enter SIP Address: ");
            var sipAddress = Console.ReadLine();
            Console.Write("Please enter a port to use: ");
            var port = Console.ReadLine();

            voIP = new VoIPHandler(sipID, sipAddress, Int32.Parse(port), "192.168.64.3");

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

        static void softphone_RegistrationReady(object sender, EventArgs e)
        {
            Console.WriteLine("Registration ready!");
        }

        static void softphone_IncomingCall(object sender, EventArgs e)
        {
            Console.WriteLine("\nIncoming call! Accept? ");
            if (Console.ReadLine().Equals("y"))
            {
                voIP.AcceptCall();
            }
            else
            {
                voIP.HangUp();
            }
            
        }

        static void softphone_CallStateChanged(object sender, EventArgs e)
        {
            Console.Write("Send SIP message or Audio? ");
            if (Console.ReadLine().Equals("msg"))
            {
                while (true)
                {
                    Console.Write("Enter a message: ");
                    var message = Console.ReadLine();
                    if (message.Equals("(exit)")) break;
                    voIP.sendMessage(message);
                }

            }
            else if(Console.ReadLine().Equals("audio"))
            {
                /*
                 * Notes:
                 * -When a user wants to transmit audio, the device accociated with that type needs to be:
                 *  -Initialized(for wav, tts, and mp3 player, a file path/message needs to be supplied).
                 *  -Connected to the media sernder(look in voiphandler).
                 *  -Started.
                 * 
                 * -This all needs to be done HERE. Hardware handler should expose functions that should enable this.
                 * -This should be done in a way that respects encapsulation and proper event driven architecture. 
                 *  Meaning, this class should not be accessing variables in VoIP handler and in HardwareAudioHandler.
                 */
                Console.Write("What type of media do you wan to transmit? ");
                MediaType mediaType = (MediaType) Enum.Parse(typeof(MediaType), Console.ReadLine());
                HardwareAudioHandler.initPlaybackDevice(mediaType, "../../Resources/beatles.mp3");
                Console.WriteLine("Playback initialized");
            }

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
