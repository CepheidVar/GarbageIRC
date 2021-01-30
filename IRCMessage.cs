using System.Collections.Generic;
using System;

namespace GarbageIRC {
    public class IRCMessage {
        public string Prefix { get; private set; } = "";
        public bool HasPrefix { get; private set; } = false;
        
        public string Command { get; private set; } = "";

        private List<string> _Parameters = new List<string>();
        public IReadOnlyList<string> Parameters { get => _Parameters; }

        public IRCMessage(string message) {
            string[] parts = message.Split(" ", 2);

            int spcIdx = 0;
            if (parts[0].StartsWith(":")) {
                HasPrefix = true;
                Prefix = parts[0][1..];
                //  Remove the bits we just read

                spcIdx = message.IndexOf(" ");
                message = message.Remove(0, spcIdx + 1);
            }

            parts = message.Split(" ", 2);

            Command = parts[0];
            spcIdx = message.IndexOf(" ");
            message = message.Remove(0, spcIdx + 1);

            //  Parameters is set based on the message.
            switch(Command) {
                case "NOTICE":
                case "PRIVMSG":
                    // Only two parameters, the target, the message.
                    {
                        parts = message.Split(":", 2, StringSplitOptions.TrimEntries);
                        _Parameters.AddRange(parts);
                    }
                break;
                case "PING":
                    //  Only one parameter.
                    {
                        parts = message.Split(":", 2, StringSplitOptions.TrimEntries);
                        _Parameters.Add(parts[1]);
                    }
                break;
                default:
                    { 
                        parts = message.Split(":");
                    }
                break;
            }
        }
    }
}