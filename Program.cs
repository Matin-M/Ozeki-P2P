﻿using System;
using System.Threading;
using Ozeki.VoIP.SIP;

namespace MediaServices
{
    class Program
    {
        static VoIPHandler voIP;
        //Replace with your local IP address. Hardcoded for testing purposes.
        const string LOCALADDRESS = "192.168.64.3";   

        static void Main(string[] args)
        {

            Console.Write("Please enter a SIP ID: ");
            var sipID = Console.ReadLine();
            Console.Write("Please enter SIP Address: ");
            var sipAddress = Console.ReadLine();
            Console.Write("Please enter a port to use: ");
            var port = Console.ReadLine();

            voIP = new VoIPHandler(sipID, sipAddress, Int32.Parse(port), LOCALADDRESS);

            voIP.IncomingCall += softphone_IncomingCall;
            voIP.CallStateChanged += softphone_CallStateChanged;
            voIP.RegistrationReady += softphone_RegistrationReady;
            voIP.IncomingMessage += softphone_incomingMessage;

            Console.Write("Would you like to make a call? ");
            if (Console.ReadLine().Equals("y"))
            {
                Console.Write("Please enter a SIP ID: ");
                var sipIDToCall = Console.ReadLine();
                Console.Write("Please enter SIP Address: ");
                var sipAddressToCall = Console.ReadLine();
                Console.Write("Please enter remote port number: ");
                var portToCall = Console.ReadLine();
                voIP.makeCall(LOCALADDRESS, portToCall, new SIPAddress(sipIDToCall, sipAddressToCall), MediaType.MP3);
            }

            Console.WriteLine("Waiting for Call(s)...");

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
            while (true)
            {
                Console.Write("Send SIP message or Audio? ");
                var input = Console.ReadLine();
                if (input.Equals("msg"))
                {
                    while (true)
                    {
                        Console.Write("Enter a message: ");
                        var message = Console.ReadLine();
                        if (message.Equals("(exit)")) break;
                        voIP.sendMessage(message);
                    }

                }
                else if (input.Equals("audio"))
                {
                    Console.Write("What type of media do you want to transmit? ");
                    MediaType mediaType = (MediaType)Enum.Parse(typeof(MediaType), Console.ReadLine());
                    if (mediaType == MediaType.MP3)
                    {
                        Console.Write("Enter file path: ");
                        var filePath = Console.ReadLine();
                        voIP.initializeAudioPlayers(mediaType, filePath);
                        HardwareAudioHandler.mp3Player.Start();

                        Console.WriteLine("Stop Media:");
                        Console.ReadLine();
                        HardwareAudioHandler.mp3Player.Stop();

                    }
                    else if (mediaType == MediaType.WAV)
                    {
                        Console.Write("Enter file path: ");
                        var filePath = Console.ReadLine();
                        voIP.initializeAudioPlayers(mediaType, filePath);

                        HardwareAudioHandler.wavPlayer.Start();

                        Console.WriteLine("Stop Media:");
                        Console.ReadLine();
                        HardwareAudioHandler.wavPlayer.Stop();

                    }
                    else if (mediaType == MediaType.TTS)
                    {
                        voIP.initializeAudioPlayers(mediaType, "N/A");
                        Console.Write("Enter a phrase to read: ");
                        var phrase = Console.ReadLine();
                        HardwareAudioHandler.setAndStartTTSMessage(phrase);

                        Console.WriteLine("Stop TTS:");
                        Console.ReadLine();
                        HardwareAudioHandler.textToSpeech.Stop();

                    }

                    Console.WriteLine("Playback initialized");
                }
                else
                {
                    Console.WriteLine("Hanging up call...");
                    voIP.closeAudioPlayers();
                    voIP.HangUp();
                    break;
                }
            }

        }

        static void softphone_incomingMessage(object sender, InstantMessage e)
        {
            Console.Write("\nMessage received from {0}: {1}", e.Sender, e.Content);
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
