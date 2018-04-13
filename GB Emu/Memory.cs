using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu
{
    public class SpecialRegisters
    {
        public abstract class MemoryRegister
        {
            public byte _val;

            public MemoryRegister(byte value)
            {
                Value = value;
            }

            public byte Value
            {
                get { return _val; }
                set { if (!ValueChanged(value)) _val = value; }
            }

            public abstract bool ValueChanged(byte value);
        }

        public class STATClass : MemoryRegister
        {
            public enum Mode { HBlank, VBlank, Search, Transfer };
            public STATClass(byte value) : base(value)
            {

            }

            public override bool ValueChanged(byte value)
            {
                switch(value&0b11)
                {
                    case 0x0: _ModeFlag = Mode.HBlank; break;
                    case 0x1: _ModeFlag = Mode.VBlank; break;
                    case 0x2: _ModeFlag = Mode.Search; break;
                    case 0x3: _ModeFlag = Mode.Transfer; break;
                }
                _CoincidenceFlag = ((value & 0x4) == 0x4);
                HBlankInterrupt = ((value & 0x8) == 0x8);
                VBlankInterrupt = ((value & 0x10) == 0x10);
                OAMInterrupt = ((value & 0x20) == 0x20);
                LYC_LYCoincidenceInterrupt = ((value & 0x40) == 0x40);
                return false;
            }

            private Mode _ModeFlag;
            private bool _CoincidenceFlag;
            public Mode ModeFlag
            {
                get { return _ModeFlag; }
                set
                {
                    _ModeFlag = value;
                    _val = (byte)(_val & 0b11111100);
                    switch (_ModeFlag)
                    {
                        case Mode.HBlank:
                            _val = (byte)(Value | 0b00);
                            break;
                        case Mode.VBlank:
                            _val = (byte)(Value | 0b01);
                            break;
                        case Mode.Search:
                            _val = (byte)(Value | 0b10);
                            break;
                        case Mode.Transfer:
                            _val = (byte)(Value | 0b11);
                            break;
                    }
                }
            }
            public bool CoincidenceFlag
            {
                get { return _CoincidenceFlag; }
                set {
                    _CoincidenceFlag = value;
                    Value = (byte)(Value & 0b11111011);
                    if (_CoincidenceFlag) Value = (byte)(Value | 0b00000100);
                }
            }
            public bool HBlankInterrupt { get; private set; }
            public bool VBlankInterrupt { get; private set; }
            public bool OAMInterrupt { get; private set; }
            public bool LYC_LYCoincidenceInterrupt { get; private set; }
        }
        public class LCDCClass : MemoryRegister
        {
            public LCDCClass(byte value) : base(value)
            {

            }

            public override bool ValueChanged(byte value)
            {
                BGDisplay = (value & 0x01) == 0x01;
                OBJDisplayEnabled = (value & 0x02) == 0x02;
                OBJSize = (value & 0x04) == 0x04;
                BGTileMapDisplaySelect = (value & 0x08) == 0x08;
                BGWindowTIleMapDataSelect = (value & 0x10) == 0x10;
                WindowDisplayEnable = (value & 0x20) == 0x20;
                WindowTileMapDisplaySelect = (value & 0x40) == 0x40;
                LCDDisplayEnable = (value & 0x80) == 0x80;
                return false;
            }

            public bool LCDDisplayEnable { get; private set; }
            public bool WindowTileMapDisplaySelect { get; private set; }
            public bool WindowDisplayEnable { get; private set; }
            public bool BGWindowTIleMapDataSelect { get; private set; }
            public bool BGTileMapDisplaySelect { get; private set; }
            public bool OBJSize { get; private set; }
            public bool OBJDisplayEnabled { get; private set; }
            public bool BGDisplay { get; private set; }
        }
        public class SimpleReg : MemoryRegister
        {
            public SimpleReg(byte value) : base(value)
            {
            }
            public override bool ValueChanged(byte value)
            {
                return false;
            }
        }

        public class PalleteGB : MemoryRegister
        {
            public enum Colors { White, LightGray, DarkGray, Black, Error}

            public PalleteGB(byte value) : base (value)
            {

            }

            public override bool ValueChanged(byte value)
            {
                Col0 = GetColor(value & 0b00000011);
                Col1 = GetColor((value >> 2) & 0b00000011);
                Col2 = GetColor((value >> 4) & 0b00000011);
                Col3 = GetColor((value >> 6) & 0b00000011);
                return false;
            }

            private Colors GetColor(int index)
            {
                switch (index)
                {
                    case 0: return Colors.White;
                    case 1: return Colors.LightGray;
                    case 2: return Colors.DarkGray;
                    case 3: return Colors.Black;
                }
                throw new IndexOutOfRangeException();
            }

            public Colors Col0 { get; private set; }
            public Colors Col1 { get; private set; }
            public Colors Col2 { get; private set; }
            public Colors Col3 { get; private set; }
        }

        public class JOYReg : MemoryRegister
        {
            private bool _A;
            private bool _B;
            private bool _Start;
            private bool _Select;
            private bool _Up;
            private bool _Down;
            private bool _Left;
            private bool _Right;
            public bool A
            {
                get
                {
                    return _A;
                }
                set
                {
                    _A = value;
                    Update();
                }
            }
            public bool B
            {
                get
                {
                    return _B;
                }
                set
                {
                    _B = value;
                    Update();
                }
            }
            public bool Start
            {
                get
                {
                    return _Start;
                }
                set
                {
                    _Start = value;
                    Update();
                }
            }
            public bool Select
            {
                get
                {
                    return _Select;
                }
                set
                {
                    _Select = value;
                    Update();
                }
            }
            public bool Up
            {
                get { return _Up; }
                set { _Up = value; Update(); }
            }
            public bool Down
            {
                get { return _Down; }
                set { _Down = value; Update(); }
            }
            public bool Left
            {
                get { return _Left; }
                set { _Left = value; Update(); }
            }
            public bool Right
            {
                get { return _Right; }
                set { _Right = value; Update(); }
            }

            private void Update()
            {
                if (_Right || _A) _val &= 0xFF - 0x01;
                else _val |= 0x01;
                if (_Left || _B) _val &= 0xFF - 0x02;
                else _val |= 0x02;
                if (_Up || _Select) _val &= 0xFF - 0x04;
                else _val |= 0x04;
                if (_Down || _Start) _val &= 0xFF - 0x08;
                else _val |= 0x08;
                if (_Left || _Right || _Up || _Down) _val &= 0xFF - 0x10;
                else _val |= 0x10;
                if (_A || _B || _Start || _Select) _val &= 0xFF - 0x20;
                else _val |= 0x20;
            }

            public JOYReg(byte value) : base(value)
            {
            }
            public override bool ValueChanged(byte value)
            {
                byte newval = value;
                newval &= 0xF0;
                _val &= 0x0F;
                _val |= newval;
                return true;
            }
        }

        Dictionary<int, MemoryRegister> AddressMap = new Dictionary<int, MemoryRegister>();
        public LCDCClass LCDC;
        public STATClass STAT;
        public SimpleReg SCX;
        public SimpleReg SCY;
        public SimpleReg LY;
        public SimpleReg LYC;
        public SimpleReg WX;
        public SimpleReg WY;
        public PalleteGB BGP;
        public PalleteGB OBP0;
        public PalleteGB OBP1;
        public JOYReg JOYP;

        public SpecialRegisters()
        {
            LCDC = new LCDCClass(0);
            STAT = new STATClass(0);
            SCX = new SimpleReg(0);
            SCY = new SimpleReg(0);
            LY = new SimpleReg(0);
            LYC = new SimpleReg(0);
            WX = new SimpleReg(0);
            WY = new SimpleReg(0);
            BGP = new PalleteGB(0);
            OBP0 = new PalleteGB(0);
            OBP1 = new PalleteGB(0);
            JOYP = new JOYReg(0);

            AddressMap.Add(0xFF00, JOYP);
            AddressMap.Add(0xFF40, LCDC);
            AddressMap.Add(0xFF41, STAT);
            AddressMap.Add(0xFF42, SCY);
            AddressMap.Add(0xFF43, SCX);
            AddressMap.Add(0xFF44, LY);
            AddressMap.Add(0xFF45, LYC);
            AddressMap.Add(0xFF4A, WY);
            AddressMap.Add(0xFF4B, WX);
            AddressMap.Add(0xFF47, BGP);
            AddressMap.Add(0xFF48, OBP0);
            AddressMap.Add(0xFF49, OBP1);
        }

        public bool Contains(int address)
        {
            return AddressMap.ContainsKey(address);
        }

        public byte this[int address]
        {
            get
            {
                if (!Contains(address)) throw new IndexOutOfRangeException();
                return AddressMap[address].Value;
            }
            set
            {
                if (!Contains(address)) throw new IndexOutOfRangeException();
                AddressMap[address].Value = value;
            }
        }
    }

    public class Memory
    {
        public byte[] bios = new byte[0x100];
        public byte[] memory = new byte[0x10000];
        public MBCs.MBC MBC;
        public bool InBios = true;
        public SpecialRegisters specialRegister = new SpecialRegisters();

        public Memory()
        {
            for (int i = 0xFF00; i <= 0xFFFF; i++)
            {
                memory[i] = 0xFF;
            }

            specialRegister.LCDC.Value = 0x00;
            specialRegister.STAT.Value = 0x84;
            specialRegister.SCY.Value = 0x00;
            specialRegister.SCX.Value = 0x00;
            specialRegister.LY.Value = 0x00;
            specialRegister.LYC.Value = 0x00;
            memory[0xFF46] = 0xFF;
            specialRegister.BGP.Value = 0xFC;
            specialRegister.OBP0.Value = 0xFF;
            specialRegister.OBP1.Value = 0xFF;
            specialRegister.WX.Value = 0x00;
            specialRegister.WY.Value = 0x00;

            memory[0xFF70] = 0xFF;
            memory[0xFF4F] = 0xFF;
            memory[0xFF4D] = 0xFF;
            memory[0xFF00] = 0xCF;
            memory[0xFF01] = 0x00;
            memory[0xFF02] = 0x7E;
            memory[0xFF04] = 0x00;
            memory[0xFF05] = 0x00;
            memory[0xFF06] = 0x00;
            memory[0xFF07] = 0xF8;
            memory[0xFF0F] = 0xE1;
            memory[0xFFFF] = 0x00;

            memory[0xFF10] = 0x80;
            memory[0xFF11] = 0x3F;
            memory[0xFF12] = 0x00;
            memory[0xFF13] = 0x00;
            memory[0xFF14] = 0xB8;
            memory[0xFF24] = 0x00;
            memory[0xFF25] = 0x00;
            memory[0xFF26] = 0x70;

            memory[0xFF15] = 0xFF;
            memory[0xFF16] = 0x3F;
            memory[0xFF17] = 0x00;
            memory[0xFF18] = 0x00;
            memory[0xFF19] = 0xB8;

            memory[0xFF1A] = 0x7F;
            memory[0xFF1B] = 0xFF;
            memory[0xFF1C] = 0x9F;
            memory[0xFF1D] = 0x00;
            memory[0xFF1E] = 0xB8;

            memory[0xFF1F] = 0xFF;
            memory[0xFF20] = 0xFF;
            memory[0xFF21] = 0x00;
            memory[0xFF22] = 0x00;
            memory[0xFF23] = 0xBF;
            memory[0xFF01] = 0x00;
        }

        public byte this[int address]
        {
            get
            {
                if (specialRegister.Contains(address))
                {
                    return specialRegister[address];
                }
                if (InBios && InRange(0x0,  0x100, address))
                {
                    return bios[address];
                }
                if (InRange(0xA000, 0xC000, address))
                {
                    address -= 0xA000;
                    return MBC.ReadRAM((ushort)address);
                }
                return memory[address];
            }
            set
            {
                MBC.CheckWrite(address, value);
                if (address == 0xFF4F)
                {
                    int addr = value << 8;
                    for (int i = 0; i <= 0x9F; i++)
                    {
                        int add0 = addr + i;
                        int add1 = 0xFE00 + i;
                        memory[add1] = memory[add0];
                    }
                }
                if (specialRegister.Contains(address))
                {
                    specialRegister[address] = value;
                }
                if (InBios & value == 1 && address == 0xFF50)
                {
                    InBios = false;
                }
                if (InRange(0x0000, 0x8000, address)) return;

                if (InRange(0xA000, 0xC000, address))
                {
                    address -= 0xA000;
                    MBC.WriteRAM((ushort)address, value);
                    return;
                }

                if (InRange(0xE000,0xFE00,address)) address -= 0x2000;
                memory[address] = value;
                if (InRange(0xC000, 0xCE00, address)) memory[address + 0x2000] = value;
                
            }
        }

        public ushort ReadLittleEndian(int address)
        {
            return (ushort)(this[address] | (this[address + 1] << 8));
        }
        public void WriteLittleEndian(int address, int value)
        {
            byte lower = (byte)(value & 0xFF);
            byte higher = (byte)((value >> 8) & 0xFF);
            this[address] = lower;
            this[address+1] = higher;
        }

        private bool InRange(int low, int high, int value)
        {
            if (low <= value && value < high)
            {
                return true;
            }
            return false;
        }
    }
}
