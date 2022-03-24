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
        public MediaType mediaType;
        private string mediaParameters;
        //Event handlers
        public event EventHandler IncomingCall;
        public event EventHandler<RegistrationStateChangedArgs> RegistrationReady;
        public event EventHandler<CallStateChangedArgs> CallStateChanged;
        public event EventHandler<InstantMessage> MessageReceived;

        public VoIPHandler(string sipID, string sipAddress, Int32 localPort, string localIpAddress)
        {
            softphone = SoftPhoneFactory.CreateSoftPhone(MINPORT, MAXPORT);
            this.sipID = sipID;
            this.sipAddress = sipAddress;
            this.localIpAddress = localIpAddress;
            this.localPort = localPort;

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
                isRegistered = true;
                DispatchAsync(() =>
                {
                    var handler = RegistrationReady;
                    if (handler != null)
                        handler(this, e);
                });
            }
        }

        void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            call = e.Item;
            caller = call.DialInfo.CallerID;
            incomingCall = true;
            call.CallStateChanged += call_CallStateChanged;
            call.InstantMessageReceived += call_InstantMessageReceived;

            DispatchAsync(() =>
            {
                var handler = IncomingCall;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            });
        }

        public void AcceptCall()
        {
            if (incomingCall && call != null)
            {
                incomingCall = false;
                call.Answer();
            }
        }

        public void HangUp()
        {
            if(call != null)
            {
                call.HangUp();
                call = null;
            }
        }

        void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            //Console.WriteLine("Call state: {0}.", e.State);

            if(e.State == CallState.Error)
            {
                Console.WriteLine(e.ToString());
            }

            if (e.State == CallState.Answered)
            {
                
                DispatchAsync(() =>
                {
                    caller = call.DialInfo.CallerID;
                    mediaReceiver.AttachToCall(call);

                    var handler = CallStateChanged;
                    if (handler != null)
                        handler(this, e);
                });

            }

            if (e.State.IsCallEnded())
            {
                Console.WriteLine("Call Terminated");
            }
        }

        public void initializeAudioPlayers(MediaType mediaType, string parameters)
        {
            HardwareAudioHandler.initPlaybackDevice(mediaType, parameters);
            Console.WriteLine("Playback initialized");
            HardwareAudioHandler.connectPlaybackDeviceToSender(ref mediaSender, mediaType);
            Console.WriteLine("Playback connected");
            mediaSender.AttachToCall(call);
            Console.WriteLine("Playback attatched to call.");
        }

        public void closeAudioPlayers()
        {
            mediaReceiver.Detach();
            mediaSender.Detach();
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
                var handler = IncomingMessage;
                if (handler != null)
                    handler(this, e);
            });

        }

        private void DispatchAsync(Action action)
        {
            var task = new WaitCallback(o => action.Invoke());
            ThreadPool.QueueUserWorkItem(task);
        }

    }


}