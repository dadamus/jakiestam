using System;
using System.IO;
using Renci.SshNet;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace ABL
{
    public class Sftp
    {
        private string udir;
        private string img_dir;
        private SftpClient sftpClient;
        public bool connectTest = false;
        public Listener parentListener;
        public string laserDir;

        public Sftp(string host, string user, string pass, string udir, string img_dir, string laserDir)
        {
            this.laserDir = laserDir;
            this.udir = udir;
            this.img_dir = img_dir;
            this.sftpClient = new SftpClient(host, user, pass);
            this.sftpClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(60);
        }

        public void Connect()
        {
            try
            {
                this.parentListener.AddToLog("Próba połączenia z FTP...");
                this.sftpClient.Connect();
            }
            catch (Exception e)
            {
                this.parentListener.FtpStatusChange(false);
                this.parentListener.AddToLog("Brak połączenia z FTP!");
                Debug.WriteLine(e);
                while (true)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        this.sftpClient.Connect();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (this.sftpClient.IsConnected == true)
                    {
                        this.parentListener.FtpStatusChange(true);
                        this.parentListener.AddToLog("Połączono z FTP!");
                        this.sftpClient.ChangeDirectory(udir);
                        break;
                    }
                }
                return;
            }
            connectTest = true;
            this.parentListener.FtpStatusChange(true);
            this.parentListener.AddToLog("Połączono z FTP!");
        }

        public void Upload(string sFile, string _name = null, string _dir = null)
        {
            if (_dir == null)
            {
                _dir = this.udir;
            }
            if (!File.Exists(sFile))
            {
                this.parentListener.AddToLog("Plik nie istnieje: " + sFile);
                return;
            }
            using (var file = File.OpenRead(sFile))
            {
                try
                {
                    if (this.sftpClient.IsConnected == false)
                    {
                        this.Connect();
                    }
                    this.sftpClient.ChangeDirectory(_dir);

                    if (_name == null)
                    {
                        _name = Path.GetFileName(file.Name);
                    }

                    this.parentListener.AddToLog("Wysyłam plik: " + _name);
                    this.sftpClient.UploadFile(file, _name, null);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Błąd sftp: " + e);
                    this.parentListener.AddToLog("Błąd wysyłania ftp!" + e);
                }
            }
        }

        class UImg
        {
            public string _name { get; set; }
            public string _img { get; set; }
        }

        public void UploadParts(XmlDocument xml, string phpScript, Listener lis, bool w3_add)
        {
            string squery = null;
            List<UImg> parts = new List<UImg>();
            XmlNodeList PartsList = xml.SelectNodes("//TubeReport/Part");
            foreach (XmlNode Part in PartsList)
            {
                string partName = Part.Attributes["Name"].Value;
                if (!parts.Exists(e => e._name == partName))
                {
                    string path = Part["BMPName"].InnerText;
                    lis.AddToLog("Obrazek: " + path);
                    if (squery == null)
                    {
                        squery = partName;
                    }
                    else
                    {
                        squery += "|" + partName;
                    }

                    if (w3_add == true) // Upload part xml
                    {
                        string name1 = "AssyData\\" + partName + "\\" + partName + ".xml";
                        string name2 = "AssyData\\" + partName + "\\" + partName + "-shd.xml";

                        this.Upload(Path.Combine(this.laserDir, name1.Replace(" ", "")));
                        this.Upload(Path.Combine(this.laserDir, name2.Replace(" ", "")));

                    }

                    parts.Add(new UImg() { _name = partName, _img = path });
                }
            }

            lis.AddToLog("IMG to php: " + squery);

            WebClient query = new WebClient();
            string reply = query.DownloadString(phpScript + "?p_a=2&parts=" + squery);
            byte[] bytes = Encoding.Default.GetBytes(reply);
            reply = Encoding.UTF8.GetString(bytes);

            lis.AddToLog("IMG from php: " + reply);

            string[] partsToAdd = reply.Split('|');
            string uploaded = "";
            foreach (string p in partsToAdd)
            {
                if (p == "" || p == " ")
                {
                    continue;
                }

                string[] part_d = p.Split('#');
                if (part_d.Length > 1)
                {

                    UImg element = parts.Find(e => e._name == part_d[0]);

                    lis.AddToLog("Dodaje zdjecie detalu: " + element._img);

                    this.Upload(element._img, part_d[1] + ".bmp", this.img_dir);
                    uploaded += part_d[1] + "|";
                }
                else
                {
                    lis.AddToLog("Błąd parsowania php: " + p);
                }
            }

            if (uploaded == "")
            {
                lis.AddToLog("Brak miniaturek do zapisu");
            }
            else
            {
                string reply2 = query.DownloadString(phpScript + "?p_a=3&d=" + uploaded);
                bytes = Encoding.Default.GetBytes(reply2);
                reply2 = Encoding.UTF8.GetString(bytes);

                lis.AddToLog("From php: " + reply2);
            }

        }

        public void Disconnect()
        {
            this.sftpClient.Disconnect();
        }
    }
}
