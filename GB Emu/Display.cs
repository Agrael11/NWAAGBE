using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_Emu
{
    public class Display
    {
        public Bitmap bmp;
        byte[] bmpData;

        public Display()
        {
            bmp = new Bitmap(160, 144);
            for (int x = 0; x < 160; x++)
            {
                for (int y = 0; y < 144; y++)
                {
                    bmp.SetPixel(x, y, Color.White);
                }
            }
            bmpData = GetBitmapData(bmp);
        }




        public int mode = 0;
        public int modeClock = 0;
        public int line = 0;

        public void Step(int time)
        {
            modeClock += time;
            switch (mode)
            {
                case 2:
                    if (modeClock >= 80)
                    {
                        modeClock = 0;
                        mode = 3;
                    }
                    break;
                case 3:
                    if (modeClock >= 172)
                    {
                        modeClock = 0;
                        mode = 0;
                        RenderLine();
                    }
                    break;
                case 0:
                    if (modeClock >= 204)
                    {
                        modeClock = 0;
                        line++;
                        Form1.Instance.cpu.memory.specialRegister.LY.Value = (byte)line;
                        if (line == 143)
                        {
                            mode = 1;
                            bmp = SetBitmapData(bmpData, bmp.Width, bmp.Height);
                            Form1.Instance.pictureBox1.Image = new Bitmap(bmp);
                        }
                        else
                        {
                            mode = 2;
                        }

                    }
                    break;
                case 1:
                    if (modeClock >= 456)
                    {
                        modeClock = 0;
                        line++;
                        Form1.Instance.cpu.memory.specialRegister.LY.Value = (byte)line;
                        if (line == 153)
                        {
                            mode = 2;
                            line = 0;
                        }
                    }
                    break;
            }
        }

        public void RenderLine()
        {
            SpecialRegisters special = Form1.Instance.cpu.memory.specialRegister;
            int BaseAddress = special.LCDC.BGTileMapDisplaySelect ? 0x9C00 : 0x9800;
            int offY = line + special.SCY.Value;
            int offX = special.SCX.Value;
            for (int xPoint = 0; xPoint < 160; xPoint++)
            {
                int y = offY;
                int x = offX + xPoint;
                int tileIndex = Form1.Instance.cpu.memory[BaseAddress + x/8 + (y/8) * 32];
                int xIndex = tileIndex % 16;
                int yIndex = tileIndex / 16;
                int index = (special.LCDC.BGWindowTIleMapDataSelect) ? 0x8000 : 0x8600;
                index += 0x10 * xIndex;
                index += 0x100 * yIndex;
                Color c = getTilePixel(index, x % 8, y % 8);
                bmpData[(line * 160 + xPoint) * 4] = c.R;
                bmpData[(line * 160 + xPoint) * 4+1] = c.G;
                bmpData[(line * 160 + xPoint) * 4+2] = c.B;
            }

        }

        public byte[] GetBitmapData(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr ptr = data.Scan0;
            byte[] bytes = new byte[data.Stride * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, data.Stride * bmp.Height);
            bmp.UnlockBits(data);
            return bytes;
        }

        public Bitmap SetBitmapData(byte[] array, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            Rectangle rect = new Rectangle(0, 0, width, height);
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr ptr = data.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(array, 0, ptr, array.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        public Color getTilePixel(int index, int x, int y)
        {
            byte line = Form1.Instance.cpu.memory[index + y * 2];
            int bit = (int)Math.Pow(2, (7 - x));
            int result = 0;
            if ((line & bit) > 0) result += 1;
            line = Form1.Instance.cpu.memory[index + y * 2 + 1];
            if ((line & bit) > 0) result += 2;
            int color = result;
            SpecialRegisters.PalleteGB.Colors realc;
            switch (color)
            {
                case 0: realc = Form1.Instance.cpu.memory.specialRegister.BGP.Col0; break;
                case 1: realc = Form1.Instance.cpu.memory.specialRegister.BGP.Col1; break;
                case 2: realc = Form1.Instance.cpu.memory.specialRegister.BGP.Col2; break;
                case 3: realc = Form1.Instance.cpu.memory.specialRegister.BGP.Col3; break;
                default: realc = SpecialRegisters.PalleteGB.Colors.Error; break;
            }
            switch (realc)
            {
                case SpecialRegisters.PalleteGB.Colors.White: return Color.White;
                case SpecialRegisters.PalleteGB.Colors.Black: return Color.Black;
                case SpecialRegisters.PalleteGB.Colors.LightGray: return Color.LightGray;
                case SpecialRegisters.PalleteGB.Colors.DarkGray: return Color.Gray;
                default: return Color.Green;
            }
        }
    }
}
