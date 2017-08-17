using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ABL
{
    public partial class Form2 : Form
    {
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Form2()
        {
            InitializeComponent();
            this.CenterToScreen();
        }

        public string GetValues()
        {
            return this.textBox1.Text;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (char.IsNumber(e.KeyChar) != true && e.KeyChar != '\b' && e.KeyChar != '.') {
                e.Handled = true;
            }
        }

        /*private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsNumber(e.KeyChar) != true && e.KeyChar != '\b')
            {
                e.Handled = true;
            }
        }*/

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("Uzupełnij wszystkie dane!");
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Hide();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }
    }
}
