using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ABL
{
    public partial class Form1 : Form
    {
        private List<string> args = new List<string>();
        public string logSrc = "C:\\abl program\\log.txt";

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        public static int serwerPort = 8687;
        public static string plateXmlPartsPath = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\Temp\\";
        public static string phpScript = "http://192.168.100.161/autoc.php";
        //public static string phpScript = "http://serwer1741859.home.pl/autoc.php";

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Form1()
        {
            /* ------------------------------------------
             * SETTINGS
             */
             
            string[] scanDir = new string[2] {
                "C:\\Program Files (x86)\\Amada\\AICAM_Tube\\3D-Laser\\Settings",
                "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\Schedule_Reports"
            };
            string laserDir = "C:\\Program Files (x86)\\Amada\\AICAM_Tube\\3D-Laser\\";
            string uploadDir = "/var/www/html/ABL/temp/";
            string imgDir = "/var/www/html/ABL/detale/img/min/";
            string DataReportDB = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\DataReport.mdb";
            string SheetImageDir = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\Schedule_Reports\\";
            Sftp sftp = new Sftp("192.168.100.161", "laser", "btl321", uploadDir, imgDir, laserDir);
            
            /*string[] scanDir = new string[2] {
                "F:\\emulator",
                "F:\\emulator-blachy"
            };
            string laserDir = "C:\\Program Files (x86)\\Amada\\AICAM_Tube\\3D-Laser\\";
            string DataReportDB = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\DataReport.mdb";
            string uploadDir = "/";
            string imgDir = "/";
            Sftp sftp = new Sftp("127.0.0.1", "tester", "password", uploadDir, imgDir, laserDir);*/
            

            if (!Directory.Exists("C:\\abl program\\"))
            {
                Directory.CreateDirectory("C:\\abl program\\");
                File.Create(logSrc);
            }
            if (!File.Exists(logSrc))
            {
                File.Create(logSrc);
            }

            string[] accacceptFiles = new string[] {
                //PROFIL
                "PrintData.xml",
                "TubeReport.xml",
                //BLACHA
                "NestingManageReport.xml",
                //Multi blacha
                "ScheduleSheet.xml"
            };
            //-------------------------------------------

            Listener listener = new Listener(scanDir, uploadDir, accacceptFiles, ref sftp, DataReportDB, SheetImageDir);

            InitializeComponent();

            listener.phpScript = phpScript;
            listener.notifyIcon = this.notifyIcon1;
            listener.form1 = this;
            listener.sftp.parentListener = listener;

            Thread tListener = new Thread(Listener.Update);
            tListener.Start((object)listener);

            Thread tServer = new Thread(Listener.ServerClient);
            tServer.Start((object)listener);

            Thread dServer = new Thread(Listener.ServerData);
            dServer.Start((object)listener);
        }

        private void otwórzToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.WindowState = FormWindowState.Minimized;
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            MessageBox.Show("test");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {

        }
    }
}
