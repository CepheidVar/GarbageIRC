using System;
using System.Threading;
using System.Collections.Generic;

namespace IRCClient
{
    class Program
    {
        private ClientConnection Connection;
        private string Nickname = null;
        private bool LoggedIn = false;
        private Queue<string> Log = new Queue<string>();
        private string Channel = "";

        public Program() {
            Connection = new ClientConnection();
            Connection.OnMessageReceived += OnMessageReceivedHandler;
        }

        public void Connect(string address, int port) {
            Connection.Connect(address, port);
        }

        public void OnMessageReceivedHandler(IRCMessage message) {
            switch(message.Command) {
                case "NOTICE":
                    Log.Enqueue($"NOTICE from {message.Prefix}: {message.Parameters[1]}");
                break;
                case "PRIVMSG":
                    Log.Enqueue($"PRIVMSG from {message.Prefix}: {message.Parameters[1]}");
                break;
                case "PING":
                    Log.Enqueue($"PING {message.Parameters[0]}!");
                    SendPONG(message.Parameters[0]);
                break;
                case "004":
                    Log.Enqueue("Logged in.");

                    SendJOIN(Channel);
                    LoggedIn = true;
                break;
                default:
                    Log.Enqueue($"Unhandled command {message.Command}");
                    break;
            }

            PrintLog();
        }

        public void SendNICK(string nickname) {
            string message = null;
            if (Nickname == null) {
                // First connection.
                message = $"NICK {nickname}\r\n";
            }
            else {
                message = $":{Nickname} NICK {nickname}";
            }

            Connection.SendMessage(message);

            //  If response is valid, set Nickname to current nickname.
        }

        public void SendUSER(string username, string realname) {
            string message = $"USER {username} 0 * :{realname}\r\n";

            Connection.SendMessage(message);
        }

        public void SendPONG(string pong) {
            string message = $"PONG {pong}\r\n";

            Connection.SendMessage(message);
        }

        public void SendJOIN(string channel) {
            string message = $"JOIN {channel}\r\n";

            Connection.SendMessage(message);
        }

        public void SendPRIVMSG(string message) {
            string toSend = $"PRIVMSG {Channel} :{message}";

            Connection.SendMessage(toSend);
        }

        public void PrintLog() {
            Console.Clear();
            foreach(string s in Log) {
                Console.WriteLine(s);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 6) {
                return;
            }

            Console.WriteLine("Huh?");

            if (!int.TryParse(args[1], out int port)) {
                return;
            }

            Program p = new Program();

            p.Connect(args[0], port);
            p.SendNICK(args[2]);
            p.SendUSER(args[3], args[4]);
            p.Channel = args[5];


            string input = "";

            while (p.Connection.Connected) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace) {
                    if (input.Length > 0) {
                        input = input[0..^1];
                    }
                }
                else if (key.Key == ConsoleKey.Enter) {
                    p.SendPRIVMSG(input);
                    input = "";
                }
                else {
                    input += key.KeyChar;
                }

                p.PrintLog();
                Console.SetCursorPosition(0, Console.WindowHeight);
                Console.Write(input);
            }
        }
    }
}
