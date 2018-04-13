using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu
{
    public enum CartridgeTypes { ROM, MBC1, MBC2, MMMD1, MBC3, MBC5, PocketCamera, Bandai, Hudson };
    public enum LicenseeType { New, Accolade, Konami, Unknown };
    
    public static class MBCFactory
    {

        public static MBCs.MBC Build(byte[] Cartridge)
        {
            string RomName = Encoding.ASCII.GetString(GetRange(Cartridge, 0x134, 0x142)).Replace("\0", "");
            bool color = (Cartridge[0x0143] == 0x30);
            char LicenseeHigh = (char)Cartridge[0x0144];
            char LicenseeLow = (char)Cartridge[0x0145];
            byte LicenseeNew = 0;
            if (LicenseeHigh != '\0' && LicenseeLow != '\0')
            {
                LicenseeNew = (byte)((('5' - 48) << 4) + ('4' - 48));
            }

            bool SuperGB = (Cartridge[0x146] == 0x03);

            int _CartridgeType = Cartridge[0x147];
            CartridgeTypes CartridgeType = CartridgeTypes.ROM;
            bool RAM = false;
            bool SRAM = false;
            bool BATTERY = false;
            bool RUMBLE = false;
            bool TIMER = false;
            switch (_CartridgeType)
            {
                case 0x00: CartridgeType = CartridgeTypes.ROM;                                                      break;
                case 0x01: CartridgeType = CartridgeTypes.MBC1;                                                     break;
                case 0x02: CartridgeType = CartridgeTypes.MBC1;          RAM = true;                                break;
                case 0x03: CartridgeType = CartridgeTypes.MBC1;          RAM = true; BATTERY = true;                break;
                case 0x05: CartridgeType = CartridgeTypes.MBC2;                                                     break;
                case 0x06: CartridgeType = CartridgeTypes.MBC2;                      BATTERY = true;                break;
                case 0x08: CartridgeType = CartridgeTypes.ROM;           RAM = true;                                break;
                case 0x09: CartridgeType = CartridgeTypes.ROM;           RAM = true; BATTERY = true;                break;
                case 0x0B: CartridgeType = CartridgeTypes.MMMD1;                                                    break;
                case 0x0C: CartridgeType = CartridgeTypes.MMMD1;        SRAM = true;                                break;
                case 0x0D: CartridgeType = CartridgeTypes.MMMD1;        SRAM = true; BATTERY = true;                break;
                case 0x0F: CartridgeType = CartridgeTypes.MBC3;                      BATTERY = true; TIMER = true;  break;
                case 0x10: CartridgeType = CartridgeTypes.MBC3;          RAM = true; BATTERY = true; TIMER = true;  break;
                case 0x11: CartridgeType = CartridgeTypes.MBC3;                                                     break;
                case 0x12: CartridgeType = CartridgeTypes.MBC3;          RAM = true;                                break;
                case 0x13: CartridgeType = CartridgeTypes.MBC5;          RAM = true; BATTERY = true;                break;
                case 0x19: CartridgeType = CartridgeTypes.MBC3;                                                     break;
                case 0x1A: CartridgeType = CartridgeTypes.MBC5;          RAM = true;                                break;
                case 0x1B: CartridgeType = CartridgeTypes.MBC5;          RAM = true; BATTERY = true;                break;
                case 0x1C: CartridgeType = CartridgeTypes.MBC5;                                      RUMBLE = true; break;
                case 0x1D: CartridgeType = CartridgeTypes.MBC5;         SRAM = true;                 RUMBLE = true; break;
                case 0x1E: CartridgeType = CartridgeTypes.MBC5;         SRAM = true; BATTERY = true; RUMBLE = true; break;
                case 0x1F: CartridgeType = CartridgeTypes.PocketCamera;                                             break;
                case 0xFD: CartridgeType = CartridgeTypes.Bandai;                                                   break;
                case 0xFE: CartridgeType = CartridgeTypes.Hudson;                                                   break;
                case 0xFF: CartridgeType = CartridgeTypes.Hudson;       SRAM = true;                                break;
            }
            MBCs.CartridgeType CartridgeInfo = new MBCs.CartridgeType() { CartridgeInfo=CartridgeType, RAM=RAM, BATTERY=BATTERY, RUMBLE=RUMBLE, SRAM=SRAM, TIMER=TIMER};

            int ROMSize = Cartridge[0x148];
            int ROMBanks = 0;
            switch (ROMSize)
            {
                case 0:    ROMSize = 256 * 1024;        ROMBanks = 1;   break;
                case 1:    ROMSize = 512 * 1024;        ROMBanks = 4;   break;
                case 2:    ROMSize = 1   * 1024 * 1024; ROMBanks = 8;   break;
                case 3:    ROMSize = 2   * 1024 * 1024; ROMBanks = 16;  break;
                case 4:    ROMSize = 4   * 1024 * 1024; ROMBanks = 32;  break;
                case 5:    ROMSize = 8   * 1024 * 1024; ROMBanks = 64;  break;
                case 6:    ROMSize = 16  * 1024 * 1024; ROMBanks = 128; break;
                case 0x52: ROMSize = 9   * 1024 * 1024; ROMBanks = 72;  break;
                case 0x53: ROMSize = 10  * 1024 * 1024; ROMBanks = 80;  break;
                case 0x54: ROMSize = 12  * 1024 * 1024; ROMBanks = 96;  break;
            }
            ROMSize /= 8;
            int RAMSize = Cartridge[0x149];
            int RAMBanks = 0;
            switch (RAMSize)
            {
                case 0: RAMSize = 0;          RAMBanks = 0;  break;
                case 1: RAMSize = 2   * 1024; RAMBanks = 1;  break;
                case 2: RAMSize = 8   * 1024; RAMBanks = 1;  break;
                case 3: RAMSize = 32  * 1024; RAMBanks = 4;  break;
                case 4: RAMSize = 128 * 1024; RAMBanks = 16; break;
            }

            bool Japanese = (Cartridge[0x14A] == 0);

            int _Licensee = Cartridge[0x14B];
            LicenseeType Licensee;
            switch (_Licensee)
            {
                case 0x33: Licensee = LicenseeType.New; break;
                case 0x79: Licensee = LicenseeType.Accolade; break;
                case 0xA4: Licensee = LicenseeType.Konami; break;
                default: Licensee = LicenseeType.Unknown; break;
            }

            int MaskROMversion = Cartridge[0x14C];
            int Complement = Cartridge[0x14D];
            int CheckSum = (Cartridge[0x14E] << 8) + Cartridge[0x14F];

            MBCs.CartridgeInfo info = new MBCs.CartridgeInfo(RomName, Licensee, LicenseeNew, Japanese, MaskROMversion, color, SuperGB, CartridgeInfo, RAMSize, RAMBanks, ROMSize, ROMBanks, Complement, CheckSum);

            switch (info.CartridgeType.CartridgeInfo)
            {
                case CartridgeTypes.ROM: return new MBCs.ROM(info, Cartridge);
                case CartridgeTypes.MBC1: return new MBCs.MBC1(info, Cartridge);

                default: return null;
            }
        }

        private static T[] GetRange<T>(T[] original, int from, int to)
        {
            T[] destination = new T[to-from+1];
            Array.Copy(original, from, destination, 0, to - from+1);
            return destination;
        }
    }
}
