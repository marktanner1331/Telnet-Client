using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telnet_client
{
    class Option
    {
        public const byte ECHO = 1;
        public const byte SUPPRESS_GO_AHEAD = 3;
        public const byte TERMINAL_TYPE = 24;
        public const byte NEGOTIATE_ABOUT_WINDOW_SIZE = 31;  
    }
}
