using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu.MBCs
{
    class ROM : MBC
    {
        byte[][] RAMBanks;
        byte[][] ROMBanks;

        public ROM(CartridgeInfo Info, byte[] data) : base(Info, data)
        {
            if (Info.CartridgeType.RAM)
            {
                RAMBanks = new byte[1][];
                RAMBanks[0] = new byte[0x2000];
            }
            ROMBanks = new byte[2][];
            ROMBanks[0] = GetRange(data, 0, 0x3fff);
            ROMBanks[1] = GetRange(data, 0x4000, 0x7ffF);
        }

        public override void CheckWrite(int address, int value)
        {
        }

        public override void CopyROM0()
        {
            for( var i = 0; i < ROMBanks[0].Length; i++)
            {
                MEMORY.memory[i] = ROMBanks[0][i];
            }
        }

        public override void CopyROMS()
        {
            for (var i = 0; i < ROMBanks[1].Length; i++)
            {
                MEMORY.memory[i+0x4000] = ROMBanks[1][i];
            }
        }


        public override void SwitchRAM()
        {
            throw new NotImplementedException();
        }

        public override void SwitchROM(int i)
        {
            throw new NotImplementedException();
        }

        public override void WriteRAM(ushort address, byte value)
        {
            if (Cartridge.CartridgeType.RAM) RAMBanks[0][address] = value;
            else throw new Exception("NO RAM IN CARTRIDGE!");
        }
        public override byte ReadRAM(ushort address)
        {
            if (Cartridge.CartridgeType.RAM) return RAMBanks[0][address];
            else throw new Exception("NO RAM IN CARTRIDGE!");
        }

        private static T[] GetRange<T>(T[] original, int from, int to)
        {
            T[] destination = new T[to - from+1];
            Array.Copy(original, from, destination, 0, to - from+1);
            return destination;
        }
    }
}
