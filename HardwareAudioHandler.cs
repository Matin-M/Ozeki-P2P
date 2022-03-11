using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Ozeki.Media;
using Ozeki.VoIP;

namespace MediaServices
{

    public enum MediaType
    {
        MICROPHONE,
        TTS,
        MP3,
        WAV
    }

    public class HardwareAudioHandler
    {
        //Audio input types.
        public static Microphone microphone;
        public static Speaker speakers;
        public static TextToSpeech textToSpeech;
        public static MP3StreamPlayback mp3Player;
        public static WaveStreamPlayback wavPlayer;
        public static MediaConnector connector = new MediaConnector();

        public static void initSpeakers()
        {
            speakers = Speaker.GetDefaultDevice();
            speakers.Start();
        }

        public static void connectReceiverToSpeaker(ref PhoneCallAudioReceiver receiver)
        {
            connector.Connect(receiver, speakers);
        }

        public static void connectPlaybackDeviceToSender(ref PhoneCallAudioSender sender, MediaType type)
        {
            switch (type)
            {
                case MediaType.MICROPHONE:
                    connector.Connect(microphone, sender); break;
                case MediaType.TTS:
                    connector.Connect(textToSpeech, sender); break;
                case MediaType.MP3:
                    connector.Connect(mp3Player, sender); break;
                case MediaType.WAV:
                    connector.Connect(wavPlayer, sender); break;
            }
        }

        public static void initPlaybackDevice(MediaType type, string parameters)
        {
            switch (type)
            {
                case MediaType.MICROPHONE:
                    initMic(); break;
                case MediaType.TTS:
                    initTTS(); break;
                case MediaType.MP3:
                    initMP3(parameters); break;
                case MediaType.WAV:
                    initWAV(parameters); break;
            }
        }

        private static void initMic()
        {
            try
            {
                microphone = Microphone.GetDefaultDevice();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void initMP3(string pathToFile)
        {
            try
            {
                mp3Player = new MP3StreamPlayback(pathToFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void initWAV(string pathToFile)
        {
            try
            {
                wavPlayer = new WaveStreamPlayback(pathToFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void initTTS()
        {
            try
            {
                textToSpeech = new TextToSpeech();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}