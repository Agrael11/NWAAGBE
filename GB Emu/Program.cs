using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GB_Emu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] data1 = System.IO.File.ReadAllLines("data.txt");
            string[] data2 = System.IO.File.ReadAllLines("data2.txt");
            

            string output = "";
            for (int i = 0; i < 256; i++)
            {
                if (data1[i] == "null")
                {
                    output += "new Instruction(null,\"null\",0),";
                }
                else
                {
                    int length = 0;
                    if (data1[i].Contains("%1")) length = 1;
                    if (data1[i].Contains("%2")) length = 2;
                    output += "new Instruction(opcode" + Convert.ToString(i, 16).ToUpper().PadLeft(2, '0') + ",\"" + data1[i].ToLower().Replace(",", ", ") + "\"," + length + "),";
                }
                if (i % 16 == 15) output += "\r\n";
            }
            System.IO.File.WriteAllText("shit.txt", output);
            output = "";
            for (int i = 0; i < 256; i++)
            {
                if (data2[i] == "null")
                {
                    output += "new Instruction(null,\"null\",0),";
                }
                else
                {
                    int length = 0;
                    if (data2[i].Contains("%1")) length = 1;
                    if (data2[i].Contains("%2")) length = 2;
                    output += "new Instruction(opcodeCB" + Convert.ToString(i, 16).ToUpper().PadLeft(2, '0') + ",\"" + data2[i].ToLower().Replace(",", ", ") + "\"," + length + "),";
                }
                if (i % 16 == 15) output += "\r\n";
            }
            System.IO.File.WriteAllText("shit2.txt", output);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
