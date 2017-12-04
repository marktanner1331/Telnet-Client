using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telnet_client
{
    class EndianConverter
    {
        public static byte[] hostToNetworkOrder(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            return hostToNetworkOrder(bytes);
        }

        public static byte[] hostToNetworkOrder(byte[] value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value.Reverse().ToArray();
            }

            return value;
        }
    }
}
