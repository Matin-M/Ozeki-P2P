using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Ozeki.Media;
using Ozeki.Network;
using Ozeki.VoIP;
using TransportType = Ozeki.Network.TransportType;

namespace MediaServices
{
    public class VoIPHandler
    {
        //Softphone object delcarations and properties
        private ISoftPhone softphone;
        private IPhoneLine phoneLine;
        private IPhoneCall call;
        //Identification
        private string sipID;
        private string sipAddress;
        private string caller;
        //Conference audio I/O
        private PhoneCallAudioSender mediaSender;
        private PhoneCallAudioReceiver mediaReceiver;
        //Networking
        private const Int32 MINPORT = 5000;
        private const Int32 MAXPORT = 10000;
        private Int32 localPort;
        private string localIpAddress;
        //Call management
        private bool incomingCall = false;
        private bool isRegistered = false;
        private MediaType mediaType;
        private string mediaParameters;

        public VoIPHandler(string sipID, string sipAddress, Int32 localPort, string localIpAddress)
        {
            softphone = SoftPhoneFactory.CreateSoftPhone(MINPORT, MAXPORT);
            this.sipID = sipID;
            this.sipAddress = sipAddress;
            this.localIpAddress = localIpAddress;
            this.localPort = localPort;
            this.mediaType = MediaType.MP3;

            try
            {
                //Initialize audio devices
                mediaSender = new PhoneCallAudioSender();
                mediaReceiver = new PhoneCallAudioReceiver();
                //Connect speakers to reciever.
                HardwareAudioHandler.initSpeakers();
                Console.WriteLine("Speakers initialized");
                HardwareAudioHandler.connectReceiverToSpeaker(ref mediaReceiver);
                Console.WriteLine("Speakers connected");

                if (localIpAddress.Equals("")) this.localIpAddress = NetworkAddressHelper.GetLocalIP().ToString();
                if (localPort == 0) this.localPort = 5060;
                SIPAddress sipIdentity = new SIPAddress(sipID, sipAddress);
                var config = new DirectIPPhoneLineConfig(localIpAddress, localPort, sipIdentity, TransportType.Udp);

                phoneLine = softphone.CreateDirectIPPhoneLine(config);
                //Configure codecs:
                foreach (var s in softphone.Codecs)
                {
                    // This line disables all of the default codecs that are used.
                    softphone.DisableCodec(s.PayloadType);
                }
                softphone.EnableCodec(CodecPayloadType.G729);
                phoneLine.RegistrationStateChanged += line_RegStateChanged;
                softphone.IncomingCall += softphone_IncomingCall;
                softphone.RegisterPhoneLine(phoneLine);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error during registration:" + e.Message);
            }

        }

        private void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
                Console.WriteLine("Registration failed!");

            if (e.State == RegState.RegistrationSucceeded)
            {
                Console.WriteLine("Registration succeeded - Online!");
                isRegistered = true;
            }
        }

        void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            Console.WriteLine("Incoming call!");
            call = e.Item;
            caller = call.DialInfo.CallerID;
            incomingCall = true;
            call.CallStateChanged += call_CallStateChanged;
            call.InstantMessageReceived += call_InstantMessageReceived;

            DispatchAsync(() =>
            {
                call.Answer();
            });
        }

        void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            Console.WriteLine("Call state: {0}.", e.State);

            if(e.State == CallState.Error)
            {
                Console.WriteLine(e.ToString());
            }

            if (e.State == CallState.Answered)
            {
                caller = call.DialInfo.CallerID;
                Console.WriteLine("Answering Call...");
                mediaReceiver.AttachToCall(call);

                Console.Write("Start Media/Enter Message: ");
                var input = Console.ReadLine();
                
                if (input.Equals("message"))
                {
                    
                    while (true)
                    {
                        //Console.WriteLine("Enter address");
                        //var address = Console.ReadLine();
                        Console.Write("Enter a message: ");
                        var message = Console.ReadLine();
                        if (message.Equals("(exit)")) break;
                        sendMessage(message);
                    }
                    
                }
                
                HardwareAudioHandler.initPlaybackDevice(mediaType, "../../Resources/beatles.mp3");
                Console.WriteLine("Playback initialized");
                HardwareAudioHandler.connectPlaybackDeviceToSender(ref mediaSender, mediaType);
                Console.WriteLine("Playback connected");
                mediaSender.AttachToCall(call);
                Console.WriteLine("Playback attatched to call.");
                HardwareAudioHandler.mp3Player.Start();

                Console.WriteLine("Stop Media:");
                Console.ReadLine();
                HardwareAudioHandler.mp3Player.Stop();

            }


            if (e.State.IsCallEnded())
            {
                Console.WriteLine("Call Terminated");
            }
        }

        public void makeCall(string remoteAddress, string remotePort, SIPAddress callerID, MediaType mediaType)
        {
            if (call == null && isRegistered)
            {
                DispatchAsync(() => {
                    this.mediaType = mediaType;
                    call = softphone.CreateDirectIPCallObject(phoneLine, new DirectIPDialParameters(new DirectIPDialInfo(remotePort, callerID)), remoteAddress);
                    call.CallStateChanged += call_CallStateChanged;
                    call.InstantMessageReceived += call_InstantMessageReceived;
                    Console.WriteLine("Starting Call...");
                    call.Start();
                });
                
            }
        }

        public void sendMessage(string message)
        {
            
            call.SendInstantMessage(message);
        }


        void call_InstantMessageReceived(object sender, InstantMessage e)
        {
            DispatchAsync(() =>
            {
                Console.Write("\nMessage received from {0}: {1}", e.Sender, e.Content);
                /*
                var handler = IncomingMessage;
                if (handler != null)
                    handler(this, e);
                */
            });

        }

        private void DispatchAsync(Action action)
        {
            var task = new WaitCallback(o => action.Invoke());
            ThreadPool.QueueUserWorkItem(task);
        }

    }


}