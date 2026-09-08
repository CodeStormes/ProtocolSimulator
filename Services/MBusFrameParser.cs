using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtocolSimulator.Services
{
    public static class MbusFrameParser
    {
        public static bool TryTakeFrame(
            List<byte> buffer,
            out byte[] frame)
        {
            frame = Array.Empty<byte>();

            while (true)
            {
                while (buffer.Count > 0 &&
                       buffer[0] != 0x68 &&
                       buffer[0] != 0x10 &&
                       buffer[0] != 0xE5)
                {
                    buffer.RemoveAt(0);
                }

                if (buffer.Count == 0)
                    return false;

                if (buffer[0] == 0xE5)
                {
                    frame = new byte[] { 0xE5 };
                    buffer.RemoveAt(0);
                    return true;
                }

                if (buffer[0] == 0x10)
                {
                    if (buffer.Count < 5)
                        return false;

                    if (buffer[4] != 0x16 ||
                        CalculateChecksum(buffer, 1, 2) != buffer[3])
                    {
                        buffer.RemoveAt(0);
                        continue;
                    }

                    frame = buffer.Take(5).ToArray();
                    buffer.RemoveRange(0, 5);
                    return true;
                }

                if (buffer.Count < 4)
                    return false;

                if (buffer[1] != buffer[2] || buffer[3] != 0x68)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                int frameLength = buffer[1] + 6;

                if (buffer.Count < frameLength)
                    return false;

                if (buffer[frameLength - 1] != 0x16 ||
                    CalculateChecksum(buffer, 4, buffer[1]) !=
                    buffer[frameLength - 2])
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                frame = buffer.Take(frameLength).ToArray();
                buffer.RemoveRange(0, frameLength);
                return true;
            }
        }

        private static byte CalculateChecksum(
            List<byte> bytes,
            int offset,
            int count)
        {
            int sum = 0;

            for (int index = offset; index < offset + count; index++)
                sum += bytes[index];

            return (byte)sum;
        }
    }
}