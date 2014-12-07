using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Popgun.Net
{
    public class Packet
    {
        private byte[] Data;
        public Opcodes Opcode { get; private set; }
        public static String PacketHeader = "POPGUN";
        private BinaryWriter writer;
        private BinaryReader reader;
        private MemoryStream stream;

        public Packet(byte[] data)
        {
            Data = data;
            stream = new MemoryStream(Data);
            reader = new BinaryReader(stream);
            if (!reader.ReadChars(PacketHeader.Length).SequenceEqual(PacketHeader))
                throw new Exception("Unknown packet received");
            Opcode = (Opcodes)reader.ReadUInt32();
        }

        public Packet(Opcodes opcode, int size)
        {
            Data = new byte[size + PacketHeader.Length + 4];
            Opcode = opcode;
            stream = new MemoryStream(Data);
            writer = new BinaryWriter(stream);
            writer.Write(PacketHeader.ToCharArray());
            writer.Write((UInt32)opcode);
        }

        public byte[] GetData()
        {
            writer.Flush();
            return Data;
        }

        public void WriteUInt(uint value)
        {
            writer.Write(value);
        }

        public uint ReadUInt()
        {
            return reader.ReadUInt32();
        }
    }
}
