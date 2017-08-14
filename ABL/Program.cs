using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System.Net;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Net.Sockets;

namespace ABL
{
    static class Program
    {
        static string mutexName = "ABL-0af3edfefd330b1980a3f377a2749502-PrOgRaM-Windows";
        static Form1 form1;
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Boolean appOn;

            /*if (System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
            remote dekstop
            }*/

            using (Mutex mutex = new Mutex(true, mutexName, out appOn))
            {
                string[] args = Environment.GetCommandLineArgs();
                if (appOn)
                {
                    //Main first app window
                    if (CheckRegister() == false) {
                        CreateRegister();
                    }
                    form1 = new Form1();
                    Application.Run(form1);
                }
                else
                {
                    if (args.Length > 1)
                    {
                        //Connect to main instance thread
                        TcpClient tcpClient = new TcpClient();
                        tcpClient.Connect(IPAddress.Parse("127.0.0.1"), Form1.serwerPort);

                        switch (args[1])
                        {
                            case "add":
                                string detailPath = WebUtility.HtmlEncode(args[2]);
                                string response = AddDetail(detailPath);
                                if (response.Length > 0) {
                                    SendTCPData(tcpClient, response);
                                }
                                break;
                            case "explorer":
                                string[] command= args[2].Split(':');
                                string directoryPath = command[1]+":"+command[2];
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
                                    FileName = directoryPath,
                                    UseShellExecute = true,
                                    Verb = "open"
                                });
                                break;
                            case "sync":
                                //Block sync in main thread
                                SendTCPData(tcpClient, "sync");

                                PlateWarehouse pw = new PlateWarehouse(new DBController());
                                pw.sync();

                                SendTCPData(tcpClient, "sync-end");
                                break;
                            default:
                                MessageBox.Show("Błędna komenda!");
                                break;
                        }
                    }
                    else
                    { 
                        MessageBox.Show("Aplikacja jest już uruchomiona!");
                    }
                    return;
                }
            }
        }

        public static void SendTCPData(TcpClient tcpClient, string message)
        {
            Stream serwerStream = tcpClient.GetStream();
            
            byte[] bytes = new byte[1024];
            bytes = Encoding.ASCII.GetBytes(message);
            for (int i = 0; i < bytes.Length; i++)
            {
                serwerStream.WriteByte(bytes[i]);
            }
        }

        public static string CalculateMD5Hash(string input)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static string AddDetail(object sPath) {
            string path = (string)sPath;
            string detailName = Path.GetFileName(path);

            DialogResult resoult = MessageBox.Show("Dodać detal " + detailName + "?", "Potwierdź wybór", MessageBoxButtons.YesNo);
            if (resoult == DialogResult.Yes)
            {
                if (Path.GetExtension(path) != ".shd" && Path.GetExtension(path) != ".SHD" && Path.GetExtension(path) != ".DXF" && Path.GetExtension(path) != ".dxf")
                {
                    MessageBox.Show("Detal ma zły format!");
                    return "Detal ma zły format!";
                }

                string drive = Path.GetPathRoot(path);
                if (drive != "Y:\\") {
                    MessageBox.Show("Zły folder detalu!");
                    return "Zły folder detalu!";
                }

                string webPath = WebUtility.HtmlEncode(path);
                WebClient client = new WebClient();
                try
                {
                    string resp = client.DownloadString("http://abl.pl/engine/addDetail.php?path=" + webPath);
                    if (resp != "1")
                    {
                        byte[] bytes = Encoding.Default.GetBytes(resp);
                        string respUTF = Encoding.UTF8.GetString(bytes);
                        MessageBox.Show(respUTF);
                        return respUTF;
                    }
                }
                catch (WebException e)
                {
                    MessageBox.Show("Wystąpił błąd: " + e);
                    return "Wystąpił błąd: " + e;
                }
            }
            return "";
        }

        public static bool CheckRegister()
        {
            object menu = Registry.ClassesRoot.OpenSubKey("*").OpenSubKey("shell").GetValue("Dodaj do projektu");
            if (menu == null) {
                return false;
            }
            object uri = Registry.ClassesRoot.GetValue("ABL");
            if (uri == null) {
                return false;
            }
            object asql= Registry.ClassesRoot.GetValue("ABL-SQL");
            if (asql == null)
            {
                return false;
            }
            object ablsync = Registry.ClassesRoot.GetValue("ABL-SYNC");
            if (ablsync == null)
            {
                return false;
            }

            return true;
            
        }

        public static void CreateRegister()
        {
            RegistryKey menu = Registry.ClassesRoot.OpenSubKey("*").OpenSubKey("shell", true).CreateSubKey("Dodaj do projektu");
            menu.SetValue("Icon", "C:\\Windows\\System32\\Shell32.dll,46");
            RegistryKey menuCommand = menu.CreateSubKey("Command");
            menuCommand.SetValue("", "C:\\abl program\\ABL.exe add \"%1\"");

            RegistryKey uri = Registry.ClassesRoot.CreateSubKey("ABL");
            uri.SetValue("CustomUrlApplication", "C:\\abl program\\ABL.exe");
            uri.SetValue("CustomUrlArguments", "explorer \"%1\"");
            uri.SetValue("URL Protocol", "");
            RegistryKey uriCommand = uri.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
            uriCommand.SetValue("", "C:\\abl program\\ABL.exe explorer \"%1\"");

            RegistryKey ablsync = Registry.ClassesRoot.CreateSubKey("ABL-SYNC");
            ablsync.SetValue("CustomUrlApplication", "C:\\abl program\\ABL.exe");
            ablsync.SetValue("CustomUrlArguments", "sync \"%1\"");
            ablsync.SetValue("URL Protocol", "");
            RegistryKey ablsyncCommand = ablsync.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
            ablsyncCommand.SetValue("", "C:\\abl program\\ABL.exe sync \"%1\"");

            RegistryKey asql = Registry.ClassesRoot.CreateSubKey("ABL-SQL");
            asql.SetValue("CustomUrlApplication", "C:\\abl program\\ABL.exe");
            asql.SetValue("CustomUrlArguments", "asql \"%1\"");
            asql.SetValue("URL Protocol", "");
            RegistryKey asqlCommand = asql.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
            asqlCommand.SetValue("", "F:\\visual\\sqlsql\\sqlsql\\bin\\Debug\\sqlsql.exe asql \"%1\"");
        }

        public static void NewInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            /*List<string> listArg = new List<string>();
            for (int i = 0; i < e.CommandLine.Count; i++)
            {
                listArg.Add(e.CommandLine[i]);
            }
            form1.getArguments(listArg.ToArray());*/
            //MessageBox.Show("Ilość argumentów: "+e.CommandLine.Count);
            e.BringToForeground = true;
        }

        public class SingleInstance : WindowsFormsApplicationBase
        {
            private SingleInstance()
            {
                base.IsSingleInstance = true;
            }

            public static void Run(Form f, StartupNextInstanceEventHandler startupHandler)
            {
                SingleInstance app = new SingleInstance();
                app.MainForm = f;
                app.StartupNextInstance += startupHandler;
                app.Run(Environment.GetCommandLineArgs());
            }
        }
    }
}
