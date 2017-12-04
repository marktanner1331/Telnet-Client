using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telnet_client
{
    class VT220
    {
        public void Write(char c)
        {
            Console.Write(c);
        }

        public void Write(string s)
        {
            Console.WriteLine(s);
        }

        public void WriteLine(string s)
        {
            Console.WriteLine(s);
        }

        public void processControlSequenceIntroducer(byte[] command)
        {
            switch ((char)command.Last())
            {
                case 'h':
                    if ((char)command[0] == '?')
                    {
                        processDECSET(command);
                    }
                    else
                    {
                        Debug.WriteLine("Haven't implemented Set Mode in VT220 Emulation");
                    }
                    break;
                case 'm':
                    if ((char)command[0] != '>')
                    {
                        processSGR(command);
                    }
                    else
                    {
                        Debug.WriteLine("Havent implemented Resource Value Set / Reset in VT220 Emulation");
                    }
                    break;
                case 'H':
                    processCUP(command);
                    break;
                case 'J':
                    processDECSED(command);
                    break;
                default:
                    Debug.WriteLine("Unknown VT220 Control Sequence Introducer ending in " + (char)command.Last() + " (" + command.Last() + ")");
                    
                    break;
            }
        }

        //clear screen
        private void processDECSED(byte[] command)
        {
            uint[] arguments = readArgumentsFromCommand(command, 0);

            uint mode = arguments.Length > 0 ? arguments[0] : 0;

            int left = Console.CursorLeft;
            int top = Console.CursorTop;

            switch (mode)
            {
                case 0: //from the cursor to the end
                    while (Console.CursorTop <= Console.WindowHeight && Console.CursorLeft < Console.WindowWidth)
                    {
                        Console.Write(" ");
                    }

                    Console.CursorLeft = left;
                    Console.CursorTop = top;
                    break;
                case 1: //from the beginning to the cursor
                    Console.CursorLeft = 0;
                    Console.CursorTop = 0;

                    while (Console.CursorLeft != left && Console.CursorTop != top)
                    {
                        Console.Write(" ");
                    }
                    break;
                case 2: //all of it
                    Console.Clear();
                    break;
            }
        }

        //cursor position
        private void processCUP(byte[] command)
        {
            uint[] arguments = readArgumentsFromCommand(command, 0);

            uint column = arguments.Length > 0 ? arguments[0] : 1;
            uint row = arguments.Length > 1 ? arguments[1] : 1;

            //i have a feeling that its 1 based
            column--;
            row--;

            Console.CursorLeft = (int)column;
            Console.CursorTop = (int)row;
        }

        //Character Attributes. (sgr stands for select graphic rendition)
        private void processSGR(byte[] command)
        {
            uint[] arguments = readArgumentsFromCommand(command, 0);

            //really guessing here
            foreach (uint argument in arguments)
            {
                setConsoleStyle(argument);
            }
        }

        private void setConsoleStyle(uint value)
        {
            switch (value)
            {
                case 1: //bold
                    break;
                case 30:
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case 31:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 32:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 33:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 34:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case 35:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 36:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case 37:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case 39:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case 40:
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case 41:
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                case 42:
                    Console.BackgroundColor = ConsoleColor.Green;
                    break;
                case 43:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    break;
                case 44:
                    Console.BackgroundColor = ConsoleColor.Blue;
                    break;
                case 45:
                    Console.BackgroundColor = ConsoleColor.Magenta;
                    break;
                case 46:
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    break;
                case 47:
                    Console.BackgroundColor = ConsoleColor.Gray;
                    break;
                case 49:
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                default:
                    Debug.WriteLine("Unknown VT220 SGR Command " + value);
                    break;
            }
        }

        private uint[] readArgumentsFromCommand(byte[] command, int startIndex)
        {
            List<uint> arguments = new List<uint>();
            uint currentArgument = 0;
            bool hasReadData = false;

            for (int i = startIndex; i < command.Length - 1; i++)
            {
                if ((char)command[i] == ';')
                {
                    arguments.Add(currentArgument);
                    currentArgument = 0;
                    hasReadData = false;
                }
                else
                {
                    currentArgument *= 10;
                    currentArgument += uint.Parse(((char)command[i]).ToString());
                    hasReadData = true;
                }
            }

            if (hasReadData)
            {
                arguments.Add(currentArgument);
            }

            return arguments.ToArray();
        }

        private void processDECSET(byte[] command)
        {
            //only a single multi character value here, i.e. no ; seperated values
            uint p = readArgumentsFromCommand(command, 1)[0];

            switch (p)
            {
                case 25:
                    Console.CursorVisible = true;
                    break;
                default:
                    Debug.WriteLine("Unknown VT220 DECSET Command " + p);
                    break;
            }
        }
    }
}
