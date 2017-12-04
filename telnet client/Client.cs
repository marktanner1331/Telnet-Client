using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace telnet_client
{
    //emulates the VT220
    class Client
    {
        public string address;
        public ushort port;

        private TcpClient client;
        private Stream stream;

        private bool fullDuplex = false;

        //only applies if fullDuplex == false
        private bool keyboardHasFocus = true;

        private bool echo = false;
        private bool bufferInput = true;

        private VT220 emulator = new VT220();

        public Client()
        {

        }

        public Client(string ipAddress)
        {
            this.address = ipAddress;
            this.port = 23;
        }

        public Client(string ipAddress, ushort port)
        {
            this.address = ipAddress;
            this.port = port;
        }

        public void connect()
        {
            if (Program.validateIPAddress(address) == false)
            {
                try
                {
                    IPAddress[] addresslist = Dns.GetHostAddresses(address);
                    if (addresslist.Length > 0)
                    {
                        emulator.WriteLine("Connecting to " + address + " " + port + " [" + addresslist[0].ToString() + "]");
                        Debug.WriteLine("Connecting to " + address + " " + port + " [" + addresslist[0].ToString() + "]");
                    }
                }
                catch
                {

                }
            }
            else
            {
                emulator.WriteLine("Connecting to " + address + " " + port);
                Debug.WriteLine("Connecting to " + address + " " + port);
            }

            try
            {
                client = new TcpClient(address, port);
                    
                stream = client.GetStream();
            }
            catch (Exception e)
            {
                emulator.WriteLine("Failed to Connect " + e.Message);
                Debug.WriteLine("Failed to Connect " + e.Message);
                return;
            }

            emulator.WriteLine("Successfully Connected");
            Debug.WriteLine("Successfully Connected");

            session();
        }

        private void processByte(byte b)
        {
            switch (b)
            {
                case 255:
                    processCommand();
                    break;
                case 27:
                    processVT220Command();
                    break;
                default:
                    emulator.Write((char)b);
                    break;
            }
        }

        private void session()
        {
            Thread keyThread = new Thread(delegate()
            {
                MemoryStream keyStream = new MemoryStream();
                while (true)
                {
                    if (client.Connected == false)
                    {
                        break;
                    }

                    if (Console.KeyAvailable == false)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    if (fullDuplex == false && keyboardHasFocus == false)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    char c = Console.ReadKey(true).KeyChar;
                    
                    if (bufferInput)
                    {
                        keyStream.WriteByte((byte)c);

                        if (c == 0x0d)
                        {
                            keyStream.WriteByte(0x0a);

                            keyStream.WriteTo(stream);
                            keyStream.Position = 0;
                            keyStream.SetLength(0);

                            keyboardHasFocus = false;
                        }
                    }
                    else
                    {
                        stream.WriteByte((byte)c);
                        if (c == 0x0d)
                        {
                            stream.WriteByte(0x0a);
                        }
                    }

                    //if the remote host is echoing the characters for us then we dont echo it ourselves
                    if (echo == false)
                    {
                        emulator.Write(c);
                        if (c == 0x0d)
                        {
                            Console.Write((char)0x0a);
                        }
                    }
                }
            });

            keyThread.Start();

            Thread printThread = new Thread(delegate()
            {
                while (client.Connected)
                {
                    try
                    {
                        byte b = (byte)stream.ReadByte();
                        processByte(b);
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }

                emulator.WriteLine("Connection Closed By Remote Terminal");
                Debug.WriteLine("Connection Closed By Remote Terminal");
            });

            printThread.Start();

            //make the main thread wait until the key thread has finished, i.e. when the session is over, control will go 
            //back to the main thread
            printThread.Join();
            keyThread.Join();
        }

        private void processVT220Command()
        {
            byte a = (byte)stream.ReadByte();

            if ((char)a != '[')
            {
                Debug.WriteLine("Unknown VT220 Control Character " + (char)a);
                return;
            }

            List<byte> command = new List<byte>();
            while (true)
            {
                a = (byte)stream.ReadByte();
                command.Add(a);

                if (((char)a >= '0' && (char)a <= '9') || (char)a == ';')
                {
                    //not the last character
                }
                else
                {
                    break;
                }
            }

            emulator.processControlSequenceIntroducer(command.ToArray());
        }

        /// <summary>
        /// expects the stream to be just past a 255 character
        /// </summary>
        private void processCommand()
        {
            byte commandType = (byte)stream.ReadByte();
            switch (commandType)
            {
                case Command.WILL:
                    processWILLCommand();
                    break;
                case Command.DO:
                    processDOCommand();
                    break;
                case Command.IAC:
                    //i think this means the server wants to stop the connection
                    client.Close();
                    break;
                case Command.SUB_NEGOTIATE: //Indicates that what follows is subnegotiation of the indicated option.
                    processSubNegotiation();
                    break;
                case Command.GO_AHEAD:
                    keyboardHasFocus = true;
                    break;
                case Command.NO_OPERATION:
                    break;
                case Command.WONT:
                    processWontCommand();
                    break;
                case 242: //The data stream portion of a Synch.
                case 243: //NVT character BRK.
                case 244: //The function IP.
                case 245: //The function AO.
                case 246: //The function AYT.
                case 247: //The function EC.
                case 248: //The function EL.
                case 254: //DONT: Indicates the demand that the other party stop performing, or confirmation that you are no longer expecting the other party to perform, the indicated option.
                default:
                    Debug.WriteLine("Unknown Command: " + commandType);
                    break;
            }
        }

        private void processSubNegotiation()
        {
            byte option = (byte)stream.ReadByte();

            switch (option)
            {
                case Option.TERMINAL_TYPE:
                    byte letsHopeItsASend = (byte)stream.ReadByte();
                    if (letsHopeItsASend == Command.SEND)
                    {
                        sendTerminalType();
                    }
                    else
                    {
                        Debug.WriteLine("Unknown Command Terminal Type " + letsHopeItsASend);
                    }

                    //the end sub neg, not going to bother checking these
                    stream.ReadByte();
                    stream.ReadByte();
                    break;
                default:
                    Debug.WriteLine("Unknown Sub Negotiation starting with " + option);
                    break;
            }
        }

        private void processDOCommand()
        {
            byte commandCode = (byte)stream.ReadByte();

            switch (commandCode)
            {
                case Option.SUPPRESS_GO_AHEAD: //suppress ga ahead
                    Debug.WriteLine("Server DO Suppress Go Ahead");
                    if (fullDuplex)
                    {
                        //ignore if we are already suppressing them
                        Debug.WriteLine("Client Ignore Server Will Suppress Go Ahead");
                        return;
                    }

                    Debug.WriteLine("Client Wont Suppress Go Ahead");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.WONT, Option.SUPPRESS_GO_AHEAD });
                    break;
                case Option.ECHO: //echo
                    Debug.WriteLine("Server DO Echo");
                    if (echo)
                    {
                        Debug.WriteLine("Client Ignore Server Do Echo");
                        //ignore if we are already echoing
                        return;
                    }

                    //not supporting echo'ing the the servers data back at him
                    Debug.WriteLine("Client Wont Echo");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.WONT, Option.ECHO });
                    break;
                case Option.NEGOTIATE_ABOUT_WINDOW_SIZE: //negotiate about window size
                    Debug.WriteLine("Server DO Negotiate About Window Size");
                    Debug.WriteLine("Client WILL Negotiate About Window Size");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.WILL, Option.NEGOTIATE_ABOUT_WINDOW_SIZE });
                    sendWindowSize();
                    break;
                case Option.TERMINAL_TYPE:
                    Debug.WriteLine("Server Do Terminal Type");
                    Debug.WriteLine("Client Will Terminal Type");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.WILL, Option.TERMINAL_TYPE });
                    break;
                default:
                    Debug.WriteLine("Unknown Server DO " + commandCode);
                    stream.WriteBytes(new byte[] { Command.IAC, Command.WONT, commandCode });
                    break;
            }
        }

        private void sendTerminalType()
        {
            // IAC SB TERMINAL-TYPE IS ... IAC SE
            //IS is 0
            Debug.WriteLine("Sending Terminal Type UNKNOWN");
            MemoryStream temp = new MemoryStream();
            temp.WriteBytes(new byte[] { Command.IAC, Command.SUB_NEGOTIATE, Option.TERMINAL_TYPE, Command.IS });
            temp.WriteBytes(Encoding.ASCII.GetBytes("UNKNOWN"));
            temp.WriteBytes(new byte[] { Command.IAC, Command.END_SUBNEG });
            temp.WriteTo(stream);
        }

        private void processWILLCommand()
        {
            byte commandCode = (byte)stream.ReadByte();

            switch (commandCode)
            {
                case Option.SUPPRESS_GO_AHEAD: //suppress ga ahead
                    Debug.WriteLine("Server WILL Supress Go Ahead");
                    if (fullDuplex)
                    {
                        Debug.WriteLine("Client Ignore Server Will Suppress Go Ahead");
                        //ignore if we are already suppressing them
                        return;
                    }

                    Debug.WriteLine("Client Dont Suppress Go Ahead");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DONT, Option.SUPPRESS_GO_AHEAD });
                    break;
                case Option.ECHO: //echo
                    Debug.WriteLine("Server WILL Echo");
                    if (echo)
                    {
                        //ignore if we are already echoing
                        Debug.WriteLine("Client Ignore Server WILL Echo");
                        return;
                    }

                    echo = true;

                    //probably best not to buffer the data, the user wont see what they have typed until they have carriage returned
                    bufferInput = false;

                    Debug.WriteLine("Client Do Echo");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DO, Option.ECHO });
                    break;
                case Option.NEGOTIATE_ABOUT_WINDOW_SIZE: //negotiate about window size
                    Debug.WriteLine("Server Will Negotiate About Window Size");
                    Debug.WriteLine("Client Dont Negotiate About Window Size");

                    //doesnt make sense for the server to send me stuff about its window so im sending back a DONT
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DONT, Option.NEGOTIATE_ABOUT_WINDOW_SIZE });
                    break;
                default:
                    Debug.WriteLine("Unknown Server WILL " + commandCode);
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DONT, commandCode });
                    break;
            }
        }

        private void processWontCommand()
        {
            byte commandCode = (byte)stream.ReadByte();

            switch (commandCode)
            {
                case Option.ECHO: //echo
                    Debug.WriteLine("Server WONT Echo");
                    if (echo == false)
                    {
                        //ignore if we are already echoing
                        Debug.WriteLine("Client Ignore Server WONT Echo");
                        return;
                    }

                    echo = false;

                    //start buffering the data again
                    bufferInput = true;

                    Debug.WriteLine("Client DONT Echo");
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DONT, Option.ECHO });
                    break;
                default:
                    Debug.WriteLine("Unknown Server WONT " + commandCode);
                    stream.WriteBytes(new byte[] { Command.IAC, Command.DONT, commandCode });
                    break;
            }
        }

        private void sendWindowSize()
        {
            MemoryStream temp = new MemoryStream();

            //IAC SB NAWS <16-bit value> <16-bit value> IAC SE
            temp.WriteBytes(new byte[] { Command.IAC, Command.SUB_NEGOTIATE, Option.NEGOTIATE_ABOUT_WINDOW_SIZE });

            ushort width = (ushort)Console.WindowWidth;
            ushort height = (ushort)Console.WindowHeight;

            Debug.WriteLine("Sending Window Size " + width + " " + height);

            temp.WriteBytes(escapeIAC(EndianConverter.hostToNetworkOrder(width)));
            temp.WriteBytes(escapeIAC(EndianConverter.hostToNetworkOrder(height)));
            temp.WriteBytes(new byte[] { Command.IAC, Command.END_SUBNEG });

            temp.WriteTo(stream);
        }

        private void sendGoAhead()
        {
            Debug.WriteLine("Sending Go Ahead");
            stream.WriteBytes(new byte[] { Command.IAC, Command.GO_AHEAD });
        }

        private byte[] escapeIAC(byte[] value)
        {
            List<byte> escapedValue = new List<byte>();
            for (int i = 0; i < value.Length; i++)
            {
                escapedValue.Add(value[i]);
                if (value[i] == 255)
                {
                    escapedValue.Add(255);
                }
            }

            return escapedValue.ToArray();
        }
    }
}
