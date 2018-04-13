using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu
{
    public class CPU
    {
        public Memory memory;
        public delegate int Opcode(CPU c);
        public bool InterruptsNext = true;
        public int BreakPoint = -1;
        bool called = false;
        public bool Running = true;
        public Display display = new Display();

        public struct Instruction
        {
            public Opcode Opcode;
            public string Name;
            public int Length;

            public Instruction(Opcode opcode, string name, int length)
            {
                Opcode = opcode;
                Name = name;
                Length = length;
            }
        }

        //PUSH & POP... what to say
        public void PUSH(ushort value)
        {
            SP-=2;
            memory.WriteLittleEndian(SP, value);
        }

        public ushort POP()
        {
            ushort ret = memory.ReadLittleEndian(SP);
            SP += 2;
            return ret;
        }

        public int clock = 0;

        public CPU()
        {
        }

        public void Halt()
        {

        }
        public void Stop()
        {

        }

        //Gets ready?
        public void GetReady()
        {
            memory.bios = System.IO.File.ReadAllBytes("BIOS GB.gb");
            PC = 0;
        }

        public int Step()
        {
            byte opcode = memory[PC];
            if (memory.InBios)
            {
                //Skips instruction 0xFA in bios - checksum of ROM.
                if (PC == 0xFA) { PC += 2; return 0; }
            }
            int time = instructionTable[opcode].Opcode(this);
            clock += time;
            if (!called)
            {
                PC++;
            }
            else
            {
                called = false;
            }
            if (memory.specialRegister.LCDC.LCDDisplayEnable)
                display.Step(time);
            //Temporary
            //memory.specialRegister.LY.Value = (byte)display.line;
            memory.specialRegister.STAT.ModeFlag = (SpecialRegisters.STATClass.Mode)display.mode;
            if (memory.specialRegister.LYC == memory.specialRegister.LY) memory.specialRegister.STAT.CoincidenceFlag = true;
            return time;
        }

        public void Run()
        {
            Running = true;
            while (Running)
            {
                int t = Step();
                if (PC == BreakPoint) Running = false;
            }
            Form1.Instance.Invoke(Form1.Instance.showValues);
        }









        #region Registers
        public byte A;
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte F;
        public byte H;
        public byte L;

        public ushort AF
        {
            get
            {
                return (ushort)((A << 8) | F);
            }
            set
            {
                F = (byte)(value & 0xFF);
                A = (byte)((value & 0xFF00) >> 8);
            }
        }
        public ushort BC
        {
            get
            {
                return (ushort)((B << 8) | C);
            }
            set
            {
                C = (byte)(value & 0xFF);
                B = (byte)((value & 0xFF00) >> 8);
            }
        }
        public ushort DE
        {
            get
            {
                return (ushort)((D << 8) | E);
            }
            set
            {
                E = (byte)(value & 0xFF);
                D = (byte)((value & 0xFF00) >> 8);
            }
        }
        public ushort HL
        {
            get
            {
                return (ushort)((H << 8) | L);
            }
            set
            {
                L = (byte)(value & 0xFF);
                H = (byte)((value & 0xFF00) >> 8);
            }
        }
        public ushort SP;
        public ushort PC;

        public bool ZeroFlag
        {
            get
            {
                return (F & 0x80) != 0;
            }
            set
            {
                if (value) F |= 0x80;
                else F &= 0xFF - 0x80;
            }
        }
        public bool SubtractFlag
        {
            get
            {
                return (F & 0x40) != 0;
            }
            set
            {
                if (value) F |= 0x40;
                else F &= 0xFF - 0x40;
            }
        }
        public bool HalfCarryFlag
        {
            get
            {
                return (F & 0x20) != 0;
            }
            set
            {
                if (value) F |= 0x20;
                else F &= 0xFF - 0x20;
            }
        }
        public bool CarryFlag
        {
            get
            {
                return (F & 0x10) != 0;
            }
            set
            {
                if (value) F |= 0x10;
                else F &= 0xFF - 0x10;
            }
        }
        #endregion

        //Carry calculator?
        public static bool GetCarry16(int num)
        {
            return num > 0xFFFF;
        }
        public static bool GetCarry8(int num)
        {
            return num > 0xFF;
        }

        public static bool GetBorrow(int num1, int num2)
        {
            return num2 > num1;
        }

        public static bool GetHalfBorrow8(int num1, int num2)
        {
            return ((num2 & 0xF) > (num1 & 0xF));
        }
        public static bool GetHalfBorrow16(int num1, int num2)
        {
            return ((num2 & 0xFFF) > (num1 & 0xFFF));
        }

        public static bool GetHalfCarry16(int num1, int num2)
        {
            return ((((num1 & 0xFFF) + (num2 & 0xFFF)) & 0x1000) == 0x1000);
        }
        public static bool GetHalfCarry8(int num1, int num2)
        {
            return ((((num1 & 0xF) + (num2 & 0xF)) & 0x10) == 0x10);
        }

        public void Call(ushort address)
        {
            PUSH((ushort)(PC+1));
            called = true;
            PC = address;
        }
        public void Return()
        {
            PC = (ushort)(POP()-1);
        }

        //Opcode tables
        public static Instruction[] instructionTable = new Instruction[]{
            new Instruction(opcode00,"nop",0),new Instruction(opcode01,"ld bc, %2",2),new Instruction(opcode02,"ld (bc), a",0),new Instruction(opcode03,"inc bc",0),new Instruction(opcode04,"inc b",0),new Instruction(opcode05,"dec b",0),new Instruction(opcode06,"ld b, %1",1),new Instruction(opcode07,"rlc a",0),new Instruction(opcode08,"ld (%2), sp",2),new Instruction(opcode09,"add hl, bc",0),new Instruction(opcode0A,"ld a, (bc)",0),new Instruction(opcode0B,"dec bc",0),new Instruction(opcode0C,"inc c",0),new Instruction(opcode0D,"dec c",0),new Instruction(opcode0E,"ld c, %1",1),new Instruction(opcode0F,"rrc a",0),
            new Instruction(opcode10,"stop",0),new Instruction(opcode11,"ld de, %2",2),new Instruction(opcode12,"ld (de), a",0),new Instruction(opcode13,"inc de",0),new Instruction(opcode14,"inc d",0),new Instruction(opcode15,"dec d",0),new Instruction(opcode16,"ld d, %1",1),new Instruction(opcode17,"rl a",0),new Instruction(opcode18,"jr %1",1),new Instruction(opcode19,"add hl, de",0),new Instruction(opcode1A,"ld a, (de)",0),new Instruction(opcode1B,"dec de",0),new Instruction(opcode1C,"inc e",0),new Instruction(opcode1D,"dec e",0),new Instruction(opcode1E,"ld e, %1",1),new Instruction(opcode1F,"rr a",0),
            new Instruction(opcode20,"jr nz, %1",1),new Instruction(opcode21,"ld hl, %2",2),new Instruction(opcode22,"ldi (hl), a",0),new Instruction(opcode23,"inc hl",0),new Instruction(opcode24,"inc h",0),new Instruction(opcode25,"dec h",0),new Instruction(opcode26,"ld h, %1",1),new Instruction(opcode27,"daa",0),new Instruction(opcode28,"jr z, %1",1),new Instruction(opcode29,"add hl, hl",0),new Instruction(opcode2A,"ldi a, (hl)",0),new Instruction(opcode2B,"dec hl",0),new Instruction(opcode2C,"inc l",0),new Instruction(opcode2D,"dec l",0),new Instruction(opcode2E,"ld l, %1",1),new Instruction(opcode2F,"cpl",0),
            new Instruction(opcode30,"jr nc, %1",1),new Instruction(opcode31,"ld sp, %2",2),new Instruction(opcode32,"ldd (hl), a",0),new Instruction(opcode33,"inc sp",0),new Instruction(opcode34,"inc (hl)",0),new Instruction(opcode35,"dec (hl)",0),new Instruction(opcode36,"ld (hl), %1",1),new Instruction(opcode37,"scf",0),new Instruction(opcode38,"jr c, %1",1),new Instruction(opcode39,"add hl, sp",0),new Instruction(opcode3A,"ldd a, (hl)",0),new Instruction(opcode3B,"dec sp",0),new Instruction(opcode3C,"inc a",0),new Instruction(opcode3D,"dec a",0),new Instruction(opcode3E,"ld a, %1",1),new Instruction(opcode3F,"ccf",0),
            new Instruction(opcode40,"ld b, b",0),new Instruction(opcode41,"ld b, c",0),new Instruction(opcode42,"ld b, d",0),new Instruction(opcode43,"ld b, e",0),new Instruction(opcode44,"ld b, h",0),new Instruction(opcode45,"ld b, l",0),new Instruction(opcode46,"ld b, (hl)",0),new Instruction(opcode47,"ld b, a",0),new Instruction(opcode48,"ld c, b",0),new Instruction(opcode49,"ld c, c",0),new Instruction(opcode4A,"ld c, d",0),new Instruction(opcode4B,"ld c, e",0),new Instruction(opcode4C,"ld c, h",0),new Instruction(opcode4D,"ld c, l",0),new Instruction(opcode4E,"ld c, (hl)",0),new Instruction(opcode4F,"ld c, a",0),
            new Instruction(opcode50,"ld d, b",0),new Instruction(opcode51,"ld d, c",0),new Instruction(opcode52,"ld d, d",0),new Instruction(opcode53,"ld d, e",0),new Instruction(opcode54,"ld d, h",0),new Instruction(opcode55,"ld d, l",0),new Instruction(opcode56,"ld d, (hl)",0),new Instruction(opcode57,"ld d, a",0),new Instruction(opcode58,"ld e, b",0),new Instruction(opcode59,"ld e, c",0),new Instruction(opcode5A,"ld e, d",0),new Instruction(opcode5B,"ld e, e",0),new Instruction(opcode5C,"ld e, h",0),new Instruction(opcode5D,"ld e, l",0),new Instruction(opcode5E,"ld e, (hl)",0),new Instruction(opcode5F,"ld e, a",0),
            new Instruction(opcode60,"ld h, b",0),new Instruction(opcode61,"ld h, c",0),new Instruction(opcode62,"ld h, d",0),new Instruction(opcode63,"ld h, e",0),new Instruction(opcode64,"ld h, h",0),new Instruction(opcode65,"ld h, l",0),new Instruction(opcode66,"ld h, (hl)",0),new Instruction(opcode67,"ld h, a",0),new Instruction(opcode68,"ld l, b",0),new Instruction(opcode69,"ld l, c",0),new Instruction(opcode6A,"ld l, d",0),new Instruction(opcode6B,"ld l, e",0),new Instruction(opcode6C,"ld l, h",0),new Instruction(opcode6D,"ld l, l",0),new Instruction(opcode6E,"ld l, (hl)",0),new Instruction(opcode6F,"ld l, a",0),
            new Instruction(opcode70,"ld (hl), b",0),new Instruction(opcode71,"ld (hl), c",0),new Instruction(opcode72,"ld (hl), d",0),new Instruction(opcode73,"ld (hl), e",0),new Instruction(opcode74,"ld (hl), h",0),new Instruction(opcode75,"ld (hl), l",0),new Instruction(opcode76,"halt",0),new Instruction(opcode77,"ld (hl), a",0),new Instruction(opcode78,"ld a, b",0),new Instruction(opcode79,"ld a, c",0),new Instruction(opcode7A,"ld a, d",0),new Instruction(opcode7B,"ld a, e",0),new Instruction(opcode7C,"ld a, h",0),new Instruction(opcode7D,"ld a, l",0),new Instruction(opcode7E,"ld a, (hl)",0),new Instruction(opcode7F,"ld a, a",0),
            new Instruction(opcode80,"add a, b",0),new Instruction(opcode81,"add a, c",0),new Instruction(opcode82,"add a, d",0),new Instruction(opcode83,"add a, e",0),new Instruction(opcode84,"add a, h",0),new Instruction(opcode85,"add a, l",0),new Instruction(opcode86,"add a, (hl)",0),new Instruction(opcode87,"add a, a",0),new Instruction(opcode88,"adc a, b",0),new Instruction(opcode89,"adc a, c",0),new Instruction(opcode8A,"adc a, d",0),new Instruction(opcode8B,"adc a, e",0),new Instruction(opcode8C,"adc a, h",0),new Instruction(opcode8D,"adc a, l",0),new Instruction(opcode8E,"adc a, (hl)",0),new Instruction(opcode8F,"adc a, a",0),
            new Instruction(opcode90,"sub a, b",0),new Instruction(opcode91,"sub a, c",0),new Instruction(opcode92,"sub a, d",0),new Instruction(opcode93,"sub a, e",0),new Instruction(opcode94,"sub a, h",0),new Instruction(opcode95,"sub a, l",0),new Instruction(opcode96,"sub a, (hl)",0),new Instruction(opcode97,"sub a, a",0),new Instruction(opcode98,"sbc a, b",0),new Instruction(opcode99,"sbc a, c",0),new Instruction(opcode9A,"sbc a, d",0),new Instruction(opcode9B,"sbc a, e",0),new Instruction(opcode9C,"sbc a, h",0),new Instruction(opcode9D,"sbc a, l",0),new Instruction(opcode9E,"sbc a, (hl)",0),new Instruction(opcode9F,"sbc a, a",0),
            new Instruction(opcodeA0,"and b",0),new Instruction(opcodeA1,"and c",0),new Instruction(opcodeA2,"and d",0),new Instruction(opcodeA3,"and e",0),new Instruction(opcodeA4,"and h",0),new Instruction(opcodeA5,"and l",0),new Instruction(opcodeA6,"and (hl)",0),new Instruction(opcodeA7,"and a",0),new Instruction(opcodeA8,"xor b",0),new Instruction(opcodeA9,"xor c",0),new Instruction(opcodeAA,"xor d",0),new Instruction(opcodeAB,"xor e",0),new Instruction(opcodeAC,"xor h",0),new Instruction(opcodeAD,"xor l",0),new Instruction(opcodeAE,"xor (hl)",0),new Instruction(opcodeAF,"xor a",0),
            new Instruction(opcodeB0,"or b",0),new Instruction(opcodeB1,"or c",0),new Instruction(opcodeB2,"or d",0),new Instruction(opcodeB3,"or e",0),new Instruction(opcodeB4,"or h",0),new Instruction(opcodeB5,"or l",0),new Instruction(opcodeB6,"or (hl)",0),new Instruction(opcodeB7,"or a",0),new Instruction(opcodeB8,"cp b",0),new Instruction(opcodeB9,"cp c",0),new Instruction(opcodeBA,"cp d",0),new Instruction(opcodeBB,"cp e",0),new Instruction(opcodeBC,"cp h",0),new Instruction(opcodeBD,"cp l",0),new Instruction(opcodeBE,"cp (hl)",0),new Instruction(opcodeBF,"cp a",0),
            new Instruction(opcodeC0,"ret nz",0),new Instruction(opcodeC1,"pop bc",0),new Instruction(opcodeC2,"jp nz, %2",2),new Instruction(opcodeC3,"jp %2",2),new Instruction(opcodeC4,"call nz, %2",2),new Instruction(opcodeC5,"push bc",0),new Instruction(opcodeC6,"add a, %1",1),new Instruction(opcodeC7,"rst 0",0),new Instruction(opcodeC8,"ret z",0),new Instruction(opcodeC9,"ret",0),new Instruction(opcodeCA,"jp z, %2",2),new Instruction(opcodeCB,"ext ops",0),new Instruction(opcodeCC,"call z, %2",2),new Instruction(opcodeCD,"call %2",2),new Instruction(opcodeCE,"adc a, %1",1),new Instruction(opcodeCF,"rst 8",0),
            new Instruction(opcodeD0,"ret nc",0),new Instruction(opcodeD1,"pop de",0),new Instruction(opcodeD2,"jp nc, %2",2),new Instruction(null,"null",0),new Instruction(opcodeD4,"call nc, %2",2),new Instruction(opcodeD5,"push de",0),new Instruction(opcodeD6,"sub a, %1",1),new Instruction(opcodeD7,"rst 10",0),new Instruction(opcodeD8,"ret c",0),new Instruction(opcodeD9,"reti",0),new Instruction(opcodeDA,"jp c, %2",2),new Instruction(null,"null",0),new Instruction(opcodeDC,"call c, %2",2),new Instruction(null,"null",0),new Instruction(null,"??sbc a, %1??",1),new Instruction(opcodeDF,"rst 18",0),
            new Instruction(opcodeE0,"ldh (%1), a",1),new Instruction(opcodeE1,"pop hl",0),new Instruction(opcodeE2,"ldh (c), a",0),new Instruction(null,"null",0),new Instruction(null,"null",0),new Instruction(opcodeE5,"push hl",0),new Instruction(opcodeE6,"and %1",1),new Instruction(opcodeE7,"rst 20",0),new Instruction(opcodeE8,"add sp, d",0),new Instruction(opcodeE9,"jp (hl)",0),new Instruction(opcodeEA,"ld (%2), a",2),new Instruction(null,"null",0),new Instruction(null,"null",0),new Instruction(null,"null",0),new Instruction(opcodeEE,"xor %1",1),new Instruction(opcodeEF,"rst 28",0),
            new Instruction(opcodeF0,"ldh a, (%1)",1),new Instruction(opcodeF1,"pop af",0),new Instruction(null,"null",0),new Instruction(opcodeF3,"di",0),new Instruction(null,"null",0),new Instruction(opcodeF5,"push af",0),new Instruction(opcodeF6,"or %1",1),new Instruction(opcodeF7,"rst 30",0),new Instruction(opcodeF8,"ldhl sp, d",0),new Instruction(opcodeF9,"ld sp, hl",0),new Instruction(opcodeFA,"ld a, (%2)",2),new Instruction(opcodeFB,"ei",0),new Instruction(null,"null",0),new Instruction(null,"null",0),new Instruction(opcodeFE,"cp %1",1),new Instruction(opcodeFF,"rst 38",0),
        };

        public static Instruction[] instructionCBTable = new Instruction[]{
            new Instruction(opcodeCB00,"rlc b",1),new Instruction(opcodeCB01,"rlc c",1),new Instruction(opcodeCB02,"rlc d",1),new Instruction(opcodeCB03,"rlc e",1),new Instruction(opcodeCB04,"rlc h",1),new Instruction(opcodeCB05,"rlc l",1),new Instruction(opcodeCB06,"rlc (hl)",1),new Instruction(opcodeCB07,"rlc a",1),new Instruction(opcodeCB08,"rrc b",1),new Instruction(opcodeCB09,"rrc c",1),new Instruction(opcodeCB0A,"rrc d",1),new Instruction(opcodeCB0B,"rrc e",1),new Instruction(opcodeCB0C,"rrc h",1),new Instruction(opcodeCB0D,"rrc l",1),new Instruction(opcodeCB0E,"rrc (hl)",1),new Instruction(opcodeCB0F,"rrc a",1),
            new Instruction(opcodeCB10,"rl b",1),new Instruction(opcodeCB11,"rl c",1),new Instruction(opcodeCB12,"rl d",1),new Instruction(opcodeCB13,"rl e",1),new Instruction(opcodeCB14,"rl h",1),new Instruction(opcodeCB15,"rl l",1),new Instruction(opcodeCB16,"rl (hl)",1),new Instruction(opcodeCB17,"rl a",1),new Instruction(opcodeCB18,"rr b",1),new Instruction(opcodeCB19,"rr c",1),new Instruction(opcodeCB1A,"rr d",1),new Instruction(opcodeCB1B,"rr e",1),new Instruction(opcodeCB1C,"rr h",1),new Instruction(opcodeCB1D,"rr l",1),new Instruction(opcodeCB1E,"rr (hl)",1),new Instruction(opcodeCB1F,"rr a",1),
            new Instruction(opcodeCB20,"sla b",1),new Instruction(opcodeCB21,"sla c",1),new Instruction(opcodeCB22,"sla d",1),new Instruction(opcodeCB23,"sla e",1),new Instruction(opcodeCB24,"sla h",1),new Instruction(opcodeCB25,"sla l",1),new Instruction(opcodeCB26,"sla (hl)",1),new Instruction(opcodeCB27,"sla a",1),new Instruction(opcodeCB28,"sra b",1),new Instruction(opcodeCB29,"sra c",1),new Instruction(opcodeCB2A,"sra d",1),new Instruction(opcodeCB2B,"sra e",1),new Instruction(opcodeCB2C,"sra h",1),new Instruction(opcodeCB2D,"sra l",1),new Instruction(opcodeCB2E,"sra (hl)",1),new Instruction(opcodeCB2F,"sra a",1),
            new Instruction(opcodeCB30,"swap b",1),new Instruction(opcodeCB31,"swap c",1),new Instruction(opcodeCB32,"swap d",1),new Instruction(opcodeCB33,"swap e",1),new Instruction(opcodeCB34,"swap h",1),new Instruction(opcodeCB35,"swap l",1),new Instruction(opcodeCB36,"swap (hl)",1),new Instruction(opcodeCB37,"swap a",1),new Instruction(opcodeCB38,"srl b",1),new Instruction(opcodeCB39,"srl c",1),new Instruction(opcodeCB3A,"srl d",1),new Instruction(opcodeCB3B,"srl e",1),new Instruction(opcodeCB3C,"srl h",1),new Instruction(opcodeCB3D,"srl l",1),new Instruction(opcodeCB3E,"srl (hl)",1),new Instruction(opcodeCB3F,"srl a",1),
            new Instruction(opcodeCB40,"bit 0, b",1),new Instruction(opcodeCB41,"bit 0, c",1),new Instruction(opcodeCB42,"bit 0, d",1),new Instruction(opcodeCB43,"bit 0, e",1),new Instruction(opcodeCB44,"bit 0, h",1),new Instruction(opcodeCB45,"bit 0, l",1),new Instruction(opcodeCB46,"bit 0, (hl)",1),new Instruction(opcodeCB47,"bit 0, a",1),new Instruction(opcodeCB48,"bit 1, b",1),new Instruction(opcodeCB49,"bit 1, c",1),new Instruction(opcodeCB4A,"bit 1, d",1),new Instruction(opcodeCB4B,"bit 1, e",1),new Instruction(opcodeCB4C,"bit 1, h",1),new Instruction(opcodeCB4D,"bit 1, l",1),new Instruction(opcodeCB4E,"bit 1, (hl)",1),new Instruction(opcodeCB4F,"bit 1, a",1),
            new Instruction(opcodeCB50,"bit 2, b",1),new Instruction(opcodeCB51,"bit 2, c",1),new Instruction(opcodeCB52,"bit 2, d",1),new Instruction(opcodeCB53,"bit 2, e",1),new Instruction(opcodeCB54,"bit 2, h",1),new Instruction(opcodeCB55,"bit 2, l",1),new Instruction(opcodeCB56,"bit 2, (hl)",1),new Instruction(opcodeCB57,"bit 2, a",1),new Instruction(opcodeCB58,"bit 3, b",1),new Instruction(opcodeCB59,"bit 3, c",1),new Instruction(opcodeCB5A,"bit 3, d",1),new Instruction(opcodeCB5B,"bit 3, e",1),new Instruction(opcodeCB5C,"bit 3, h",1),new Instruction(opcodeCB5D,"bit 3, l",1),new Instruction(opcodeCB5E,"bit 3, (hl)",1),new Instruction(opcodeCB5F,"bit 3, a",1),
            new Instruction(opcodeCB60,"bit 4, b",1),new Instruction(opcodeCB61,"bit 4, c",1),new Instruction(opcodeCB62,"bit 4, d",1),new Instruction(opcodeCB63,"bit 4, e",1),new Instruction(opcodeCB64,"bit 4, h",1),new Instruction(opcodeCB65,"bit 4, l",1),new Instruction(opcodeCB66,"bit 4, (hl)",1),new Instruction(opcodeCB67,"bit 4, a",1),new Instruction(opcodeCB68,"bit 5, b",1),new Instruction(opcodeCB69,"bit 5, c",1),new Instruction(opcodeCB6A,"bit 5, d",1),new Instruction(opcodeCB6B,"bit 5, e",1),new Instruction(opcodeCB6C,"bit 5, h",1),new Instruction(opcodeCB6D,"bit 5, l",1),new Instruction(opcodeCB6E,"bit 5, (hl)",1),new Instruction(opcodeCB6F,"bit 5, a",1),
            new Instruction(opcodeCB70,"bit 6, b",1),new Instruction(opcodeCB71,"bit 6, c",1),new Instruction(opcodeCB72,"bit 6, d",1),new Instruction(opcodeCB73,"bit 6, e",1),new Instruction(opcodeCB74,"bit 6, h",1),new Instruction(opcodeCB75,"bit 6, l",1),new Instruction(opcodeCB76,"bit 6, (hl)",1),new Instruction(opcodeCB77,"bit 6, a",1),new Instruction(opcodeCB78,"bit 7, b",1),new Instruction(opcodeCB79,"bit 7, c",1),new Instruction(opcodeCB7A,"bit 7, d",1),new Instruction(opcodeCB7B,"bit 7, e",1),new Instruction(opcodeCB7C,"bit 7, h",1),new Instruction(opcodeCB7D,"bit 7, l",1),new Instruction(opcodeCB7E,"bit 7, (hl)",1),new Instruction(opcodeCB7F,"bit 7, a",1),
            new Instruction(opcodeCB80,"res 0, b",1),new Instruction(opcodeCB81,"res 0, c",1),new Instruction(opcodeCB82,"res 0, d",1),new Instruction(opcodeCB83,"res 0, e",1),new Instruction(opcodeCB84,"res 0, h",1),new Instruction(opcodeCB85,"res 0, l",1),new Instruction(opcodeCB86,"res 0, (hl)",1),new Instruction(opcodeCB87,"res 0, a",1),new Instruction(opcodeCB88,"res 1, b",1),new Instruction(opcodeCB89,"res 1, c",1),new Instruction(opcodeCB8A,"res 1, d",1),new Instruction(opcodeCB8B,"res 1, e",1),new Instruction(opcodeCB8C,"res 1, h",1),new Instruction(opcodeCB8D,"res 1, l",1),new Instruction(opcodeCB8E,"res 1, (hl)",1),new Instruction(opcodeCB8F,"res 1, a",1),
            new Instruction(opcodeCB90,"res 2, b",1),new Instruction(opcodeCB91,"res 2, c",1),new Instruction(opcodeCB92,"res 2, d",1),new Instruction(opcodeCB93,"res 2, e",1),new Instruction(opcodeCB94,"res 2, h",1),new Instruction(opcodeCB95,"res 2, l",1),new Instruction(opcodeCB96,"res 2, (hl)",1),new Instruction(opcodeCB97,"res 2, a",1),new Instruction(opcodeCB98,"res 3, b",1),new Instruction(opcodeCB99,"res 3, c",1),new Instruction(opcodeCB9A,"res 3, d",1),new Instruction(opcodeCB9B,"res 3, e",1),new Instruction(opcodeCB9C,"res 3, h",1),new Instruction(opcodeCB9D,"res 3, l",1),new Instruction(opcodeCB9E,"res 3, (hl)",1),new Instruction(opcodeCB9F,"res 3, a",1),
            new Instruction(opcodeCBA0,"res 4, b",1),new Instruction(opcodeCBA1,"res 4, c",1),new Instruction(opcodeCBA2,"res 4, d",1),new Instruction(opcodeCBA3,"res 4, e",1),new Instruction(opcodeCBA4,"res 4, h",1),new Instruction(opcodeCBA5,"res 4, l",1),new Instruction(opcodeCBA6,"res 4, (hl)",1),new Instruction(opcodeCBA7,"res 4, a",1),new Instruction(opcodeCBA8,"res 5, b",1),new Instruction(opcodeCBA9,"res 5, c",1),new Instruction(opcodeCBAA,"res 5, d",1),new Instruction(opcodeCBAB,"res 5, e",1),new Instruction(opcodeCBAC,"res 5, h",1),new Instruction(opcodeCBAD,"res 5, l",1),new Instruction(opcodeCBAE,"res 5, (hl)",1),new Instruction(opcodeCBAF,"res 5, a",1),
            new Instruction(opcodeCBB0,"res 6, b",1),new Instruction(opcodeCBB1,"res 6, c",1),new Instruction(opcodeCBB2,"res 6, d",1),new Instruction(opcodeCBB3,"res 6, e",1),new Instruction(opcodeCBB4,"res 6, h",1),new Instruction(opcodeCBB5,"res 6, l",1),new Instruction(opcodeCBB6,"res 6, (hl)",1),new Instruction(opcodeCBB7,"res 6, a",1),new Instruction(opcodeCBB8,"res 7, b",1),new Instruction(opcodeCBB9,"res 7, c",1),new Instruction(opcodeCBBA,"res 7, d",1),new Instruction(opcodeCBBB,"res 7, e",1),new Instruction(opcodeCBBC,"res 7, h",1),new Instruction(opcodeCBBD,"res 7, l",1),new Instruction(opcodeCBBE,"res 7, (hl)",1),new Instruction(opcodeCBBF,"res 7, a",1),
            new Instruction(opcodeCBC0,"set 0, b",1),new Instruction(opcodeCBC1,"set 0, c",1),new Instruction(opcodeCBC2,"set 0, d",1),new Instruction(opcodeCBC3,"set 0, e",1),new Instruction(opcodeCBC4,"set 0, h",1),new Instruction(opcodeCBC5,"set 0, l",1),new Instruction(opcodeCBC6,"set 0, (hl)",1),new Instruction(opcodeCBC7,"set 0, a",1),new Instruction(opcodeCBC8,"set 1, b",1),new Instruction(opcodeCBC9,"set 1, c",1),new Instruction(opcodeCBCA,"set 1, d",1),new Instruction(opcodeCBCB,"set 1, e",1),new Instruction(opcodeCBCC,"set 1, h",1),new Instruction(opcodeCBCD,"set 1, l",1),new Instruction(opcodeCBCE,"set 1, (hl)",1),new Instruction(opcodeCBCF,"set 1, a",1),
            new Instruction(opcodeCBD0,"set 2, b",1),new Instruction(opcodeCBD1,"set 2, c",1),new Instruction(opcodeCBD2,"set 2, d",1),new Instruction(opcodeCBD3,"set 2, e",1),new Instruction(opcodeCBD4,"set 2, h",1),new Instruction(opcodeCBD5,"set 2, l",1),new Instruction(opcodeCBD6,"set 2, (hl)",1),new Instruction(opcodeCBD7,"set 2, a",1),new Instruction(opcodeCBD8,"set 3, b",1),new Instruction(opcodeCBD9,"set 3, c",1),new Instruction(opcodeCBDA,"set 3, d",1),new Instruction(opcodeCBDB,"set 3, e",1),new Instruction(opcodeCBDC,"set 3, h",1),new Instruction(opcodeCBDD,"set 3, l",1),new Instruction(opcodeCBDE,"set 3, (hl)",1),new Instruction(opcodeCBDF,"set 3, a",1),
            new Instruction(opcodeCBE0,"set 4, b",1),new Instruction(opcodeCBE1,"set 4, c",1),new Instruction(opcodeCBE2,"set 4, d",1),new Instruction(opcodeCBE3,"set 4, e",1),new Instruction(opcodeCBE4,"set 4, h",1),new Instruction(opcodeCBE5,"set 4, l",1),new Instruction(opcodeCBE6,"set 4, (hl)",1),new Instruction(opcodeCBE7,"set 4, a",1),new Instruction(opcodeCBE8,"set 5, b",1),new Instruction(opcodeCBE9,"set 5, c",1),new Instruction(opcodeCBEA,"set 5, d",1),new Instruction(opcodeCBEB,"set 5, e",1),new Instruction(opcodeCBEC,"set 5, h",1),new Instruction(opcodeCBED,"set 5, l",1),new Instruction(opcodeCBEE,"set 5, (hl)",1),new Instruction(opcodeCBEF,"set 5, a",1),
            new Instruction(opcodeCBF0,"set 6, b",1),new Instruction(opcodeCBF1,"set 6, c",1),new Instruction(opcodeCBF2,"set 6, d",1),new Instruction(opcodeCBF3,"set 6, e",1),new Instruction(opcodeCBF4,"set 6, h",1),new Instruction(opcodeCBF5,"set 6, l",1),new Instruction(opcodeCBF6,"set 6, (hl)",1),new Instruction(opcodeCBF7,"set 6, a",1),new Instruction(opcodeCBF8,"set 7, b",1),new Instruction(opcodeCBF9,"set 7, c",1),new Instruction(opcodeCBFA,"set 7, d",1),new Instruction(opcodeCBFB,"set 7, e",1),new Instruction(opcodeCBFC,"set 7, h",1),new Instruction(opcodeCBFD,"set 7, l",1),new Instruction(opcodeCBFE,"set 7, (hl)",1),new Instruction(opcodeCBFF,"set 7, a",1),
        };
        
        public static Instruction GetInstruction(Memory MEM, int address)
        {
            if (MEM[address] == 0xCB)
            {
                return instructionCBTable[MEM[address + 1]];
            }
            return instructionTable[MEM[address]];
        }


        //Opcode CBxx
        public static int opcodeCB(CPU c)
        {
            c.PC++;
            byte opcode = c.memory[c.PC];
            return instructionCBTable[opcode].Opcode(c);
        }
        
        //Shitton of opcodes
        #region Opcodes
        #region 8-Bit Loads
        #region LD nn,n
        public static int opcode06(CPU c) //LD B,n
        {
            c.PC++;
            c.B = c.memory[c.PC];
            return 8;
        }
        public static int opcode0E(CPU c) //LD C,n
        {
            c.PC++;
            c.C = c.memory[c.PC];
            return 8;
        }
        public static int opcode16(CPU c) //LD D,n
        {
            c.PC++;
            c.D = c.memory[c.PC];
            return 8;
        }
        public static int opcode1E(CPU c) //LD E,n
        {
            c.PC++;
            c.E = c.memory[c.PC];
            return 8;
        }
        public static int opcode26(CPU c) //LD H,n
        {
            c.PC++;
            c.H = c.memory[c.PC];
            return 8;
        }
        public static int opcode2E(CPU c) //LD L,n
        {
            c.PC++;
            c.L = c.memory[c.PC];
            return 8;
        }
        #endregion
        #region LD r1, r2
        public static int opcode7F(CPU c) //LD A,A
        {
            return 4;
        }
        public static int opcode78(CPU c) //LD A,B
        {
            c.A = c.B;
            return 4;
        }
        public static int opcode79(CPU c) //LD A,C
        {
            c.A = c.C;
            return 4;
        }
        public static int opcode7A(CPU c) //LD A,D
        {
            c.A = c.D;
            return 4;
        }
        public static int opcode7B(CPU c) //LD A,E
        {
            c.A = c.E;
            return 4;
        }
        public static int opcode7C(CPU c) //LD A,H
        {
            c.A = c.H;
            return 4;
        }
        public static int opcode7D(CPU c) //LD A,L
        {
            c.A = c.L;
            return 4;
        }
        public static int opcode7E(CPU c) //LD A,(HL)
        {
            c.A = c.memory[c.HL];
            return 8;
        }
        public static int opcode40(CPU c) //LD B,B
        {
            return 4;
        }
        public static int opcode41(CPU c) //LD B,C
        {
            c.B = c.C;
            return 4;
        }
        public static int opcode42(CPU c) //LD B,D
        {
            c.B = c.D;
            return 4;
        }
        public static int opcode43(CPU c) //LD B,E
        {
            c.B = c.E;
            return 4;
        }
        public static int opcode44(CPU c) //LD B,H
        {
            c.B = c.H;
            return 4;
        }
        public static int opcode45(CPU c) //LD B,L
        {
            c.B = c.L;
            return 4;
        }
        public static int opcode46(CPU c) //LD B,(HL)
        {
            c.B = c.memory[c.HL];
            return 8;
        }
        public static int opcode48(CPU c) //LD C,B
        {
            c.C = c.B;
            return 4;
        }
        public static int opcode49(CPU c) //LD C,C
        {
            return 4;
        }
        public static int opcode4A(CPU c) //LD C,D
        {
            c.C = c.D;
            return 4;
        }
        public static int opcode4B(CPU c) //LD C,E
        {
            c.C = c.E;
            return 4;
        }
        public static int opcode4C(CPU c) //LD C,H
        {
            c.C = c.H;
            return 4;
        }
        public static int opcode4D(CPU c) //LD C,L
        {
            c.C = c.L;
            return 4;
        }
        public static int opcode4E(CPU c) //LD C,(HL)
        {
            c.C = c.memory[c.HL];
            return 8;
        }
        public static int opcode50(CPU c) //LD D,B
        {
            c.D = c.B;
            return 4;
        }
        public static int opcode51(CPU c) //LD D,C
        {
            c.D = c.C;
            return 4;
        }
        public static int opcode52(CPU c) //LD D,D
        {
            return 4;
        }
        public static int opcode53(CPU c) //LD D,E
        {
            c.D = c.E;
            return 4;
        }
        public static int opcode54(CPU c) //LD D,H
        {
            c.D = c.H;
            return 4;
        }
        public static int opcode55(CPU c) //LD D,L
        {
            c.D = c.L;
            return 4;
        }
        public static int opcode56(CPU c) //LD D,(HL)
        {
            c.D = c.memory[c.HL];
            return 8;
        }
        public static int opcode58(CPU c) //LD E,B
        {
            c.E = c.B;
            return 4;
        }
        public static int opcode59(CPU c) //LD E,C
        {
            c.E = c.C;
            return 4;
        }
        public static int opcode5A(CPU c) //LD E,D
        {
            c.E = c.D;
            return 4;
        }
        public static int opcode5B(CPU c) //LD E,E
        {
            return 4;
        }
        public static int opcode5C(CPU c) //LD E,H
        {
            c.E = c.H;
            return 4;
        }
        public static int opcode5D(CPU c) //LD E,L
        {
            c.E = c.L;
            return 4;
        }
        public static int opcode5E(CPU c) //LD E,(HL)
        {
            c.E = c.memory[c.HL];
            return 8;
        }
        public static int opcode60(CPU c) //LD H,B
        {
            c.H = c.B;
            return 4;
        }
        public static int opcode61(CPU c) //LD H,C
        {
            c.H = c.C;
            return 4;
        }
        public static int opcode62(CPU c) //LD H,D
        {
            c.H = c.D;
            return 4;
        }
        public static int opcode63(CPU c) //LD H,E
        {
            c.H = c.E;
            return 4;
        }
        public static int opcode64(CPU c) //LD H,H
        {
            return 4;
        }
        public static int opcode65(CPU c) //LD H,L
        {
            c.H = c.L;
            return 4;
        }
        public static int opcode66(CPU c) //LD H,(HL)
        {
            c.H = c.memory[c.HL];
            return 8;
        }
        public static int opcode68(CPU c) //LD L,B
        {
            c.L = c.B;
            return 4;
        }
        public static int opcode69(CPU c) //LD L,C
        {
            c.L = c.C;
            return 4;
        }
        public static int opcode6A(CPU c) //LD L,D
        {
            c.L = c.D;
            return 4;
        }
        public static int opcode6B(CPU c) //LD L,E
        {
            c.L = c.E;
            return 4;
        }
        public static int opcode6C(CPU c) //LD L,H
        {
            c.L = c.H;
            return 4;
        }
        public static int opcode6D(CPU c) //LD L,L
        {
            return 4;
        }
        public static int opcode6E(CPU c) //LD L,(HL)
        {
            c.L = c.memory[c.HL];
            return 8;
        }
        public static int opcode70(CPU c) //LD (HL),B
        {
            c.memory[c.HL] = c.B;
            return 8;
        }
        public static int opcode71(CPU c) //LD (HL),C
        {
            c.memory[c.HL] = c.C;
            return 8;
        }
        public static int opcode72(CPU c) //LD (HL),D
        {
            c.memory[c.HL] = c.D;
            return 8;
        }
        public static int opcode73(CPU c) //LD (HL),E
        {
            c.memory[c.HL] = c.E;
            return 8;
        }
        public static int opcode74(CPU c) //LD (HL),H
        {
            c.memory[c.HL] = c.H;
            return 8;
        }
        public static int opcode75(CPU c) //LD (HL),L
        {
            c.memory[c.HL] = c.L;
            return 8;
        }
        public static int opcode36(CPU c) //LD (HL),n
        {
            c.memory[c.HL] = c.memory[c.PC+1];
            return 12;
        }
        #endregion
        #region LD A,n
        public static int opcode0A(CPU c) //LD A, (BC)
        {
            c.A = c.memory[c.BC];
            return 8;
        }
        public static int opcode1A(CPU c) //LD A, (DE)
        {
            c.A = c.memory[c.DE];
            return 8;
        }
        public static int opcodeFA(CPU c) //LD A, (nn)
        {
            c.PC++;
            c.A = c.memory[c.memory.ReadLittleEndian(c.PC)];
            c.PC++;
            return 16;
        }
        public static int opcode3E(CPU c) //LD A, n
        {
            c.PC++;
            c.A = c.memory[c.PC];
            return 8;
        }
        #endregion
        #region LD n, A
        public static int opcode47(CPU c) //LD B, A
        {
            c.B = c.A;
            return 4;
        }
        public static int opcode4F(CPU c) //LD C, A
        {
            c.C = c.A;
            return 4;
        }
        public static int opcode57(CPU c) //LD D, A
        {
            c.D = c.A;
            return 4;
        }
        public static int opcode5F(CPU c) //LD E, A
        {
            c.E = c.A;
            return 4;
        }
        public static int opcode67(CPU c) //LD H, A
        {
            c.H = c.A;
            return 4;
        }
        public static int opcode6F(CPU c) //LD ", A
        {
            c.L = c.A;
            return 4;
        }
        public static int opcode02(CPU c) //LD (BC), A
        {
            c.memory[c.BC] = c.A;
            return 8;
        }
        public static int opcode12(CPU c) //LD (DE), A
        {
            c.memory[c.DE] = c.A;
            return 8;
        }
        public static int opcode77(CPU c) //LD (HL), A
        {
            c.memory[c.HL] = c.A;
            return 8;
        }
        public static int opcodeEA(CPU c) //LD (nn), A
        {
            c.PC++;
            c.memory[c.memory.ReadLittleEndian(c.PC)] = c.A;
            c.PC++;
            return 16;
        }
        #endregion
        #region LD (C),A,(C)
        public static int opcodeF2(CPU c) //LD A, (C)
        {
            c.A = c.memory[0xFF00+c.C];
            return 8;
        }
        public static int opcodeE2(CPU c) //LD (C), A
        {
            c.memory[0xFF00 + c.C] = c.A;
            return 8;
        }
        #endregion
        #region LDD/I (HL),A,(HL)
        public static int opcode3A(CPU c) //LDD A, (HL)
        {
            c.A = c.memory[c.HL];
            c.HL--;
            return 8;
        }
        public static int opcode32(CPU c) //LDD (HL) , A
        {
            c.memory[c.HL] = c.A;
            c.HL--;
            return 8;
        }
        public static int opcode2A(CPU c) //LDI A, (HL)
        {
            c.A = c.memory[c.HL];
            c.HL++;
            return 8;
        }
        public static int opcode22(CPU c) //LDI (HL), A
        {
            c.memory[c.HL] = c.A;
            c.HL++;
            return 8;
        }
        #endregion
        #region LDH (n),A,(n)
        public static int opcodeE0(CPU c) //LDH (n), A
        {
            c.PC++;
            c.memory[0xFF00 + c.memory[c.PC]] = c.A;
            return 12;
        }
        public static int opcodeF0(CPU c) //LDH A, (n)
        {
            c.PC++;
            c.A = c.memory[0xFF00 + c.memory[c.PC]];
            return 12;
        }
        #endregion
        #endregion
        #region 16-Bit Loads
        #region LD n,nn
        public static int opcode01(CPU c)
        {
            c.PC++;
            c.BC = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            return 12;
        }
        public static int opcode11(CPU c)
        {
            c.PC++;
            c.DE = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            return 12;
        }
        public static int opcode21(CPU c)
        {
            c.PC++;
            c.HL = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            return 12;
        }
        public static int opcode31(CPU c)
        {
            c.PC++;
            c.SP = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            return 12;
        }
        #endregion
        #region LD (nn),SP,HL/n
        public static int opcodeF9(CPU c) //LD SP,HP
        {
            c.SP = c.HL;
            return 8;
        }
        public static int opcodeF8(CPU c) //LDHL SP,n
        {
            c.PC++;
            int addr = c.HL + c.memory[c.PC];
            c.ZeroFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(addr);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.memory[c.PC]);
            return 12;
        }
        public static int opcode08(CPU c) //LD (nn),SP
        {
            c.PC++;
            int address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            c.memory.WriteLittleEndian(address, c.SP);
            return 20;
        }
        #endregion
        #region POP PUSH
        public static int opcodeF5(CPU c)//PUSH AF
        {
            c.PUSH(c.AF);
            return 16;
        }
        public static int opcodeC5(CPU c)//PUSH BC
        {
            c.PUSH(c.BC);
            return 16;
        }
        public static int opcodeD5(CPU c)//PUSH DE
        {
            c.PUSH(c.DE);
            return 16;
        }
        public static int opcodeE5(CPU c)//PUSH HL
        {
            c.PUSH(c.HL);
            return 16;
        }
        public static int opcodeF1(CPU c)//POP AF
        {
            c.AF = c.POP();
            return 12;
        }
        public static int opcodeC1(CPU c)//POP BC
        {
            c.BC = c.POP();
            return 12;
        }
        public static int opcodeD1(CPU c)//POP DE
        {
            c.DE = c.POP();
            return 12;
        }
        public static int opcodeE1(CPU c)//POP HL
        {
            c.HL = c.POP();
            return 12;
        }
        #endregion
        #endregion
        #region 8-Bit ALU
        #region ADD nn
        public byte ADD(byte number1, byte number2)
        {
            int result = number1 + number2;
            CarryFlag = GetCarry8(result);
            result = (byte)result;
            HalfCarryFlag = GetHalfCarry8(number1, number2);
            SubtractFlag = false;
            ZeroFlag = (result == 0);
            return (byte)result;
        }
        public static int opcode87(CPU c) //ADD A,A
        {
            c.A = c.ADD(c.A, c.A);
            return 4;
        }
        public static int opcode80(CPU c) //ADD A,B
        {
            c.A = c.ADD(c.A, c.B);
            return 4;
        }
        public static int opcode81(CPU c) //ADD A,C
        {
            c.A = c.ADD(c.A, c.C);
            return 4;
        }
        public static int opcode82(CPU c) //ADD A,D
        {
            c.A = c.ADD(c.A, c.D);
            return 4;
        }
        public static int opcode83(CPU c) //ADD A,E
        {
            c.A = c.ADD(c.A, c.E);
            return 4;
        }
        public static int opcode84(CPU c) //ADD A,H
        {
            c.A = c.ADD(c.A, c.H);
            return 4;
        }
        public static int opcode85(CPU c) //ADD A,L
        {
            c.A = c.ADD(c.A,c.L);
            return 4;
        }
        public static int opcode86(CPU c) //ADD A,(HL)
        {
            byte num = c.memory[c.HL];
            c.A = c.ADD(c.A, num);
            return 8;
        }
        public static int opcodeC6(CPU c) //ADD A,#
        {
            c.PC++;
            byte num = c.memory[c.PC];
            c.A = c.ADD(c.A, num);
            return 8;
        }
        #endregion
        #region ADC nn
        public byte ADC(byte number1, byte number2)
        {
            int result = number1 + number2 + (CarryFlag ? 1 : 0);
            CarryFlag = GetCarry8(result);
            result = (byte)result;
            HalfCarryFlag = GetHalfCarry8(number1, number2);
            SubtractFlag = false;
            ZeroFlag = (result == 0);
            return (byte)result;
        }
        public static int opcode8F(CPU c) //ADC A,A
        {
            c.A = c.ADC(c.A,c.A);
            return 4;
        }
        public static int opcode88(CPU c) //ADC A,B
        {
            c.A = c.ADC(c.A, c.B);
            return 4;
        }
        public static int opcode89(CPU c) //ADC A,C
        {
            c.A = c.ADC(c.A, c.C);
            return 4;
        }
        public static int opcode8A(CPU c) //ADC A,D
        {
            c.A = c.ADC(c.A, c.D);
            return 4;
        }
        public static int opcode8B(CPU c) //ADC A,E
        {
            c.A = c.ADC(c.A, c.E);
            return 4;
        }
        public static int opcode8C(CPU c) //ADC A,H
        {
            c.A = c.ADC(c.A, c.H);
            return 4;
        }
        public static int opcode8D(CPU c) //ADC A,L
        {
            c.A = c.ADC(c.A, c.L);
            return 4;
        }
        public static int opcode8E(CPU c) //ADC A,(HL)
        {
            byte num2 = c.memory[c.HL];
            c.A = c.ADC(c.A, num2);
            return 8;
        }
        public static int opcodeCE(CPU c) //ADC A,#
        {
            c.PC++;
            byte num2 = c.memory[c.PC];
            c.A = c.ADC(c.A, num2);
            return 8;
        }
        #endregion
        #region SUB nn
        public static int opcode97(CPU c) //SUB A,A
        {
            int result = c.A - c.A;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.A);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.A);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode90(CPU c) //SUB A,B
        {
            int result = c.A - c.B;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.B);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.B);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode91(CPU c) //SUB A,C
        {
            int result = c.A - c.C;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.C);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.C);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode92(CPU c) //SUB A,D
        {
            int result = c.A - c.D;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.D);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.D);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode93(CPU c) //SUB A,E
        {
            int result = c.A - c.E;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.E);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.E);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode94(CPU c) //SUB A,H
        {
            int result = c.A - c.H;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.H);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.H);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode95(CPU c) //SUB A,L
        {
            int result = c.A - c.L;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.L);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.L);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode96(CPU c) //SUB A,(HL)
        {
            int result = c.A - c.memory[c.HL];
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.memory[c.HL]);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.memory[c.HL]);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        public static int opcodeD6(CPU c) //SUB A,#
        {
            c.PC++;
            int num = c.memory[c.PC];
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        #endregion
        #region SBC nn
        public static int opcode9F(CPU c) //SBC A,A
        {
            var num = c.A + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode98(CPU c) //SBC A,B
        {
            var num = c.B + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode99(CPU c) //SBC A,C
        {
            var num = c.C + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode9A(CPU c) //SBC A,D
        {
            var num = c.D + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode9B(CPU c) //SBC A,E
        {
            var num = c.E + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode9C(CPU c) //SBC A,H
        {
            var num = c.H + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode9D(CPU c) //SBC A,L
        {
            var num = c.L + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode9E(CPU c) //SBC A,(HL)
        {
            var num = c.memory[c.HL] + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        public static int opcodeSBCANUNKNOWN(CPU c) //SBC A, #
        {
            c.PC++;
            var num = c.memory[c.PC] + (c.CarryFlag ? 1 : 0);
            int result = c.A - num;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, num);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, num);
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        #endregion
        #region AND nn
        public static int opcodeA7(CPU c) //AND A,A
        {
            int result = c.A & c.A;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA0(CPU c) //AND A,B
        {
            int result = c.A & c.B;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA1(CPU c) //AND A,C
        {
            int result = c.A & c.C;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA2(CPU c) //AND A,D
        {
            int result = c.A & c.D;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA3(CPU c) //AND A,E
        {
            int result = c.A & c.E;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA4(CPU c) //AND A,H
        {
            int result = c.A & c.H;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA5(CPU c) //AND A,L
        {
            int result = c.A & c.L;
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA6(CPU c) //AND A,(HL)
        {
            int result = c.A & c.memory[c.HL];
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        public static int opcodeE6(CPU c) //AND A,#
        {
            c.PC++;
            int result = c.A & c.memory[c.PC];
            c.HalfCarryFlag = true;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        #endregion
        #region OR nn
        public static int opcodeB7(CPU c) //OR A,A
        {
            int result = c.A | c.A;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB0(CPU c) //OR A,B
        {
            int result = c.A | c.B;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB1(CPU c) //OR A,C
        {
            int result = c.A | c.C;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB2(CPU c) //OR A,D
        {
            int result = c.A | c.D;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB3(CPU c) //OR A,E
        {
            int result = c.A | c.E;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB4(CPU c) //OR A,H
        {
            int result = c.A | c.H;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB5(CPU c) //OR A,L
        {
            int result = c.A | c.L;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeB6(CPU c) //OR A,(HL)
        {
            int result = c.A | c.memory[c.HL];
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        public static int opcodeF6(CPU c) //OR A,#
        {
            c.PC++;
            int result = c.A | c.memory[c.PC];
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        #endregion
        #region XOR nn
        public static int opcodeAF(CPU c) //XOR A,A
        {
            int result = c.A ^ c.A;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA8(CPU c) //XOR A,B
        {
            int result = c.A ^ c.B;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeA9(CPU c) //XOR A,C
        {
            int result = c.A ^ c.C;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeAA(CPU c) //XOR A,D
        {
            int result = c.A ^ c.D;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeAB(CPU c) //XOR A,E
        {
            int result = c.A ^ c.E;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeAC(CPU c) //XOR A,H
        {
            int result = c.A ^ c.H;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeAD(CPU c) //XOR A,L
        {
            int result = c.A ^ c.L;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcodeAE(CPU c) //XOR A,(HL)
        {
            int result = c.A ^ c.memory[c.HL];
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        public static int opcodeEE(CPU c) //XOR A,#
        {
            c.PC++;
            int result = c.A ^ c.memory[c.PC];
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 8;
        }
        #endregion
        #region CP nn
        public static int opcodeBF(CPU c) //CP A,A
        {
            int result = c.A - c.A;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.A);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.A);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeB8(CPU c) //CP A,B
        {
            int result = c.A - c.B;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.B);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.B);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeB9(CPU c) //CP A,C
        {
            int result = c.A - c.C;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.C);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.C);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeBA(CPU c) //CP A,D
        {
            int result = c.A - c.D;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.D);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.D);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeBB(CPU c) //CP A,E
        {
            int result = c.A - c.E;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.E);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.E);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeBC(CPU c) //CP A,H
        {
            int result = c.A - c.H;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.H);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.H);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeBD(CPU c) //CP A,L
        {
            int result = c.A - c.L;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.L);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.L);
            c.ZeroFlag = (result == 0);
            return 4;
        }
        public static int opcodeBE(CPU c) //CP A,(HL)
        {
            int result = c.A - c.memory[c.HL];
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.memory[c.HL]);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.memory[c.HL]);
            c.ZeroFlag = (result == 0);
            return 8;
        }
        public static int opcodeFE(CPU c) //CP A,#
        {
            c.PC++;
            int result = c.A - c.memory[c.PC];
            c.HalfCarryFlag = GetHalfBorrow8(c.A, c.memory[c.PC]);
            c.SubtractFlag = true;
            c.CarryFlag = GetBorrow(c.A, c.memory[c.PC]);
            c.ZeroFlag = (result == 0);
            return 8;
        }
        #endregion
        #region INC nn
        public static int opcode3C(CPU c) //INC A
        {
            int result = c.A + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.A, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode04(CPU c) //INC B
        {
            int result = c.B + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.B, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.B = (byte)result;
            return 4;
        }
        public static int opcode0C(CPU c) //INC C
        {
            int result = c.C + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.C, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.C = (byte)result;
            return 4;
        }
        public static int opcode14(CPU c) //INC D
        {
            int result = c.D + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.D, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.D = (byte)result;
            return 4;
        }
        public static int opcode1C(CPU c) //INC E
        {
            int result = c.E + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.E, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.E = (byte)result;
            return 4;
        }
        public static int opcode24(CPU c) //INC H
        {
            int result = c.H + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.H, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.H = (byte)result;
            return 4;
        }
        public static int opcode2C(CPU c) //INC L
        {
            int result = c.L + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.L, 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.L = (byte)result;
            return 4;
        }
        public static int opcode34(CPU c) //INC (HL)
        {
            int result = c.memory[c.HL] + 1;
            c.HalfCarryFlag = GetHalfCarry8(c.memory[c.HL], 1);
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.memory[c.HL] = (byte)result;
            return 12;
        }
        #endregion
        #region DEC nn
        public static int opcode3D(CPU c) //DEC A
        {
            int result = c.A - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.A, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.A = (byte)result;
            return 4;
        }
        public static int opcode05(CPU c) //DEC A
        {
            int result = c.B - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.B, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.B = (byte)result;
            return 4;
        }
        public static int opcode0D(CPU c) //DEC C
        {
            int result = c.C - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.C, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.C = (byte)result;
            return 4;
        }
        public static int opcode15(CPU c) //DEC D
        {
            int result = c.D - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.D, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.D = (byte)result;
            return 4;
        }
        public static int opcode1D(CPU c) //DEC E
        {
            int result = c.E - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.E, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.E = (byte)result;
            return 4;
        }
        public static int opcode25(CPU c) //DEC H
        {
            int result = c.H - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.H, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.H = (byte)result;
            return 4;
        }
        public static int opcode2D(CPU c) //DEC L
        {
            int result = c.L - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.L, 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.L = (byte)result;
            return 4;
        }
        public static int opcode35(CPU c) //DEC (HL)
        {
            int result = c.memory[c.HL] - 1;
            c.HalfCarryFlag = GetHalfBorrow8(c.memory[c.HL], 1);
            c.SubtractFlag = true;
            c.ZeroFlag = (result == 0);
            c.memory[c.HL] = (byte)result;
            return 12;
        }
        #endregion
        #endregion
        #region 16-bit Arithmetic
        #region ADD HL,n
        public static int opcode09(CPU c) //ADD HL,BC
        {
            int result = c.HL + c.BC;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(result);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.BC);
            return 8;
        }
        public static int opcode19(CPU c) //ADD HL,DE
        {
            int result = c.HL + c.DE;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(result);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.DE);
            return 8;
        }
        public static int opcode29(CPU c) //ADD HL,HL
        {
            int result = c.HL + c.HL;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(result);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.HL);
            return 8;
        }
        public static int opcode39(CPU c) //ADD HL,SP
        {
            int result = c.HL + c.SP;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(result);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.SP);
            return 8;
        }
        #endregion
        public static int opcodeE8(CPU c) //ADD SP,n
        {
            c.PC++;
            int result = c.HL + c.memory[c.PC];
            c.ZeroFlag = false;
            c.SubtractFlag = false;
            c.CarryFlag = GetCarry16(result);
            c.HalfCarryFlag = GetHalfCarry16(c.HL, c.memory[c.PC]);
            return 16;
        }
        #region INC nn
        public static int opcode03(CPU c) //INC BC
        {
            c.BC++;
            return 8;
        }
        public static int opcode13(CPU c) //INC DE
        {
            c.DE++;
            return 8;
        }
        public static int opcode23(CPU c) //INC HL
        {
            c.HL++;
            return 8;
        }
        public static int opcode33(CPU c) //INC SP
        {
            c.SP++;
            return 8;
        }
        #endregion
        #region DEC nn
        public static int opcode0B(CPU c) //DEC BC
        {
            c.BC--;
            return 8;
        }
        public static int opcode1B(CPU c) //DEC DE
        {
            c.DE--;
            return 8;
        }
        public static int opcode2B(CPU c) //DEC HL
        {
            c.HL--;
            return 8;
        }
        public static int opcode3B(CPU c) //DEC SP
        {
            c.SP--;
            return 8;
        }
        #endregion
        #endregion
        #region Miscellaneous
        #region SWAP
        public static int opcodeCB37(CPU c) //SWAP A
        {
            byte result = c.A;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.A = result;
            return 8;
        }
        public static int opcodeCB30(CPU c) //SWAP B
        {
            byte result = c.B;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.B = result;
            return 8;
        }
        public static int opcodeCB31(CPU c) //SWAP C
        {
            byte result = c.C;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.C = result;
            return 8;
        }
        public static int opcodeCB32(CPU c) //SWAP D
        {
            byte result = c.D;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.D = result;
            return 8;
        }
        public static int opcodeCB33(CPU c) //SWAP E
        {
            byte result = c.E;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.E = result;
            return 8;
        }
        public static int opcodeCB34(CPU c) //SWAP H
        {
            byte result = c.H;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.H = result;
            return 8;
        }
        public static int opcodeCB35(CPU c) //SWAP L
        {
            byte result = c.L;
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.L = result;
            return 8;
        }
        public static int opcodeCB36(CPU c) //SWAP (HL)
        {
            byte result = c.memory[c.HL];
            result = (byte)(((result & 0xF) << 4) + ((result >> 4) & 0xF));
            c.HalfCarryFlag = false;
            c.CarryFlag = false;
            c.SubtractFlag = false;
            c.ZeroFlag = (result == 0);
            c.memory[c.HL] = result;
            return 16;
        }
        #endregion
        public static int opcode27(CPU c) //DAA
        {
            int result = c.A;
            if (!c.SubtractFlag)
            {
                if (c.HalfCarryFlag && ((result & 0xF) > 9))
                {
                    result += 0x6;
                }
                if (c.CarryFlag && ((result & 0xF0) > 0x90))
                {
                    result += 0x6;
                }
            }
            else
            {
                if (c.HalfCarryFlag) result = (result - 6) & 0xFF;
                if (c.CarryFlag) result -= 0x60;
            }

            c.HalfCarryFlag = false;
            c.CarryFlag = GetCarry8(result);
            c.ZeroFlag = (result == 0);
            c.A = (byte)(result & 0xFF);
            return 4;
        }
        public static int opcode2F(CPU c) //CPL
        {
            c.A = (byte)(c.A ^ 0xFF);
            c.SubtractFlag = true;
            c.HalfCarryFlag = true;
            return 4;
        }
        public static int opcode3F(CPU c) //CCF
        {
            c.CarryFlag = !c.CarryFlag;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        public static int opcode37(CPU c) //SCF
        {
            c.CarryFlag = true;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        public static int opcode00(CPU c) //NOP
        {
            return 4;
        }
        public static int opcode76(CPU c) //HALT
        {
            c.Halt();
            return 4;
        }
        public static int opcode10(CPU c) //STOP
        {
            c.PC++;
            c.Stop();
            return 4;
        }
        public static int opcodeF3(CPU c) //DI
        {
            c.InterruptsNext = false;
            return 4;
        }
        public static int opcodeFB(CPU c) //EI
        {
            c.InterruptsNext = true;
            return 4;
        }
        #endregion
        #region Rotates & Shifts
        public static int opcode07(CPU c) //RLCA
        {
            c.CarryFlag = ((c.A & 0x80) == 0x80);
            c.A = (byte)(c.A << 1);
            c.A |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = c.A == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        public static int opcode17(CPU c) //RLA
        {
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((c.A & 0x80) == 0x80);
            c.A = (byte)(c.A << 1);
            c.A |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = c.A == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        public static int opcode0F(CPU c) //RRCA
        {
            c.CarryFlag = ((c.A & 0x1) == 0x1);
            c.A = (byte)(c.A >> 1);
            c.A |= (byte)((c.CarryFlag)?0x80:0);
            c.ZeroFlag = c.A == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        public static int opcode1F(CPU c) //RRA
        {
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((c.A & 0x1) == 0x1);
            c.A = (byte)(c.A << 1);
            c.A |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = c.A == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            return 4;
        }
        #region RLC n
        public static int opcodeCB07(CPU c) //RLC A
        {
            byte value = c.A;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB00(CPU c) //RLC B
        {
            byte value = c.B;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB01(CPU c) //RLC C
        {
            byte value = c.C;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB02(CPU c) //RLC D
        {
            byte value = c.D;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB03(CPU c) //RLC E
        {
            byte value = c.E;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB04(CPU c) //RLC H
        {
            byte value = c.H;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB05(CPU c) //RLC L
        {
            byte value = c.L;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB06(CPU c) //RLC (HL)
        {
            byte value = c.memory[c.HL];
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((c.CarryFlag) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }

        #endregion
        #region RL n
        public static int opcodeCB17(CPU c) //RL A
        {
            byte value = c.A;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB10(CPU c) //RL B
        {
            byte value = c.B;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }

        public static int opcodeCB11(CPU c) //RL C
        {
            byte value = c.C;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }

        public static int opcodeCB12(CPU c) //RL D
        {
            byte value = c.D;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }

        public static int opcodeCB13(CPU c) //RL E
        {
            byte value = c.E;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }

        public static int opcodeCB14(CPU c) //RL H
        {
            byte value = c.H;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }

        public static int opcodeCB15(CPU c) //RL L
        {
            byte value = c.L;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }

        public static int opcodeCB16(CPU c) //RL (HL)
        {
            byte value = c.memory[c.HL];
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value |= (byte)((oldCarry) ? 1 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }

        #endregion
        #region RRC n
        public static int opcodeCB0F(CPU c) //RRC A
        {
            byte value = c.A;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB08(CPU c) //RRC B
        {
            byte value = c.B;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB09(CPU c) //RRC C
        {
            byte value = c.C;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB0A(CPU c) //RRC D
        {
            byte value = c.D;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB0B(CPU c) //RRC E
        {
            byte value = c.E;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB0C(CPU c) //RRC H
        {
            byte value = c.H;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB0D(CPU c) //RRC L
        {
            byte value = c.L;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB0E(CPU c) //RRC (HL)
        {
            byte value = c.memory[c.HL];
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((c.CarryFlag) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }
        #endregion
        #region RR n
        public static int opcodeCB1F(CPU c) //RR A
        {
            byte value = c.A;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB18(CPU c) //RR B
        {
            byte value = c.B;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB19(CPU c) //RR C
        {
            byte value = c.C;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB1A(CPU c) //RR D
        {
            byte value = c.D;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB1B(CPU c) //RR E
        {
            byte value = c.E;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB1C(CPU c) //RR H
        {
            byte value = c.H;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB1D(CPU c) //RR L
        {
            byte value = c.L;
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB1E(CPU c) //RR (HL)
        {
            byte value = c.memory[c.HL];
            bool oldCarry = c.CarryFlag;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= (byte)((oldCarry) ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }
        #endregion
        #region SLA n
        public static int opcodeCB27(CPU c) //SLA A
        {
            byte value = c.A;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB20(CPU c) //SLA B
        {
            byte value = c.B;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB21(CPU c) //SLA C
        {
            byte value = c.C;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB22(CPU c) //SLA D
        {
            byte value = c.D;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB23(CPU c) //SLA E
        {
            byte value = c.E;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB24(CPU c) //SLA H
        {
            byte value = c.H;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB25(CPU c) //SLA L
        {
            byte value = c.L;
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB26(CPU c) //SLA (HL)
        {
            byte value = c.memory[c.HL];
            c.CarryFlag = ((value & 0x80) == 0x80);
            value = (byte)(value << 1);
            value &= 0b11111110;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }
        #endregion
        #region SRA n
        public static int opcodeCB2F(CPU c) //SRA A
        {
            byte value = c.A;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb?0x80:0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB28(CPU c) //SRA B
        {
            byte value = c.B;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB29(CPU c) //SRA C
        {
            byte value = c.C;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB2A(CPU c) //SRA D
        {
            byte value = c.D;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB2B(CPU c) //SRA E
        {
            byte value = c.E;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB2C(CPU c) //SRA H
        {
            byte value = c.H;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB2D(CPU c) //SRA L
        {
            byte value = c.L;
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB2E(CPU c) //SRA (HL)
        {
            byte value = c.memory[c.HL];
            c.CarryFlag = ((value & 0x1) == 0x1);
            bool oldmsb = ((value & 0x80) == 0x80);
            value = (byte)(value >> 1);
            value |= (byte)(oldmsb ? 0x80 : 0);
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }
        #endregion
        #region SRL n
        public static int opcodeCB3F(CPU c) //SRL A
        {
            byte value = c.A;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.A = value;
            return 8;
        }
        public static int opcodeCB38(CPU c) //SRL B
        {
            byte value = c.B;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.B = value;
            return 8;
        }
        public static int opcodeCB39(CPU c) //SRL C
        {
            byte value = c.C;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.C = value;
            return 8;
        }
        public static int opcodeCB3A(CPU c) //SRL D;
        {
            byte value = c.D;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.D = value;
            return 8;
        }
        public static int opcodeCB3B(CPU c) //SRL E
        {
            byte value = c.E;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.E = value;
            return 8;
        }
        public static int opcodeCB3C(CPU c) //SRL H
        {
            byte value = c.H;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.H = value;
            return 8;
        }
        public static int opcodeCB3D(CPU c) //SRL L
        {
            byte value = c.L;
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.L = value;
            return 8;
        }
        public static int opcodeCB3E(CPU c) //SRL (HL)
        {
            byte value = c.memory[c.HL];
            c.CarryFlag = ((value & 0x1) == 0x1);
            value = (byte)(value >> 1);
            value |= 0b01111111;
            c.ZeroFlag = value == 0;
            c.HalfCarryFlag = false;
            c.SubtractFlag = false;
            c.memory[c.HL] = value;
            return 16;
        }
        #endregion
        #endregion
        #region Bit Operations
        #region BIT b,r
        #region BIT 0, r
        public static int opcodeCB47(CPU c) //BIT 0,c.A
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB40(CPU c) //BIT 0,c.B
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB41(CPU c) //BIT 0,c.C
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB42(CPU c) //BIT 0,c.D
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB43(CPU c) //BIT 0,c.E
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB44(CPU c) //BIT 0,c.H
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB45(CPU c) //BIT 0,c.L
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB46(CPU c) //BIT 0,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 1, r
        public static int opcodeCB4F(CPU c) //BIT 1,c.A
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB48(CPU c) //BIT 1,c.B
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB49(CPU c) //BIT 1,c.C
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB4A(CPU c) //BIT 1,c.D
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB4B(CPU c) //BIT 1,c.E
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB4C(CPU c) //BIT 1,c.H
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB4D(CPU c) //BIT 1,c.L
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB4E(CPU c) //BIT 1,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 2, r
        public static int opcodeCB57(CPU c) //BIT 2,c.A
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB50(CPU c) //BIT 2,c.B
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB51(CPU c) //BIT 2,c.C
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB52(CPU c) //BIT 2,c.D
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB53(CPU c) //BIT 2,c.E
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB54(CPU c) //BIT 2,c.H
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB55(CPU c) //BIT 2,c.L
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB56(CPU c) //BIT 2,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 3, r
        public static int opcodeCB5F(CPU c) //BIT 3,c.A
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB58(CPU c) //BIT 3,c.B
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB59(CPU c) //BIT 3,c.C
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB5A(CPU c) //BIT 3,c.D
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB5B(CPU c) //BIT 3,c.E
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB5C(CPU c) //BIT 3,c.H
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB5D(CPU c) //BIT 3,c.L
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB5E(CPU c) //BIT 3,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 4, r
        public static int opcodeCB67(CPU c) //BIT 4,c.A
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB60(CPU c) //BIT 4,c.B
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB61(CPU c) //BIT 4,c.C
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB62(CPU c) //BIT 4,c.D
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB63(CPU c) //BIT 4,c.E
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB64(CPU c) //BIT 4,c.H
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB65(CPU c) //BIT 4,c.L
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB66(CPU c) //BIT 4,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 5, r
        public static int opcodeCB6F(CPU c) //BIT 5,c.A
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB68(CPU c) //BIT 5,c.B
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB69(CPU c) //BIT 5,c.C
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB6A(CPU c) //BIT 5,c.D
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB6B(CPU c) //BIT 5,c.E
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB6C(CPU c) //BIT 5,c.H
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB6D(CPU c) //BIT 5,c.L
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB6E(CPU c) //BIT 5,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 6, r
        public static int opcodeCB77(CPU c) //BIT 6,c.A
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB70(CPU c) //BIT 6,c.B
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB71(CPU c) //BIT 6,c.C
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB72(CPU c) //BIT 6,c.D
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB73(CPU c) //BIT 6,c.E
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB74(CPU c) //BIT 6,c.H
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB75(CPU c) //BIT 6,c.L
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB76(CPU c) //BIT 6,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #region BIT 7, r
        public static int opcodeCB7F(CPU c) //BIT 7,c.A
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.A & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB78(CPU c) //BIT 7,c.B
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.B & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB79(CPU c) //BIT 7,c.C
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.C & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB7A(CPU c) //BIT 7,c.D
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.D & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB7B(CPU c) //BIT 7,c.E
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.E & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB7C(CPU c) //BIT 7,c.H
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.H & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB7D(CPU c) //BIT 7,c.L
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.L & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 8;
        }
        public static int opcodeCB7E(CPU c) //BIT 7,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.ZeroFlag = ((c.memory[c.HL] & bit) == 0);

            c.SubtractFlag = false;
            c.HalfCarryFlag = true;
            return 16;
        }
        #endregion
        #endregion
        #region SET b,r
        #region SET 0, r
        public static int opcodeCBC7(CPU c) //SET 0,c.A
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBC0(CPU c) //SST 0,c.B
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBC1(CPU c) //SST 0,c.C
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBC2(CPU c) //SST 0,c.D
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBC3(CPU c) //SST 0,c.E
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBC4(CPU c) //SST 0,c.H
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBC5(CPU c) //SST 0,c.L
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBC6(CPU c) //SST 0,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 1, r
        public static int opcodeCBCF(CPU c) //SET 1,c.A
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBC8(CPU c) //SST 1,c.B
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBC9(CPU c) //SST 1,c.C
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBCA(CPU c) //SST 1,c.D
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBCB(CPU c) //SST 1,c.E
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBCC(CPU c) //SST 1,c.H
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBCD(CPU c) //SST 1,c.L
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBCE(CPU c) //SST 1,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 2, r
        public static int opcodeCBD7(CPU c) //SET 2,c.A
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBD0(CPU c) //SST 2,c.B
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBD1(CPU c) //SST 2,c.C
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBD2(CPU c) //SST 2,c.D
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBD3(CPU c) //SST 2,c.E
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBD4(CPU c) //SST 2,c.H
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBD5(CPU c) //SST 2,c.L
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBD6(CPU c) //SST 2,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 3, r
        public static int opcodeCBDF(CPU c) //SET 3,c.A
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBD8(CPU c) //SST 3,c.B
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBD9(CPU c) //SST 3,c.C
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBDA(CPU c) //SST 3,c.D
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBDB(CPU c) //SST 3,c.E
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBDC(CPU c) //SST 3,c.H
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBDD(CPU c) //SST 3,c.L
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBDE(CPU c) //SST 3,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 4, r
        public static int opcodeCBE7(CPU c) //SET 4,c.A
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBE0(CPU c) //SST 4,c.B
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBE1(CPU c) //SST 4,c.C
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBE2(CPU c) //SST 4,c.D
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBE3(CPU c) //SST 4,c.E
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBE4(CPU c) //SST 4,c.H
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBE5(CPU c) //SST 4,c.L
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBE6(CPU c) //SST 4,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 5, r
        public static int opcodeCBEF(CPU c) //SET 5,c.A
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBE8(CPU c) //SST 5,c.B
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBE9(CPU c) //SST 5,c.C
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBEA(CPU c) //SST 5,c.D
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBEB(CPU c) //SST 5,c.E
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBEC(CPU c) //SST 5,c.H
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBED(CPU c) //SST 5,c.L
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBEE(CPU c) //SST 5,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 6, r
        public static int opcodeCBF7(CPU c) //SET 6,c.A
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBF0(CPU c) //SST 6,c.B
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBF1(CPU c) //SST 6,c.C
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBF2(CPU c) //SST 6,c.D
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBF3(CPU c) //SST 6,c.E
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBF4(CPU c) //SST 6,c.H
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBF5(CPU c) //SST 6,c.L
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBF6(CPU c) //SST 6,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion
        #region SET 7, r
        public static int opcodeCBFF(CPU c) //SET 7,c.A
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.A |= bit;

            return 8;
        }
        public static int opcodeCBF8(CPU c) //SST 7,c.B
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.B |= bit;

            return 8;
        }
        public static int opcodeCBF9(CPU c) //SST 7,c.C
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.C |= bit;

            return 8;
        }
        public static int opcodeCBFA(CPU c) //SST 7,c.D
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.D |= bit;

            return 8;
        }
        public static int opcodeCBFB(CPU c) //SST 7,c.E
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.E |= bit;

            return 8;
        }
        public static int opcodeCBFC(CPU c) //SST 7,c.H
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.H |= bit;

            return 8;
        }
        public static int opcodeCBFD(CPU c) //SST 7,c.L
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.L |= bit;

            return 8;
        }
        public static int opcodeCBFE(CPU c) //SST 7,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.memory[c.HL] |= bit;

            return 16;
        }
        #endregion

        #endregion
        #region RES b,r
        #region RES 0, r
        public static int opcodeCB87(CPU c) //RES 0,c.A
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB80(CPU c) //RES 0,c.B
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB81(CPU c) //RES 0,c.C
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB82(CPU c) //RES 0,c.D
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB83(CPU c) //RES 0,c.E
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB84(CPU c) //RES 0,c.H
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB85(CPU c) //RES 0,c.L
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB86(CPU c) //RES 0,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 0);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 1, r
        public static int opcodeCB8F(CPU c) //RES 1,c.A
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB88(CPU c) //RES 1,c.B
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB89(CPU c) //RES 1,c.C
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB8A(CPU c) //RES 1,c.D
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB8B(CPU c) //RES 1,c.E
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB8C(CPU c) //RES 1,c.H
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB8D(CPU c) //RES 1,c.L
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB8E(CPU c) //RES 1,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 1);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 2, r
        public static int opcodeCB97(CPU c) //RES 2,c.A
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB90(CPU c) //RES 2,c.B
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB91(CPU c) //RES 2,c.C
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB92(CPU c) //RES 2,c.D
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB93(CPU c) //RES 2,c.E
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB94(CPU c) //RES 2,c.H
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB95(CPU c) //RES 2,c.L
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB96(CPU c) //RES 2,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 2);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 3, r
        public static int opcodeCB9F(CPU c) //RES 3,c.A
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB98(CPU c) //RES 3,c.B
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB99(CPU c) //RES 3,c.C
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB9A(CPU c) //RES 3,c.D
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB9B(CPU c) //RES 3,c.E
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB9C(CPU c) //RES 3,c.H
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB9D(CPU c) //RES 3,c.L
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCB9E(CPU c) //RES 3,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 3);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 4, r
        public static int opcodeCBA7(CPU c) //RES 4,c.A
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA0(CPU c) //RES 4,c.B
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA1(CPU c) //RES 4,c.C
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA2(CPU c) //RES 4,c.D
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA3(CPU c) //RES 4,c.E
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA4(CPU c) //RES 4,c.H
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA5(CPU c) //RES 4,c.L
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA6(CPU c) //RES 4,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 4);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 5, r
        public static int opcodeCBAF(CPU c) //RES 5,c.A
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA8(CPU c) //RES 5,c.B
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBA9(CPU c) //RES 5,c.C
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBAA(CPU c) //RES 5,c.D
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBAB(CPU c) //RES 5,c.E
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBAC(CPU c) //RES 5,c.H
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBAD(CPU c) //RES 5,c.L
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBAE(CPU c) //RES 5,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 5);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 6, r
        public static int opcodeCBB7(CPU c) //RES 6,c.A
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB0(CPU c) //RES 6,c.B
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB1(CPU c) //RES 6,c.C
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB2(CPU c) //RES 6,c.D
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB3(CPU c) //RES 6,c.E
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB4(CPU c) //RES 6,c.H
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB5(CPU c) //RES 6,c.L
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB6(CPU c) //RES 6,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 6);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion
        #region RES 7, r
        public static int opcodeCBBF(CPU c) //RES 7,c.A
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.A &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB8(CPU c) //RES 7,c.B
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.B &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBB9(CPU c) //RES 7,c.C
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.C &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBBA(CPU c) //RES 7,c.D
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.D &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBBB(CPU c) //RES 7,c.E
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.E &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBBC(CPU c) //RES 7,c.H
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.H &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBBD(CPU c) //RES 7,c.L
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.L &= (byte)(0xFF - bit);

            return 8;
        }
        public static int opcodeCBBE(CPU c) //RES 7,c.memory[c.HL]
        {
            byte bit = (byte)Math.Pow(2, 7);

            c.memory[c.HL] &= (byte)(0xFF - bit);

            return 16;
        }
        #endregion

        #endregion
        #endregion
        #region Jumps
        public static int opcodeC3(CPU c) //JP nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC = address;
            c.PC--;
            return 12;
        }
        public static int opcodeC2(CPU c) //JP NZ,nn
        {
            if (!c.ZeroFlag)
            {
                c.PC++;
                ushort address = c.memory.ReadLittleEndian(c.PC);
                c.PC = address;
                c.PC--;
            }
            else c.PC += 2;
            return 12;
        }
        public static int opcodeCA(CPU c) //JP Z,nn
        {
            if (c.ZeroFlag)
            {
                c.PC++;
                ushort address = c.memory.ReadLittleEndian(c.PC);
                c.PC = address;
                c.PC--;
            }
            else c.PC += 2;
            return 12;
        }
        public static int opcodeD2(CPU c) //JP NC,nn
        {
            if (!c.CarryFlag)
            {
                c.PC++;
                ushort address = c.memory.ReadLittleEndian(c.PC);
                c.PC = address;
                c.PC--;
            }
            else c.PC += 2;
            return 12;
        }
        public static int opcodeDA(CPU c) //JP C,nn
        {
            if (c.CarryFlag)
            {
                c.PC++;
                ushort address = c.memory.ReadLittleEndian(c.PC);
                c.PC = address;
                c.PC--;
            }
            else c.PC += 2;
            return 12;
        }
        public static int opcodeE9(CPU c) //JP (HL),nn
        {
            ushort address = c.HL;
            c.PC = address;
            c.PC--;
            return 4;
        }
        public static int opcode18(CPU c) //JR n
        {
            c.PC++;
            ushort address = (ushort)((SByte)c.memory[c.PC]);
            c.PC += address;
            return 8;
        }
        public static int opcode20(CPU c) //JR NZ,n
        {
            if (!c.ZeroFlag)
            {
                c.PC++;
                ushort address = (ushort)((SByte)c.memory[c.PC]);
                c.PC += address;
            }
            else
            {
                c.PC++;
            }
            return 8;
        }
        public static int opcode28(CPU c) //JR Z,n
        {
            if (c.ZeroFlag)
            {
                c.PC++;
                ushort address = (ushort)((SByte)c.memory[c.PC]);
                c.PC += address;
            }
            else
            {
                c.PC++;
            }
            return 8;
        }
        public static int opcode30(CPU c) //JR NC,n
        {
            if (!c.CarryFlag)
            {
                c.PC++;
                ushort address = (ushort)((SByte)c.memory[c.PC]);
                c.PC += address;
            }
            else
            {
                c.PC++;
            }
            return 8;
        }
        public static int opcode38(CPU c) //JR C,n
        {
            if (c.CarryFlag)
            {
                c.PC++;
                ushort address = (ushort)((SByte)c.memory[c.PC]);
                c.PC += address;
            }
            else
            {
                c.PC++;
            }
            return 8;
        }
        #endregion
        #region Calls
        public static int opcodeCD(CPU c) //CALL nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            c.Call(address);
            return 12;
        }
        public static int opcodeC4(CPU c) //CALL NZ,nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            if (!c.ZeroFlag)
            {
                c.Call(address);
            }
            return 12;
        }
        public static int opcodeCC(CPU c) //CALL Z,nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            if (c.ZeroFlag)
            {
                c.Call(address);
            }
            return 12;
        }
        public static int opcodeD4(CPU c) //CALL NC,nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            if (!c.CarryFlag)
            {
                c.Call(address);
            }
            return 12;
        }
        public static int opcodeDC(CPU c) //CALL C,nn
        {
            c.PC++;
            ushort address = c.memory.ReadLittleEndian(c.PC);
            c.PC++;
            if (c.CarryFlag)
            {
                c.Call(address);
            }
            return 12;
        }
        #endregion
        #region Restarts
        public static int opcodeC7(CPU c) //RST 00H
        {
            c.Call((ushort)(0));
            return 32;
        }
        public static int opcodeCF(CPU c) //RST 08H
        {
            c.Call(8);
            return 32;
        }
        public static int opcodeD7(CPU c) //RST 10H
        {
            c.Call(0x10);
            return 32;
        }
        public static int opcodeDF(CPU c) //RST 18H
        {
            c.Call(0x18);
            return 32;
        }
        public static int opcodeE7(CPU c) //RST 20H
        {
            c.Call(0x20);
            return 32;
        }
        public static int opcodeEF(CPU c) //RST 28H
        {
            c.Call(0x28);
            return 32;
        }
        public static int opcodeF7(CPU c) //RST 30H
        {
            c.Call(0x30);
            return 32;
        }
        public static int opcodeFF(CPU c) //RST 38H
        {
            c.Call(0x38);
            return 32;
        }
        #endregion
        #region Returns
        public static int opcodeC9(CPU c) //RET
        {
            c.Return();
            return 8;
        }
        public static int opcodeC0(CPU c) //RET NZ
        {
            if (!c.ZeroFlag) c.Return();
            return 8;
        }
        public static int opcodeC8(CPU c) //RET Z
        {
            if (c.ZeroFlag) c.Return();
            return 8;
        }
        public static int opcodeD0(CPU c) //RET NC
        {
            if (!c.CarryFlag) c.Return();
            return 8;
        }
        public static int opcodeD8(CPU c) //RET C
        {
            if (c.CarryFlag) c.Return();
            return 8;
        }
        public static int opcodeD9(CPU c) //RETI
        {
            c.Return();
            c.InterruptsNext = true;
            return 8;
        }
        #endregion
        #endregion
    }
}
    