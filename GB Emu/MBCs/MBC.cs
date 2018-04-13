using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu.MBCs
{
    public struct CartridgeType
    {
        public CartridgeTypes CartridgeInfo;
        public bool RAM;
        public bool SRAM;
        public bool BATTERY;
        public bool RUMBLE;
        public bool TIMER;
    }

    public struct CartridgeInfo
    {
        public string ROMName;
        public LicenseeType LicenseeType;
        public byte LicenseeNew;
        public bool Japanese;
        public int MaskROMVersion;
        public bool GameBoyColor;
        public bool SuperGameBoy;
        public CartridgeType CartridgeType;
        public int RAMSize;
        public int RAMBanks;
        public int ROMSize;
        public int ROMBanks;
        public int Complement;
        public int CheckSum;

        public CartridgeInfo(string ROMName, LicenseeType LicenseeType, byte LicenseeNew, bool Japanese, int MaskROMVersion, bool GameBoyColor,
            bool SuperGameBoy, CartridgeType CartridgeType,
            int RAMSize, int RAMBanks, int ROMSize, int ROMBanks, int Complement, int Checksum)
        {
            this.ROMName = ROMName;
            this.LicenseeType = LicenseeType;
            this.LicenseeNew = LicenseeNew;
            this.Japanese = Japanese;
            this.MaskROMVersion = MaskROMVersion;
            this.GameBoyColor = GameBoyColor;
            this.SuperGameBoy = SuperGameBoy;
            this.CartridgeType = CartridgeType;
            this.RAMSize = RAMSize;
            this.RAMBanks = RAMBanks;
            this.ROMSize = ROMSize;
            this.ROMBanks = ROMBanks;
            this.Complement = Complement;
            this.CheckSum = Checksum;
        }
    }

    public abstract class MBC
    {
        public Memory MEMORY;

        public abstract void CheckWrite(int address, int value);

        public MBC(CartridgeInfo Info, byte[] data)
        {
            Cartridge = Info;
        }

        public CartridgeInfo Cartridge;

        public abstract void CopyROM0();
        public abstract void CopyROMS();

        
        public abstract void SwitchROM(int i);
        public abstract void SwitchRAM();


        public abstract byte ReadRAM(ushort address);
        public abstract void WriteRAM(ushort address, byte value);
    }
}
