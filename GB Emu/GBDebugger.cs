using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GB_Emu
{
    public partial class GBDebugger : UserControl
    {
        public GBDebugger()
        {
            InitializeComponent();
        }

        public void ShowData(Memory MEM, ushort PC)
        {
            listView1.Items.Clear();
            int start = PC - 10;
            int end = PC;
            for (int i = 0; i < 10; i++)
            {
                if (CPU.GetInstruction(MEM, end).Length == 0)
                {
                    end++;
                    continue;
                }
                if (CPU.GetInstruction(MEM, end).Length == 1)
                {
                    end += 2;
                    continue;
                }
                if (CPU.GetInstruction(MEM, end).Length == 2)
                {
                    end += 3;
                    continue;
                }
            }
            start = (start >= 0) ? start : 0;
            end = (end <= 0xFFFF) ? end : 0xFFFF;
            for (int i = start; i < PC; i++)
            {
                AddToList(i, MEM[i], MEM);
            }
            AddToList(PC, MEM[PC], MEM, true);
            for (int i = PC + CPU.GetInstruction(MEM, PC).Length + 1; i <= end; i++)
            {
                AddToList(i, MEM[i], MEM);
                if (CPU.GetInstruction(MEM, i).Length == 1) i++;
                if (CPU.GetInstruction(MEM, i).Length == 2) i += 2;
            }
        }

        public void AddToList(int addr, byte value, Memory MEM, bool active = false)
        {
            ListViewItem item = new ListViewItem("0x" + Convert.ToString(addr,16).PadLeft(4,'0').ToUpper());
            CPU.Instruction ins = CPU.GetInstruction(MEM, addr);

            string val = "";
            val += Text = "" + Convert.ToString(value, 16).PadLeft(2, '0').ToUpper();
            for (int i = 0; i < ins.Length; i++)
            {
                val += Text = " " + Convert.ToString(MEM[addr+1+i], 16).PadLeft(2, '0').ToUpper();
            }
            item.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = val });


            string decomp = "";
            decomp += ins.Name;
            if (decomp.Contains("%2"))
            {
                decomp = decomp.Replace("%2",Convert.ToString(MEM[addr + 2], 16).ToUpper() + Convert.ToString(MEM[addr + 1], 16).ToUpper());
            }
            if (decomp.Contains("%1"))
            {
                decomp = decomp.Replace("%1", Convert.ToString(MEM[addr + 1], 16).ToUpper());
            }
            item.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = decomp });
            listView1.Items.Add(item);
            if (active)
            {
                item.BackColor = Color.Black;
                item.ForeColor = Color.White;
            }
        }
    }
}
