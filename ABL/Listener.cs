using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace ABL
{
    public class Listener
    {
        public string[] dir;
        public string uploadDir, phpScript;
        public lastEditedFile lastEdited;
        public Sftp sftp;
        public string[] acceptFiles;
        public NotifyIcon notifyIcon;
        public Form1 form1;
        public string DataReportDb;
        public TcpListener serwer;
        public List<TcpClient> serwer_client;
        public bool serverStarted = false;
        public string SheetImageDir;

        public bool syncBlocked = false;

        public DBController db;

        public Listener
        (
            string[] dir, 
            string uploadDir, 
            string[] aF, 
            ref Sftp sftp, 
            string DataReportDb, 
            string SheetImageDir
        )
        {
            this.db = new DBController(this);

            this.serwer_client = new List<TcpClient>();

            try
            {
                this.serwer = new TcpListener(IPAddress.Any, Form1.serwerPort);
                this.serwer.Start();
                serverStarted = true;
            } catch (Exception ex) {
                this.AddToLog("Port zajety! " + ex.Message);
                Application.Exit();
            }
            this.dir = dir;
            this.uploadDir = uploadDir;
            this.acceptFiles = aF;
            this.sftp = sftp;
            this.DataReportDb = DataReportDb;
            this.SheetImageDir = SheetImageDir;

            this.lastEdited = new lastEditedFile(null, null, DateTime.Now);
        }

        public List<FileInfo> getLastEdited()
        {
            DateTime toCompare;
            List<FileInfo> newFiles = new List<FileInfo>();
            toCompare = this.lastEdited.date;

            foreach(string _dir in this.dir)
            {
                DirectoryInfo dir = new DirectoryInfo(_dir);
                foreach (var fi in dir.GetFiles())
                {
                    if (toCompare.CompareTo(fi.LastWriteTime) < 0)
                    {
                        newFiles.Add(fi);
                    }
                }
            }
            return newFiles;
        }

        public void ShowTrayMessage(string title, string text)
        {
            if (text == "" || text == null)
            {
                text = "Brak odpowiedzi js";
            }
            this.notifyIcon.BalloonTipTitle = title;
            this.notifyIcon.BalloonTipText = text;
            this.notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            this.notifyIcon.Visible = true;
            this.notifyIcon.ShowBalloonTip(3000);
        }

        public void AddToLog(string text)
        {
            if (this.form1.listBox1.InvokeRequired)
            {
                this.form1.Invoke(new Action<string>(AddToLog), text);
            }
            else
            {
                this.form1.listBox1.Items.Add(text);
                using (StreamWriter ofile = new StreamWriter(this.form1.logSrc, true))
                {
                    DateTime dtwrite = DateTime.Now;
                    ofile.WriteLine(dtwrite.ToString("g") + " : " + text);
                    ofile.Close();
                }
            }
        }

        public void FtpStatusChange(bool status)
        {
            if (this.form1.statusStrip1.InvokeRequired)
            {
                this.form1.Invoke(new Action<bool>(FtpStatusChange), status);
            }
            else
            {
                if (status == false)
                {
                    this.form1.toolStripStatusLabel1.Text = "FTP: Rozłączony";
                }
                else
                {
                    this.form1.toolStripStatusLabel1.Text = "FTP: Połączony";
                }
            }
        }

        public void SyncStatusChange(double percent, int items, int allItems, string label = "Synchronizacja")
        {
            if (this.form1.statusStrip1.InvokeRequired)
            {
                this.form1.Invoke(new Action<double, int, int, string>(SyncStatusChange), percent, items, allItems, label);
            } 
            else
            {
                this.form1.toolStripStatusLabel2.Text = label + ": "+percent+"% (" + items + "/" + allItems + ")";
            }
        }

        public void SaveFile(FileInfo file)
        {
            if (this.lastEdited.date.CompareTo(file.LastWriteTime) < 0)
            {
                this.lastEdited = new lastEditedFile(file.FullName, file.Name, file.LastWriteTime);
            }
        }

        //Tcp server loop
        public static void ServerClient(object oListener)
        {
            Listener listener = (Listener)oListener;

            while(true)
            {
                TcpClient client = listener.serwer.AcceptTcpClient();
                listener.AddToLog("Nowe polaczenie!");
                listener.serwer_client.Add(client);
            }
        }

        public static void ServerData(object oListener)
        {
            Listener listener = (Listener)oListener;
            while(true)
            {
                for(int i = 0; i < listener.serwer_client.Count; i++)
                {
                    Byte[] bytes = new Byte[1024];
                    TcpClient client = listener.serwer_client[i];
                    if (client.Connected) {
                    NetworkStream stream = client.GetStream();
                        if (stream.CanRead && stream.DataAvailable)
                        {
                            int d;
                            string data = null;
                            try
                            {
                                if ((d = stream.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                    data = Encoding.ASCII.GetString(bytes, 0, d);
                                    switch (data)
                                    {
                                        case "sync":
                                            listener.AddToLog("Synchronizacja mysql -> mdb: Rozpoczeta!");
                                            listener.syncBlocked = true;
                                        break;

                                        case "sync-end":
                                            listener.AddToLog("Synchronizacja mysql -> mdb: Zakonczona!");
                                            listener.syncBlocked = false;
                                        break;
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                listener.serwer_client.Remove(client);
                                listener.AddToLog("Rozlaczono!");
                            }
                        }
                    }
                    else
                    {
                        listener.AddToLog("Rozlaczono!");
                        listener.serwer_client.Remove(client);

                    }
                }
                Thread.Sleep(10);
            }
        }

        private void checkMdbSyncErro(List<int> responses, string[] ids, Listener listener, string url = "plate_warehouse_sync_error")
        {
            List<string> toRemove = new List<string>();
            for (int i = 0; i < responses.Count; i++)
            {
                toRemove.Add(ids[responses[i]]);
            }

            string data = JsonConvert.SerializeObject(toRemove);

            WebClient web = new WebClient();
            web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string response = web.UploadString(Form1.phpScript + "?p_a=plate_warehouse_sync_error", "orders=" + data);
        }

        private void doMdbToMysqlMaterialSync(string dataToUpload, Listener listener)
        {
            try
            {
                WebClient web = new WebClient();
                web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string webResponse = web.UploadString(Form1.phpScript + "?p_a=material_sync_new", "toSync=[" + dataToUpload + "]");

                byte[] bytes = Encoding.Default.GetBytes(webResponse);
                webResponse = Encoding.UTF8.GetString(bytes);
                MaterialJsonResponse data = JsonConvert.DeserializeObject<MaterialJsonResponse>(webResponse);
                if (data.insert.Length > 0)
                {
                    this.db.executeQueryAicamBases(data.insert);
                    string UpdateMaterialName = "";
                    foreach (string MaterialName in data.insert_id)
                    {
                        if (UpdateMaterialName.Length > 0)
                        {
                            UpdateMaterialName += ",";
                        }
                        UpdateMaterialName += "'" + MaterialName + "'";
                    }
                    this.db.executeQueryAicamBases("UPDATE tmaterialsynced SET synced = 1 WHERE MaterialName in (" + UpdateMaterialName + ")");
                }

                if (data.update.Length > 0)
                {
                    string query_update1 = data.update.Replace(":TableName", "tmaterialsynced");
                    string query_update2 = data.update.Replace(":TableName", "T_material");

                    checkMdbSyncErro(this.db.executeQueryAicamBases(query_update1), data.update_id, listener, "material_sync_error");
                    this.db.executeQueryAicamBases(query_update2);

                    string UpdateMaterialName = "";
                    foreach (string MaterialName in data.update_id)
                    {
                        if (UpdateMaterialName.Length > 0)
                        {
                            UpdateMaterialName += ",";
                        }
                        UpdateMaterialName += "'" + MaterialName + "'";
                    }
                    this.db.executeQueryAicamBases("UPDATE tmaterialsynced SET synced = 1 WHERE MaterialName in (" + UpdateMaterialName + ")");
                }

                if (data.delete.Length > 0)
                {
                    this.db.executeQueryAicamBases(data.delete.Replace(":TableName", "tmaterialsynced"));
                    this.db.executeQueryAicamBases(data.delete.Replace(":TableName", "T_material"));
                }
            } catch (Exception ex)
            {
                listener.AddToLog("Mat Sync Error: " + ex.Message);
            }
        }

        private void doMdbToMysqlSync(string dataToUpload, Listener listener)
        {
            string webResponse = "";
            try
            {
                WebClient web = new WebClient();
                web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                webResponse = web.UploadString(Form1.phpScript + "?p_a=plate_warehouse_sync_new", "toSync=[" + dataToUpload + "]");

                byte[] bytes = Encoding.Default.GetBytes(webResponse);
                webResponse = Encoding.UTF8.GetString(bytes);
                PlateWarehouseJsonResponse data = JsonConvert.DeserializeObject<PlateWarehouseJsonResponse>(webResponse);
                if (data.insert.Length > 0)
                {
                    this.db.executeQueryAicamBases(data.insert);
                    string UpdateSheetCode = "";
                    foreach (string SheetCode in data.insert_id)
                    {
                        if (UpdateSheetCode.Length > 0)
                        {
                            UpdateSheetCode += ",";
                        }
                        UpdateSheetCode += "'" + SheetCode + "'";
                    }
                    this.db.executeQueryAicamBases("UPDATE platewarehousesynced SET synced = 1 WHERE SheetCode in (" + UpdateSheetCode + ")");
                }

                if (data.update.Length > 0)
                {
                    string query_update1 = data.update.Replace(":TableName", "platewarehousesynced");
                    string query_update2 = data.update.Replace(":TableName", "T_MaterialSheet");

                    checkMdbSyncErro(this.db.executeQueryAicamBases(query_update1), data.update_id, listener);
                    this.db.executeQueryAicamBases(query_update2);

                    string UpdateSheetCode = "";
                    foreach (string SheetCode in data.update_id)
                    {
                        if (UpdateSheetCode.Length > 0)
                        {
                            UpdateSheetCode += ",";
                        }
                        UpdateSheetCode += "'" + SheetCode + "'";
                    }
                    this.db.executeQueryAicamBases("UPDATE platewarehousesynced SET synced = 1 WHERE SheetCode in (" + UpdateSheetCode + ")");
                }

                if (data.delete.Length > 0)
                {
                    this.db.executeQueryAicamBases(data.delete.Replace(":TableName", "platewarehousesynced"));
                    this.db.executeQueryAicamBases(data.delete.Replace(":TableName", "T_MaterialSheet"));
                }
            }
            catch(Exception ex)
            {
                listener.AddToLog("Data " + webResponse);
                listener.AddToLog("Sync Error: " + ex.Message);
            }
            
        }

        //Thread loop
        public static void Update(object oListener)
        {
            Listener listener = (Listener)oListener;

            listener.AddToLog("Uruchomiono!");
            
            listener.sftp.Connect();
            
            while (true)
            {
                if (!listener.serverStarted)
                {
                    continue;
                }

                if (!listener.syncBlocked)
                {
                    //Sync material
                    List<T_material> materialToSync = listener.db.GetNotSynchronizedMaterial(listener);
                    int materialCount = materialToSync.Count;
                    if (materialCount > 0)
                    {
                        listener.AddToLog("Synchronizacja materialu mdb -> mysql: Rozpoczeta!");
                        listener.AddToLog("Znaleziono " + materialCount + " zmienionych materialow");

                        listener.SyncStatusChange(0, 0, materialCount, "Synchronizacja materialu");

                        string dataToUpload = "";

                        for(int i = 0; i < materialCount; i++)
                        {
                            string materialData = JsonConvert.SerializeObject(materialToSync[i]);
                            if (dataToUpload.Length > 0)
                            {
                                dataToUpload += ", ";
                            }
                            dataToUpload += materialData;

                            if (dataToUpload.Length > 5000)
                            {
                                listener.doMdbToMysqlMaterialSync(dataToUpload, listener);
                                double percent = Math.Floor((double)(i * 100.0 / materialCount));
                                listener.SyncStatusChange(percent, i, materialCount, "Synchronizacja materialu");
                                dataToUpload = "";
                            }
                        }

                        if (dataToUpload.Length > 0)
                        {
                            listener.doMdbToMysqlMaterialSync(dataToUpload, listener);
                            listener.SyncStatusChange(100, materialCount, materialCount, "Synchronizacja materialu");
                            dataToUpload = "";
                        }
                    }
                    materialToSync.Clear();
                   

                    //Sync main plate
                    List<T_MaterialSheet> platesToSync = listener.db.GetNotSynchronized(listener);
                    int platesCount = platesToSync.Count;
                    if (platesCount > 0)
                    {
                        listener.AddToLog("Synchronizacja mdb -> mysql: Rozpoczeta!");
                        listener.AddToLog("Znaleziono " + platesCount + " zmienionych blach");

                        listener.SyncStatusChange(0, 0, platesCount);

                        string dataToUpload = "";

                        for (int i = 0; i < platesCount; i++)
                        {
                            string plateData = JsonConvert.SerializeObject(platesToSync[i]);
                            if (dataToUpload.Length > 0)
                            {
                                dataToUpload += ", ";
                            }
                            dataToUpload += plateData;

                            if (dataToUpload.Length > 5000)
                            {
                                listener.doMdbToMysqlSync(dataToUpload, listener);
                                double percent = Math.Floor((double)(i * 100.0 / platesCount));
                                listener.SyncStatusChange(percent, i, platesCount);
                                dataToUpload = "";
                            }
                        }

                        if (dataToUpload.Length > 0)
                        {
                            listener.doMdbToMysqlSync(dataToUpload, listener);
                            listener.SyncStatusChange(100, platesCount, platesCount);
                            dataToUpload = "";
                        }
                    }
                    platesToSync.Clear();

                    //Sync memo
                    listener.db.openAicamBases();
                    List<T_MaterialSheet> memoSync = listener.db.getNotSyncedSkeletonData("T_MaterialSheet", "plateWarehouseSynced");
                    listener.db.closeAicamBases();
                    int memoCount = memoSync.Count;
                    if (memoCount > 0)
                    {
                        listener.AddToLog("Synchronizacja memo: " + memoCount);
                        listener.SyncStatusChange(0, 0, memoCount, "Synchronizacja MEMO");
                        for (int i = 0; i < memoCount; i++)
                        {
                            try
                            {
                                listener.db.insertMemo("plateWarehouseSynced", "SkeletonData", memoSync[i].SheetCode, memoSync[i].SkeletonData);
                            } catch (Exception ex)
                            {
                                listener.AddToLog("Blad synchronizacji memo: " + memoSync[i].SheetCode + " " + ex.Message);
                            }

                            double percent = Math.Floor((double)(i * 100.0 / memoCount));
                            listener.SyncStatusChange(percent, i, memoCount, "Synchronizacja MEMO");
                        }
                        listener.SyncStatusChange(100, memoCount, memoCount, "Synchronizacja MEMO");
                    }
                    memoSync.Clear();
                }

                List<FileInfo> lastFile = listener.getLastEdited();

                if (lastFile.Count > 0)
                {
                    foreach (FileInfo file in lastFile)
                    {
                        bool doscript = false;
                        for (int i = 0; i < listener.acceptFiles.Length; i++)
                        {
                            if (listener.acceptFiles[i] == file.Name)
                            {
                                doscript = true;
                                break;
                            }
                        }
                        if (doscript == true)
                        {
                            if (file.IsReadOnly)
                            {
                                continue;
                            }

                            bool plateSave = false;
                            //Blachy mode1
                            if (file.Name == "NestingManageReport.xml")
                            {
                                string plate_dir = listener.dir[1];
                                ABL.Costing.Plate.Mode1 model1 = new ABL.Costing.Plate.Mode1(listener, plate_dir);
                                model1.Process();
                                plateSave = true;
                            }

                            //Blachy multipart
                            if (file.Name == "ScheduleSheet.xml")
                            {
                                string plate_dir = listener.dir[1];
                                ABL.Costing.Plate.Mode2 model2 = new ABL.Costing.Plate.Mode2(listener, plate_dir);
                                model2.Process();

								ABL.Costing.Plate.Mode3 model3 = new Costing.Plate.Mode3(listener, plate_dir);
                                model3.Process();

                                //Ostatni etap produckcja
                                ABL.Costing.Plate.Production plateProduction = new Costing.Plate.Production(listener, plate_dir);
                                plateProduction.Process();
                                plateSave = true;
                            }

                            if (plateSave == true)
                            {
                                //Set time
                                listener.SaveFile(file);
                                continue;
                            }

                            //Check
                            XmlDocument xml = new XmlDocument();
                            try
                            {
                                xml.Load(file.FullName);
                            }
                            catch (InvalidOperationException e)
                            {
                                MessageBox.Show(e.Message);
                                continue;
                            }

                            XmlNodeList xmlNameCollection = xml.GetElementsByTagName("Name");

                            string pName = "";
                            string fName = "";
                            string ss = "";

                            if (xmlNameCollection.Count > 0)
                            {
                                pName = xmlNameCollection.Item(0).InnerText;
                                fName = pName.Substring(0, 2);
                                ss = pName.Substring(0, 1);
                            }
                            else
                            {
                                
                            }
                            
                            int nu;
                            string value;
                            string cName;
                            string phpAdd = "";
                            bool save = true;

                            if (fName == "W3" || ss == "T")
                            {
                                if (fName == "W3")
                                {
                                    if (pName.Length > 2)
                                    {
                                        cName = pName.Substring(2, 2);
                                    }
                                    else
                                    {
                                        cName = null;
                                    }
                                    if (cName != "+C")
                                    {
                                        Thread.Sleep(2000);
                                        Form2 form2 = new Form2();
                                        var res = form2.ShowDialog();
                                        if (res == DialogResult.OK)
                                        {
                                            value = form2.GetValues();
                                            phpAdd = "?&p_a=tube_single&c_mb=" + value;
                                        }
                                        else
                                        {
                                            save = false;
                                            doscript = false;
                                        }
                                    }
                                }
                                else if (ss == "T")
                                {
                                    if (Int32.TryParse(pName.Substring(1, 1), out nu) == false)
                                    {
                                        save = false;
                                    }
                                    phpAdd = "?p_a=1";
                                }

                                if (save == true)
                                {
                                    bool w3_add = false;
                                    //Upload
                                    if (fName == "W3")
                                    {
                                        listener.AddToLog("Wysyłam autowycene!");
                                        w3_add = true;
                                    }
                                    else if (ss == "T")
                                    {
                                        listener.AddToLog("Wysyłam program!");
                                    }
                                    listener.sftp.Upload(file.FullName);
                                    listener.sftp.UploadParts(xml, listener.phpScript, listener, w3_add);

                                    //Run php script
                                    WebClient serwer = new WebClient();

                                    string reply = serwer.DownloadString(listener.phpScript + phpAdd);
                                    byte[] bytes = Encoding.Default.GetBytes(reply);
                                    reply = Encoding.UTF8.GetString(bytes);

                                    listener.ShowTrayMessage("Skaner", reply);
                                    listener.AddToLog("Odpowiedź serwera: " + reply);
                                }
                            }
                        }
                        listener.SaveFile(file);
                    }
                }
                Thread.Sleep(2000);
            }
        }
    }
}