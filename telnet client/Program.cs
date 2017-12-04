using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telnet_client
{
    class Program
    {
        const string MODE_MAIN = "main";
        const string MODE_CONNECTED = "connected";


        static string mode = MODE_MAIN;
        static Client client;

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                connect(args, 0);
            }

            while (true)
            {
                Console.Write("telnet>");
                string line = Console.ReadLine();
                string[] words = line.Split(' ');

                switch (words[0].ToLower())
                {
                    case "o":
                        connect(words, 1);
                        break;
                    case "places":
                        printPlaces(words, 1);
                        break;
                    case "?":
                    case "help":
                        printHelp();
                        break;
                    default:
                        Console.WriteLine("Unknown Command, type \"?\" or \"help\" for information");
                        break;
                }
            }
        }

        static void printHelp()
        {
            Console.WriteLine();
            printOption("o", "Connect to specified Host [and port].");
            printOption("places", "Display a list of common places to connect to.");
            printOption("?", "Shows this screen.");
            printOption("help", "Shows this screen.");
            Console.WriteLine();
        }

        static void printOption(string name, string value)
        {
            Console.WriteLine(name + "\t\t\t" + value);
        }

        static void printPlaces(string[] arguments, int startIndex)
        {
            string line;
            if (startIndex < arguments.Length)
            {
                line = arguments[startIndex];
            }
            else
            {
                printOption("0", "back");
                printOption("1", "New Moon");
                printOption("2", "The BOFH Excuse Server");
                printOption("3", "Star Wars");
                line = Console.ReadLine();
            }

            //not sure why im bothering to parse this when i just switch it later
            int option = 0;
            if (int.TryParse(line, out option) == false)
            {
                return;
            }

            switch (option)
            {
                case 0:
                    return;
                case 1:
                    connect(new string[] { "eclipse.cs.pdx.edu:7680" }, 0);
                    return;
                case 2:
                    connect(new string[] { "towel.blinkenlights.nl:666" }, 0);
                    return;
                case 3:
                    connect(new string[] { "towel.blinkenlights.nl" }, 0);
                    return;
            }
        }

        static void connect(string[] args, int startIndex)
        {
            string address;
            ushort port;

            switch (args.Length - startIndex)
            {
                case 0:
                    Console.WriteLine("Please Specify An IP Address");
                    return;
                case 1:
                    address = args[startIndex];
                    port = 23;

                    if (address.Contains(":"))
                    {
                        string[] parts = address.Split(':');
                        address = parts[0];
                        ushort.TryParse(parts[1], out port);
                    }
                    break;
                case 2:
                     address = args[startIndex];
                     ushort.TryParse(args[startIndex + 1], out port);
                     break;
                default:
                    Console.WriteLine("Unknown Arguments");
                    return;
            }

            //if it failed to parse the port, it would be 0
            if (port == 0)
            {
                Console.WriteLine("Invalid Port");
                return;
            }

            client = new Client(address, port);
            mode = MODE_CONNECTED;
            client.connect();
            mode = MODE_MAIN;
        }

        public static bool validateIPAddress(string ipAddressString)
        {
            string[] parts = ipAddressString.Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            byte[] ipAddress = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (byte.TryParse(parts[i], out ipAddress[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
