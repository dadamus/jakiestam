using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using Newtonsoft.Json;

namespace ABL.Costing.Plate
{
    class Mode1 : ABL.Costing.Costing
    {
        private string path;
        public Listener listener;
        private List<Parts> parts = new List<Parts>();

        private Dictionary<string, string> nestingManageReport;
        private Dictionary<string, string> partData = new Dictionary<string, string>();
        private Dictionary<string, string> constructInfo = new Dictionary<string, string>();
        private Dictionary<string, string> matUsedInfo = new Dictionary<string, string>();

        public Mode1(Listener listener, string path)
        {
            this.nestingManageReport = new Dictionary<string, string>();
            this.listener = listener;
            this.path = path;
        }

        public bool Process()
        {
            System.Threading.Thread.Sleep(1000);
            if (NestingManageReport())
            {
                Dictionary<string, string> ProcessTimeTable = this.listener.db.getProcessTimeTable(this.nestingManageReport["name"]);
                Dictionary<string, string> SheetTabl = this.listener.db.getSheetTable(this.nestingManageReport["name"]);
                this.partDataXml();
                this.ConstructInfo();
                this.MatUsedInfo();

                //Upload img
                this.listener.sftp.Upload(SheetTabl["db_Image"]);

                //Marge Dictionary
                ProcessTimeTable.ToList().ForEach(x => SheetTabl.Add(x.Key, x.Value));
                SheetTabl.ToList().ForEach(x => nestingManageReport.Add(x.Key, x.Value));
                nestingManageReport.ToList().ForEach(x => partData.Add(x.Key, x.Value));
                partData.ToList().ForEach(x => constructInfo.Add(x.Key, x.Value));
                constructInfo.ToList().ForEach(x => matUsedInfo.Add(x.Key, x.Value));

                Dictionary<string, string> outDictionary = new Dictionary<string, string>();
                matUsedInfo.ToList().ForEach(x => outDictionary.Add(x.Key, x.Value));

                StreamWriter outputFile = new StreamWriter("C:\\abl program\\plate.csv");
                foreach(KeyValuePair<string, string> entry in outDictionary)
                {
                    outputFile.WriteLine('"' + entry.Key + "\",\"" + entry.Value + '"');
                }
                outputFile.Close();

                this.listener.sftp.Upload("C:\\abl program\\plate.csv");
                WebClient serwer = new WebClient();

                string reply = serwer.DownloadString(listener.phpScript + "?p_a=add_plate_costing_single");
                byte[] bytes = Encoding.Default.GetBytes(reply);
                reply = Encoding.UTF8.GetString(bytes);
                this.listener.ShowTrayMessage("Wycena blach", reply);
                this.listener.AddToLog("Wycena blach php: " + reply);
                return true;
            } else
            {
                return false;
            }
        }

        //-------------------- F I L E S -----------------------------
        private bool NestingManageReport() // Main init file
        {
            XmlDocument nestingManageReportXml = new XmlDocument();
            try
            {
                nestingManageReportXml.Load(this.path + "/NestingManageReport.xml");
                XmlNodeList nameNode = nestingManageReportXml.GetElementsByTagName("NstStarToEnd");
                if (nameNode.Count > 0)
                {
                    string name = nameNode.Item(0).InnerText;
                    string sname = name.Substring(0, 1);

                    if (sname == "S") // Dobra nazwa brak bledow w odczycie
                    {
                        //Sprawdzamy czy to jest ten sam detal
                        XmlNodeList detailNames = nestingManageReportXml.GetElementsByTagName("PartCode");

                        string detailCode = detailNames.Item(0).InnerText; ;
                        for (int d = 1; d < detailNames.Count; d++)
                        {
                            if (detailCode == detailNames.Item(d).InnerText) continue;

                            this.listener.AddToLog("Na arkuszu znajduja się rowniez inne czesci!");
                            return false;
                        }

                        WebClient serwer = new WebClient();
                        string reply = serwer.DownloadString(listener.phpScript + "?p_a=check_costing_line&code=" + detailCode);
                        byte[] bytes = Encoding.Default.GetBytes(reply);
                        reply = Encoding.UTF8.GetString(bytes);

                        if (reply == "null")
                        {
                            this.listener.AddToLog("Nie wykryto detalu w kolejce!");
                            return false;
                        }

                        //Load parts
                        XmlNodeList partNode = nestingManageReportXml.GetElementsByTagName("Part");
                        if (partNode.Count == 0)
                        {
                            this.listener.AddToLog("Brak detali!");
                            return false;
                        }

                        for (int p = 0; p < partNode.Count; p++)
                        {
                            Dictionary<string, string> PartValues = new Dictionary<string, string>();
                            for(int pi =0; pi < partNode[0].ChildNodes.Count; pi++)
                            {
                                string value = partNode[0].ChildNodes[pi].InnerText.ToString();
                                switch(partNode[0].ChildNodes[pi].Name)
                                {
                                    case "PartCode":
                                        PartValues["PartCode"] = value;
                                        break;
                                    case "PlanQty":
                                        PartValues["PlanQty"] = value;
                                        break;
                                    case "CountOnSheet0":
                                        PartValues["CountOnSheet0"] = value;
                                        break;
                                }
                            }

                            Parts Part = new Parts();
                            Part.PartCode = PartValues["PartCode"];
                            Part.PlanQty = PartValues["PlanQty"];
                            Part.CountOnSheet = PartValues["CountOnSheet0"];
                            this.parts.Add(Part);
                        }

                        //this.detailData = JsonConvert.DeserializeObject(reply); //todo check
                        this.nestingManageReport["name"] = name;
                        this.nestingManageReport["detailCode"] = detailCode;
                    }
                    else
                    {
                        return false;
                    }
                    
                }
                else
                {
                    this.listener.AddToLog("Brak tagu name!");
                    return false;
                }
            }
            catch (XmlException ex)
            {
                this.listener.AddToLog("BLAD XML: " + ex.Message);
                return false;
            }

            return true;
        }

        private bool partDataXml()
        {
            if (this.parts.Count == 0) return false;

            string partCode = this.parts[0].PartCode;

            DateTime partDirectoryLastWrite = new DateTime(DateTime.MinValue.Ticks);
            DirectoryInfo partDirectory = new DirectoryInfo("\\NotFoud");

            DirectoryInfo partsDirectory = new DirectoryInfo(Form1.plateXmlPartsPath);
            foreach (var tempPartDirectory in partsDirectory.GetDirectories())
            {
                DateTime tempDirectoryLastWrite = tempPartDirectory.LastWriteTime;
                if (tempDirectoryLastWrite.CompareTo(partDirectoryLastWrite) > 0)
                {
                    partDirectoryLastWrite = tempDirectoryLastWrite;
                    partDirectory = tempPartDirectory;
                }
            }

            if (!partDirectory.Exists) return false;

            foreach (var partFile in partDirectory.GetFiles())
            {
                if (Path.GetFileNameWithoutExtension(partFile.Name) == partCode)
                {
                    XmlDocument partDocument = new XmlDocument();
                    partDocument.Load(partFile.FullName);

                    XmlNodeList headerNode = partDocument.GetElementsByTagName("Header");
                    Dictionary<string, string> HeaderValues = new Dictionary<string, string>();
                    for(int he = 0; he < headerNode[0].ChildNodes.Count; he++)
                    {
                        string value = headerNode[0].ChildNodes[he].InnerText.ToString();
                        switch(headerNode[0].ChildNodes[he].Name)
                        {
                            case "UnfoldDimensionSizeX":
                                this.partData["UnfoldDimensionSizeX"] = value;
                            break;

                            case "UnfoldDimensionSizeY":
                                this.partData["UnfoldDimensionSizeY"] = value;
                            break;

                            case "AreaWithoutHoles":
                                this.partData["AreaWithoutHoles"] = value;
                                break;

                            case "AreaWithHoles":
                                this.partData["AreaWithHoles"] = value;
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private bool ConstructInfo()
        {
            XmlDocument constructInfoXml = new XmlDocument();
            constructInfoXml.Load(this.path + "/ConstructInfo.xml");

            XmlNodeList SheetNameNode = constructInfoXml.GetElementsByTagName("SheetName");
            this.constructInfo["SheetName"] = SheetNameNode[0].InnerText.ToString();

            XmlNodeList PartCountNode = constructInfoXml.GetElementsByTagName("PartCount");
            this.constructInfo["PartCount"] = PartCountNode[0].InnerText.ToString();

            XmlNodeList PartNameNode = constructInfoXml.GetElementsByTagName("PartName");
            this.constructInfo["CPartName"] = PartNameNode[0].InnerText.ToString();
            return true;
        }

        private bool MatUsedInfo()
        {
            XmlDocument matUsedInfoXml = new XmlDocument();
            matUsedInfoXml.Load(this.path + "/UsedMatInfo.xml");

            XmlNodeList MatNameNode = matUsedInfoXml.GetElementsByTagName("MatName");
            this.matUsedInfo["MatName"] = MatNameNode[0].InnerText.ToString();

            XmlNodeList thicknessNode = matUsedInfoXml.GetElementsByTagName("thickness");
            this.matUsedInfo["thickness"] = thicknessNode[0].InnerText.ToString();

            XmlNodeList SheetSizeNode = matUsedInfoXml.GetElementsByTagName("SheetSize");
            this.matUsedInfo["SheetSize"] = SheetSizeNode[0].InnerText.ToString();

            XmlNodeList SheetCodeNode = matUsedInfoXml.GetElementsByTagName("SheetCode");
            this.matUsedInfo["SheetCode"] = SheetCodeNode[0].InnerText.ToString();

            return true;
        }
    }

    class Parts
    {
        private string _PartCode;
        private string _PlanQty;
        private string _CountOnSheet;

        public string PartCode
        {
            get { return _PartCode; }
            set { _PartCode = value; }
        }

        public string PlanQty
        {
            get { return _PlanQty; }
            set { _PlanQty = value; }
        }

        public string CountOnSheet
        {
            get { return _CountOnSheet; }
            set { _CountOnSheet = value; }
        }
    }
}
