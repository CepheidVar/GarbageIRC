using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GarbageIRC {
    public delegate void MessageReceived(IRCMessage message);

    public class ClientConnection {
        private static readonly int BUFFER_SIZE = 512;
        private TcpClient Connection;
        private NetworkStream ServerStream;
        private Thread ServerThread = null;
        public bool Connected { get; private set; } = false;
        private bool StopThread = false;

        public event MessageReceived OnMessageReceived;

        public ClientConnection() {
            Connection = new TcpClient();
        }

        public void ServerThreadFunc() {
            LineBuffer lb = new LineBuffer();
            while (!StopThread) {
                do {
                    byte[] bytes = new byte[BUFFER_SIZE];
                    int bytesRead = ServerStream.Read(bytes);
                    lb.AddData(bytes, bytesRead);
                } while (ServerStream.DataAvailable && !StopThread);

                while (lb.LineAvailable && !StopThread) {
                    string line = lb.GetNextLine();
                    IRCMessage message = new IRCMessage(line);
                    lock(this) {
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
        }
        
        public void Connect(string address, int port) {
            lock (this) {
                if (Connected) {
                    Disconnect();
                }

                if (!Connected) {
                    Connection.Connect(address, port);
                    ServerStream = Connection.GetStream();
                    Connected = true;

                    if (ServerThread == null) {
                        ServerThread = new Thread(ServerThreadFunc);
                    }

                    ServerThread.Start();
                }
            }
        }

        public void Disconnect() {
            lock (this) {
                if (Connected) {
                    Connection.Close();
                    ServerStream.Close();
                    ServerStream = null;

                    StopThread = true;
                    ServerThread.Join();

                    StopThread = false;
                    Connected = false;
                }
            }
        }

        public void SendMessage(string message) {
            message += "\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            int toWrite = bytes.Length;
            if (toWrite > 512) {
                bytes[510] = 0x0d;
                bytes[511] = 0x0a;    
                toWrite = 512;            
            }

            ServerStream.Write(bytes, 0, toWrite);
        }
    }
}