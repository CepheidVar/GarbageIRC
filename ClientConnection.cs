using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IRCClient {
    public delegate void MessageReceived(IRCMessage message);

    public class ClientConnection {
        private TcpClient Connection;
        private NetworkStream ServerStream;
        private Thread ServerThread;
        public bool Connected { get; private set; } = false;

        public event MessageReceived OnMessageReceived;

        public ClientConnection() {
            Connection = new TcpClient();
        }

        public void ServerThreadFunc() {
            byte[] bytes = new byte[512];
            List<byte> data = new List<byte>();
            List<byte> stringBytes = new List<byte>();
            while (Connected) {
                data.Clear();
                stringBytes.Clear();
                if (Connection.Connected) {
                    int bytesRead = 0;
                    bytesRead = ServerStream.Read(bytes);
                    for (int i = 0; i < bytesRead; i++) {
                        data.Add(bytes[i]);
                    }

                    while (ServerStream.DataAvailable) {
                        bytesRead = ServerStream.Read(bytes);
                        for (int i = 0; i < bytesRead; i++) {
                            data.Add(bytes[i]);
                        }
                    }
                }

                if (!Connection.Connected) {
                    return;
                }

                bool doneSplitting = false;
                while (!doneSplitting) {
                    stringBytes.Clear();
                    bool foundString = false;

                    //  We got all the data that can be read, so see if any strings are in it.
                    for (int i = 0; i < data.Count && !foundString; i++) {
                        if (i + 1 < data.Count && data[i] == 0x0d && data[i + 1] == 0x0a) {
                            data.RemoveRange(0, i + 2);
                            string decoded = Encoding.UTF8.GetString(stringBytes.ToArray());
                            IRCMessage ircMessage = new IRCMessage(decoded);
                            foundString = true;

                            lock (this) {
                                OnMessageReceived?.Invoke(ircMessage);
                            }
                        }
                        else {
                            stringBytes.Add(data[i]);
                        }
                    }

                    //  Escape loop if remaining data is incomplete.
                    if (!foundString) {
                        doneSplitting = true;
                    }
                }
            }
        }
        
        public void Connect(string address, int port) {
            lock (this) {
                if (!Connected) {
                    Console.WriteLine("Connecting to " + address + ":" + port);
                    Connection.Connect(address, port);
                    Console.WriteLine("Connected.");
                    ServerStream = Connection.GetStream();
                    Connected = true;

                    ServerThread = new Thread(ServerThreadFunc);
                    ServerThread.Start();
                }
            }
        }

        public void Disconnect() {
            lock (this) {
                if (Connected) {
                    Connection.Close();
                    Connected = false;
                }
            }

            ServerThread.Join();
            Console.WriteLine("Connection closed.");
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