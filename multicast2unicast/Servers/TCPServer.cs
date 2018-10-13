using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace multicast2unicast
{
    class TCPServer
    {
        public TCPServer(string _ip, int _port, byte[] data)
        {
            IPAddress ip;
            IPAddress.TryParse(_ip, out ip);
            int port = _port;
            Server(port, data);
        }
        public void Server(int port, byte[] data)
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            // we set our IP address as server's address, and we also set the port: 9999

            server.Start();  // this will start the server

            while (true)   //we wait for a connection
            {
                TcpClient client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

                NetworkStream ns = client.GetStream(); //networkstream is used to send/receive messages

                byte[] hello = new byte[100];   //any message must be serialized (converted to byte array)
                hello = Encoding.Default.GetBytes("hello world");  //conversion string => byte array

                //ns.Write(hello, 0, hello.Length);     //sending the message
                ns.Write(data, 0, data.Length);     //sending the message

                while (client.Connected)  //while the client is connected, we look for incoming messages
                {
                    Console.WriteLine("Client Connected");
                    //ns.Write(data, 0, data.Length);
                }
            }
        }
    }
}