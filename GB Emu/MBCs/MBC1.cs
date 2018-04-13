using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu.MBCs
{
    class MBC1 : MBC
    {
        byte[][] RAMBanks;
        byte[][] ROMBanks;
        int selectedROMBank = 1;
        int selectedRAMBank = 0;
        bool RAMOn = false;
        bool ROMMode = true;
        CartridgeInfo info;

        public MBC1(CartridgeInfo Info, byte[] data) : base(Info, data)
        {
            this.info = Info;
            if (Info.CartridgeType.RAM)
            {
                RAMBanks = new byte[Info.RAMBanks][];
                for (int i = 0; i < Info.RAMBanks; i++)
                {
                    RAMBanks[i] = new byte[(Info.RAMSize/Info.RAMBanks)];
                }
            }
            ROMBanks = new byte[Info.ROMBanks][];
            for (int i = 0; i < Info.ROMBanks; i++)
            {
                ROMBanks[i] = GetRange(data, i*Info.ROMSize / info.ROMBanks, (i+1)*(Info.ROMSize / info.ROMBanks) -1);
            }
        }

        public override void CheckWrite(int address, int value)
        {
            if ((address >= 0) && (address < 0x2000))
            {
                if (value == 0) RAMOn = false;
                else if (value == 0x0A) RAMOn = true;
            }
            else if ((address >= 0x2000) && (address < 0x4000))
            {
                byte info = (byte)(value & 0b00011111);
                if (info == 0) info = 1;
                selectedROMBank &= 0b11100000;
                selectedROMBank |= info;
                CopyROMS();
            }
            else if ((address >= 0x4000) && (address < 0x6000))
            {
                if (ROMMode)
                {
                    byte info = (byte)(value & 0b00000011);
                    info = (byte)(info << 5);
                    selectedROMBank &= 0b00011111;
                    selectedROMBank |= info;
                    CopyROMS();
                }
                else selectedRAMBank = value;
            }
            else if ((address >= 0x6000) && (address < 0x8000))
            {
                if (value == 0) ROMMode = true;
                else if (value == 1) ROMMode = false;
            }
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
            if (info.ROMBanks > 1)
            {
                for (var i = 0; i < ROMBanks[selectedROMBank].Length; i++)
                {
                    MEMORY.memory[i + 0x4000] = ROMBanks[selectedROMBank][i];
                }
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
            if (RAMOn)
            {
                if (Cartridge.CartridgeType.RAM) RAMBanks[selectedRAMBank][address] = value;
                else throw new Exception("NO RAM IN CARTRIDGE!");
            }
        }
        public override byte ReadRAM(ushort address)
        {
            if (RAMOn)
            {
                if (Cartridge.CartridgeType.RAM) return RAMBanks[selectedRAMBank][address];
                else throw new Exception("NO RAM IN CARTRIDGE!");
            }
            else return 0;
        }

        private static T[] GetRange<T>(T[] original, int from, int to)
        {
            T[] destination = new T[to - from+1];
            Array.Copy(original, from, destination, 0, to - from+1);
            return destination;
        }
    }
}
