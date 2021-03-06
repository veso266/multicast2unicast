﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Cinegy.TsDecoder.TransportStream;


namespace multicast2unicast
{
    class Program
    {
        static protected UdpClient listener;
        static protected IPAddress localAddress;
        static protected IPAddress listenAddress;
        static protected int listenPort;

        static protected IPEndPoint inPoint;

        byte[] data;

        // SLO 3: 232.2.1.41:5002

        public static void MulticastListen(string ip, int port, string interface2bind)
        {
            IPAddress.TryParse(ip, out listenAddress); //Our program that we want to recieve
            listenPort = port;
            IPAddress.TryParse(interface2bind, out localAddress); //To know to which interface to bind
            
            //IPAddress.TryParse("192.168.88.12", out localAddress); //To know to which interface to bind
            //IPAddress.TryParse("232.2.1.41", out listenAddress); //Our program that we want to recieve
            listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            inPoint = new IPEndPoint(localAddress, listenPort);
            listener.Client.Bind(inPoint);
            listener.JoinMulticastGroup(listenAddress);
        }

        public void loop()
        {
            bool done = false;
            try
            {
                while (!done)
                {
                    byte[] bytes = listener.Receive(ref inPoint);
                    //Console.WriteLine("Received Multicast from  {0} : length {1}\n", listenAddress.ToString(), bytes.Length);
                    //Lets get to buisness now
                    rtp _rtp = new rtp(bytes, bytes.Length);
                    
                    if (_rtp.RTP_verify())
                    {
                        //_rtp.RTP_check();
                        byte[] RTPPayload = _rtp.RTP_process();

                        TCPServer _tcp = new TCPServer("192.168.88.12", 1234, RTPPayload);





                        //Console.WriteLine("RTP Packet Length: " + bytes.Length);
                        //Console.WriteLine("RTP Payload Length: " + RTPPayload.Length);
                    }
                    //Lets get to buisness now






                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
                //sender.Close();
            }
        }

        static void Main(string[] args)
        {
            Program _Program = new Program();
            //rtp _rtp = new rtp();
            //MulticastListen("232.2.1.41", 5002, "192.168.88.12"); //SLO 3 (udp)
            MulticastListen("232.2.201.53", 5003, "192.168.88.12"); //SLO 1 (rtp)
            _Program.loop(); //To recieve packest continuously
            //_rtp.RTP_check(_Program.data, _Program.data.Length);
            Console.ReadLine();

        }
    }
}
