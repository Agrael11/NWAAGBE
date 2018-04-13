using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GB_Emu
{
    public partial class Form1 : Form
    {
        public static Form1 Instance;
        public CPU cpu = new CPU();

        public Form1()
        {
            InitializeComponent();
            Form1.Instance = this;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "GameBoy|*.gb|All Files|*.*";
            dialog.Multiselect = false;
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                MBCs.MBC mbc = MBCFactory.Build(System.IO.File.ReadAllBytes(dialog.FileName));
                Memory mem = new Memory();
                mem.MBC = mbc;
                mbc.MEMORY = mem;
                mbc.CopyROM0();
                mbc.CopyROMS();
                cpu.memory = mem;
                cpu.GetReady();
                showValues = ShowValues;
                ShowValues();
            }
        }

        public delegate void VoidInvoker();
        public VoidInvoker showValues;

        public void ShowValues()
        {
            textBox1.Text = Convert.ToString(cpu.AF, 16).PadLeft(4, '0').ToUpper();
            textBox2.Text = Convert.ToString(cpu.BC, 16).PadLeft(4, '0').ToUpper();
            textBox3.Text = Convert.ToString(cpu.DE, 16).PadLeft(4, '0').ToUpper();
            textBox4.Text = Convert.ToString(cpu.HL, 16).PadLeft(4, '0').ToUpper();
            textBox5.Text = Convert.ToString(cpu.SP, 16).PadLeft(4, '0').ToUpper();
            textBox6.Text = Convert.ToString(cpu.PC, 16).PadLeft(4, '0').ToUpper();
            textBox11.Text = Convert.ToString(cpu.memory.specialRegister.LCDC.Value, 16).PadLeft(4, '0').ToUpper();
            textBox12.Text = Convert.ToString(cpu.memory.specialRegister.STAT.Value, 16).PadLeft(4, '0').ToUpper();
            textBox13.Text = Convert.ToString(cpu.memory.specialRegister.LY.Value, 16).PadLeft(4, '0').ToUpper();
            textBox14.Text = (Convert.ToString(cpu.memory[cpu.SP + 1],16)+Convert.ToString(cpu.memory[cpu.SP], 16)).PadLeft(4, '0').ToUpper();
            textBox9_TextChanged(null,null);
            gbDebugger1.ShowData(cpu.memory, cpu.PC);
            //pictureBox1.Image = MakeBGLayer();
            pictureBox1.Image = new Bitmap(cpu.display.bmp);
            Bitmap bmp1 = new Bitmap(8 * 16, 8 * 16);
            using (Graphics g = Graphics.FromImage(bmp1))
                for (var y = 0; y < 16; y++)
                {
                    for (var x = 0; x < 16; x++)
                    {
                        int index = 0x8000;
                        index += 0x10 * x;
                        index += 0x100 * y;

                        Bitmap bmp2 = DrawTile(index);
                        g.DrawImage(bmp2, new Point(x * 8, y * 8));
                    }
                }
            pictureBox2.Image = bmp1;
        }

        public Bitmap MakeBGLayer()
        {
            Bitmap bmpr = new Bitmap(160, 144);
            Bitmap bmp = new Bitmap(160,144);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var y = 0; y < 32; y++)
                    {
                        int address;
                        if (cpu.memory.specialRegister.LCDC.BGTileMapDisplaySelect) address = 0x9C00;
                        else address = 0x9800;
                        int tileIndex = cpu.memory[address + x + y * 32];
                        int yIndex = tileIndex / 16;
                        int xIndex = tileIndex % 16;
                        int index;
                        if (cpu.memory.specialRegister.LCDC.BGWindowTIleMapDataSelect) index = 0x8000;
                        else index = 0x8800;
                        index += 0x10 * xIndex;
                        index += 0x100 * yIndex;

                        Bitmap bmp2 = DrawTile(index);
                        int drawX = x * 8 + cpu.memory.specialRegister.SCX.Value;
                        int drawY = y * 8 + cpu.memory.specialRegister.SCY.Value;
                        g.DrawImage(bmp2, new Point(drawX, drawY));
                        if (drawX < 0) drawX += 256;
                        if (drawY < 0) drawY += 256;
                        g.DrawImage(bmp2, new Point(drawX, drawY));
                    }
                }
            }
            
            return bmp;
        }

        public Bitmap DrawTile(int TileAddress)
        {
            Bitmap bmp = new Bitmap(8, 8);
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    int color = GetPixelTileColor(TileAddress, x, y);
                    switch(color)
                    {
                        case 0: bmp.SetPixel(x, y, Color.White);break;
                        case 1: bmp.SetPixel(x, y, Color.Black); break;
                        case 2: bmp.SetPixel(x, y, Color.LightGray); break;
                        case 3: bmp.SetPixel(x, y, Color.Gray); break;
                    }
                }
            }
            return bmp;
        }

        public int GetPixelTileColor(int TileAddress, int x, int y)
        {
            byte line = cpu.memory[TileAddress + y * 2];
            int bit = (int)Math.Pow(2, (7-x));
            int result = 0;
            if ((line & bit) > 0) result += 1;
            line = cpu.memory[TileAddress + y * 2 + 1];
            if ((line & bit) > 0) result += 2;
            return result;
        }

        private void button2_Click(object sender, EventArgs e) //STEP
        {
            cpu.Step();
            ShowValues();
        }

        private void button3_Click(object sender, EventArgs e) //RUN
        {
            Thread thr = new Thread(cpu.Run);
            thr.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ShowValues();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            cpu.AF = Convert.ToUInt16(textBox1.Text,16);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            cpu.BC = Convert.ToUInt16(textBox2.Text, 16);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            cpu.DE = Convert.ToUInt16(textBox3.Text, 16);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            cpu.HL = Convert.ToUInt16(textBox4.Text, 16);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            cpu.SP = Convert.ToUInt16(textBox5.Text, 16);
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            cpu.PC = Convert.ToUInt16(textBox6.Text, 16);
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox7.Text)) cpu.BreakPoint = -1;
            else cpu.BreakPoint = Convert.ToUInt16(textBox7.Text, 16);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            cpu.Running = false;
        }

        bool ignore = false;

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            ignore = true;
            try
            {
                textBox8.Text = Convert.ToString(cpu.memory.ReadLittleEndian(Convert.ToUInt16(textBox9.Text, 16)), 16).PadLeft(4,'0').ToUpper();
                textBox10.Text = Convert.ToString(cpu.memory[Convert.ToUInt16(textBox9.Text, 16)], 16).PadLeft(2, '0').ToUpper();
            }
            catch
            {

            }
            ignore = false;
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (!ignore)
            {
                int address = Convert.ToUInt16(textBox9.Text, 16);
                ushort value = Convert.ToUInt16(textBox8.Text, 16);
                cpu.memory.WriteLittleEndian(address,value);
            }
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            if (!ignore)
            {
                int address = Convert.ToUInt16(textBox9.Text, 16);
                byte value = Convert.ToByte(textBox8.Text, 16);
                cpu.memory[address] = value;
            }
        }

        private void gbDebugger1_Load(object sender, EventArgs e)
        {

        }

        private void textBox15_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            textBox15.Text = "";

            if (e.KeyCode == Keys.Up) cpu.memory.specialRegister.JOYP.Up = true;
            if (e.KeyCode == Keys.Down) cpu.memory.specialRegister.JOYP.Down = true;
            if (e.KeyCode == Keys.Left) cpu.memory.specialRegister.JOYP.Left = true;
            if (e.KeyCode == Keys.Right) cpu.memory.specialRegister.JOYP.Right = true;
            if (e.KeyCode == Keys.A) cpu.memory.specialRegister.JOYP.A = true;
            if (e.KeyCode == Keys.B) cpu.memory.specialRegister.JOYP.B = true;
            if (e.KeyCode == Keys.Space) cpu.memory.specialRegister.JOYP.Select = true;
            if (e.KeyCode == Keys.Enter) cpu.memory.specialRegister.JOYP.Start = true;
        }

        private void textBox15_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            textBox15.Text = "";

            int a = cpu.memory.specialRegister.JOYP.Value;
            if (e.KeyCode == Keys.Up) cpu.memory.specialRegister.JOYP.Up = false;
            if (e.KeyCode == Keys.Down) cpu.memory.specialRegister.JOYP.Down = false;
            if (e.KeyCode == Keys.Left) cpu.memory.specialRegister.JOYP.Left = false;
            if (e.KeyCode == Keys.Right) cpu.memory.specialRegister.JOYP.Right = false;
            if (e.KeyCode == Keys.A) cpu.memory.specialRegister.JOYP.A = false;
            if (e.KeyCode == Keys.B) cpu.memory.specialRegister.JOYP.B = false;
            if (e.KeyCode == Keys.Space) cpu.memory.specialRegister.JOYP.Select = false;
            if (e.KeyCode == Keys.Enter) cpu.memory.specialRegister.JOYP.Start = false;
        }
    }
}
