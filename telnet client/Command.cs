using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telnet_client
{
    class Command
    {
        public const byte IS = 0;
        public const byte SEND = 1;
        public const byte END_SUBNEG = 240;
        public const byte NO_OPERATION = 241;
        public const byte DATA_MARK = 242;
        public const byte BREAK = 243;
        public const byte INT_PROCESS = 244;
        public const byte ABORT_OUTPUT = 245;
        public const byte YOU_THERE = 246;
        public const byte ERASE_CHAR = 247;
        public const byte ERASE_LINE = 248;
        public const byte GO_AHEAD = 249;
        public const byte SUB_NEGOTIATE = 250;
        public const byte WILL = 251;
        public const byte WONT = 252;
        public const byte DO = 253;
        public const byte DONT = 254;
        public const byte IAC = 255;
    }
}
