using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace ABL.Costing.Plate
{
    class Mode2 : ABL.Costing.Costing
    {
        private Listener listener;
        private string dir;
        private string assembly_dir;
        
        private List<Mode3Data.DetailData> details = new List<Mode3Data.DetailData>();

        public Mode2(Listener listener, string dir)
        {
            this.listener = listener;
            this.dir = dir;
            this.assembly_dir = Path.Combine(Directory.GetParent(dir).ToString(), "Assembly\\");
        }

        public bool Process()
        {
            Thread.Sleep(1000);
            if (!this.ScheduleSheet())
            {
                return false;
            }

            if (!this.getParts())
            {
                return false;
            }

            WebClient client = new WebClient();

            string data = JsonConvert.SerializeObject(this.details);

            this.listener.AddToLog("Wysylam multipart");
            WebClient web = new WebClient();
            web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string response = web.UploadString(Form1.phpScript + "?p_a=multipart_plate_costing_details", "details=" + data);
            byte[] bytes = Encoding.Default.GetBytes(response);
            response = Encoding.UTF8.GetString(bytes);
            this.listener.AddToLog("Odpowiedz od php: " + response);
            return true;
        }

        private bool getParts()
        {
            string assembly_directory_path = this.FindAssemblyFile().FullName;

            foreach(Mode3Data.DetailData detail in this.details)
            {
                string part_path = Path.Combine(assembly_directory_path, "Part_" + detail.sheet + ".xml");
                if (!File.Exists(part_path))
                {
                    this.listener.AddToLog("Brak pliku: " + part_path);
                    return false;
                }

                XmlDocument partSheet = new XmlDocument();
                partSheet.Load(part_path);
                XmlNodeList laserMatNameNode = partSheet.GetElementsByTagName("LaserMatName");
                detail.laser_mat_name = laserMatNameNode.Item(0).InnerText;
            }

            return true;
        }

        private bool ScheduleSheet()
        {
            try
            {
                XmlDocument scheduleSheetXML = new XmlDocument();
                scheduleSheetXML.Load(this.dir + "/ScheduleSheet.xml");
                XmlNodeList detailsNode = scheduleSheetXML.GetElementsByTagName("Sheets").Item(0).ChildNodes;
                
                for (int n = 0; n < detailsNode.Count; n++)
                {
                    XmlNode detail = detailsNode.Item(n);
                    XmlNodeList parameters = detail.ChildNodes;

                    Mode3Data.DetailData detailData = new Mode3Data.DetailData();
                    for (int a = 0; a < parameters.Count; a++)
                    {
                        XmlNode parameter = parameters.Item(a);
                        switch (parameter.Name)
                        {
                            case "SheetName":
                                int detail_id = ValidDetailName(parameter.InnerText);
                                if (detail_id == 0)
                                {
                                    return false;
                                }
                                detailData.detail_id = detail_id;
                                detailData.sheet = parameter.InnerText;
                                break;

                            case "PreTime":
                                detailData.pretime = parameter.InnerText;
                                break;
                        }

                        if (detailData.sheet != null) {
                            this.details.Add(detailData);
                        }
                    }
                }
            } catch (XmlException ex)
            {
                this.listener.AddToLog("BLAD XML: " + ex.Message);
                return false;
            }

            return true;
        }

        private int ValidDetailName(string name)
        {
            int detail_id = 0;
            if (name[0].ToString() + name[1].ToString() == "MP")
            {
                string[] arguments = name.Split('-');
                Int32.TryParse(arguments[4], out detail_id);
            }
            return detail_id;
        }

        private DirectoryInfo FindAssemblyFile()
        {
            DirectoryInfo lastEdited = new DirectoryInfo(this.assembly_dir);
            DateTime lastEditedDate = DateTime.MinValue;

            DirectoryInfo assembly = new DirectoryInfo(this.assembly_dir);
            foreach (DirectoryInfo d in assembly.GetDirectories())
            {
                foreach (FileInfo f in d.GetFiles())
                {
                    if (Path.GetFileName(f.DirectoryName) == "Demo")
                    {
                        continue;
                    }

                    DateTime fileDate = File.GetLastWriteTime(f.FullName);

                    if (DateTime.Compare(fileDate, lastEditedDate) > 0)
                    {
                        lastEditedDate = fileDate;
                        lastEdited = d;
                    }
                }
            }

            return lastEdited;
        }
    }
}
