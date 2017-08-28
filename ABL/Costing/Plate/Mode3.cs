using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace ABL.Costing.Plate
{
    class Mode3 : ABL.Costing.Costing
    {
        private Listener listener;
        private string dir;
        private string assembly_dir;
        private List<Mode3Data.ProgramData> programs = new List<Mode3Data.ProgramData>();
        private List<Mode3Data.MaterialData> materials = new List<Mode3Data.MaterialData>();
        private List<Mode3Data.ProgramCardData> programsData = new List<Mode3Data.ProgramCardData>();
        private Dictionary<string, int> partId = new Dictionary<string, int>();
        private Dictionary<int, string> partImgUrl = new Dictionary<int, string>();

        public Mode3(Listener listener, string dir)
        {
            this.listener = listener;
            this.dir = dir;
            this.assembly_dir = Path.Combine(Directory.GetParent(dir).ToString(), "Assembly\\");
        }

        public bool Process()
        {
            if (!this.NestingManageReport()) {
                return false;
            }
            if (!this.ScheduleSheet())
            {
                return false;
            }

            if (!this.UsedMatInfo())
            {
                return false;
            }

            if (!this.ConstructInfo())
            {
                return false;
            }

            //Serializujemy wszystko!!!
            string response = this.MakeResponse();

            WebClient client = new WebClient();
            this.listener.AddToLog("Wysylam blachy multi");
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string webresponse = client.UploadString(Form1.phpScript + "?p_a=multipart_plate_costing", "data=" + response);
            byte[] bytes = Encoding.Default.GetBytes(webresponse);
            webresponse = Encoding.UTF8.GetString(bytes);
            this.listener.AddToLog("Odpowiedz od php: " + webresponse);

            return true;
        }

        private string MakeResponse()
        {
            Mode3Data.Response response = new Mode3Data.Response();
            response.SetPrograms(this.programs);
            response.SetProgramsData(this.programsData);
            response.SetMaterials(this.materials);

            return JsonConvert.SerializeObject(response);
        }

        private Mode3Data.ProgramCardPartData GetPartsData(XmlNodeList partsNode, string path, string ProjectName)
        {
            Mode3Data.ProgramCardPartData data = new Mode3Data.ProgramCardPartData();
            for (int a = 0; a < partsNode.Count; a++)
            {
                XmlNode parameter = partsNode.Item(a);

                switch (parameter.Name)
                {
                    case "PartNo":
                        int PartNo = 0;
                        if (!Int32.TryParse(parameter.InnerText, out PartNo))
                        {
                            throw new Exception("Blad part data: PartNo");
                        }
                        data.PartNo = PartNo;
                        break;
                    case "PartName":
                        data.PartName = parameter.InnerText;
                        break;
                    case "PartCount":
                        int PartCount = 0;
                        if (!Int32.TryParse(parameter.InnerText, out PartCount))
                        {
                            throw new Exception("Blad part data: part count");
                        }
                        data.PartCount = PartCount;
                        break;
                }
            }

            //Pobieramy xmla partu
            XmlDocument partXml = new XmlDocument();
            partXml.Load(Path.Combine(path, "Part_" + data.PartName + ".xml"));

            //Najpierw nazwa materialu bo jest w dziwnym miejscu
            XmlNode LaserMatNameNode = partXml.GetElementsByTagName("LaserMatName").Item(0);
            data.LaserMatName = LaserMatNameNode.InnerText;

            //Reszta danych
            XmlNodeList partParameters = partXml.GetElementsByTagName("ExtraData").Item(0).ChildNodes;

            for (int p = partParameters.Count; p > 0; p--)
            {
                XmlNode partParameter = partParameters.Item(p - 1);

                switch (partParameter.Name)
                {
                    case "UnfoldXSize":
                        data.UnfoldXSize = this.ParseFloat(partParameter.InnerText);
                        break;
                    case "UnfoldYSize":
                        data.UnfoldYSize = this.ParseFloat(partParameter.InnerText);
                        break;
                    case "RectangleArea":
                        data.RectangleArea = this.ParseFloat(partParameter.InnerText);
                        break;
                    case "RectangleAreaW":
                        data.RectangleAreaW = this.ParseFloat(partParameter.InnerText);
                        break;
                    case "RectangleAreaWO":
                        data.RectangleAreaWO = this.ParseFloat(partParameter.InnerText);
                        break;
                    case "Weight":
                        data.Weight = this.ParseFloat(partParameter.InnerText);
                        break;
                }
            }

			//Miniaturka
			int id = partId[data.PartName];

            string oldImgPath = Path.Combine(this.dir, ProjectName, id + ".jpg");
            string newImgPath = Path.Combine(this.dir, ProjectName, "pimg_" + data.PartNo + ".jpg");

            if (File.Exists(oldImgPath)) {
                File.Copy(oldImgPath, newImgPath);
                this.listener.AddToLog("Wysylam obrazek, part: " + data.PartNo);
                this.ImageUpload(newImgPath);
                File.Delete(newImgPath);
            } else {
                this.listener.AddToLog("Brak obrazka: " + oldImgPath);
            }

			return data;
        }

        private float ParseFloat(string input)
        {
            float output = 0;
            if (!float.TryParse(input, out output))
            {
                throw new Exception("Blad parsowania floatu! " + input);
            }
            return output;
        }

        //Plik potrzebny tylko do miniaturek
		bool NestingManageReport()
        {
            try {
                XmlDocument nestingManageReport = new XmlDocument();
                nestingManageReport.Load(this.dir + "/NestingManageReport.xml");
                XmlNodeList parts = nestingManageReport.GetElementsByTagName("Part");

                for (int p = 0; p < parts.Count; p++) {
                    XmlNode part = parts.Item(p);
                    XmlNodeList parameters = part.ChildNodes;

                    int seq = 0;
                    string partName = "";

                    for (int a = 0; a < parameters.Count; a++) {
                        XmlNode parameter = parameters.Item(a);

                        switch (parameter.Name) {
                            case "Seq":
                                Int32.TryParse(parameter.InnerText, out seq);
                                break;

                            case "PartCode":
                                partName = parameter.InnerText;
                                break;
                        }
                    }

                    this.partId.Add(partName, seq);
                }
            } catch (Exception ex) {
                this.listener.AddToLog("Błąd NestingManageReport: " + ex.Message);
                return false;
            }
            return true;
        }


		bool ConstructInfo()
        {
            try
            {
                XmlDocument constructInfoXml = new XmlDocument();
                constructInfoXml.Load(this.dir + "/ConstructInfo.xml");
                XmlNodeList programs = constructInfoXml.GetElementsByTagName("Sheet");
                string partPath = this.FindAssemblyFile().FullName;

                for (int p = 0; p < programs.Count; p++)
                {
                    XmlNode program = programs.Item(p);
                    XmlNodeList parameters = program.ChildNodes;

                    Mode3Data.ProgramCardData programData = new Mode3Data.ProgramCardData();

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        XmlNode parameter = parameters.Item(i);

                        switch (parameter.Name)
                        {
                            case "SheetName":
                                programData.SheetName = parameter.InnerText;
                                break;

                            case "SheetCount":
                                int SheetCount = 0;
                                if (!Int32.TryParse(parameter.InnerText, out SheetCount))
                                {
                                    this.listener.AddToLog("Blad parsowania ConstructInfo: SheetCount");
                                    return false;
                                }

                                programData.SheetCount = SheetCount;
                                break;
                        }
                    }

					XmlNodeList programNodes = program.ChildNodes;
                    for (int pp = 0; pp < programNodes.Count; pp++)
                    {
                        XmlNode programNode = programNodes.Item(pp);
                        if (programNode.Name == "Part")
                        {
                            programData.AddPart(this.GetPartsData(programNode.ChildNodes, partPath, programData.SheetName));
                        }
                    }

                    this.programsData.Add(programData);
                }
            }
            catch (Exception ex)
            {
                this.listener.AddToLog("Blad ConstructInfo: " + ex.Message);
                return false;
            }

            return true;
        }

        private bool UsedMatInfo()
        {
            try
            {
                XmlDocument usedMatInfoXml = new XmlDocument();
                usedMatInfoXml.Load(this.dir + "/UsedMatInfo.xml");
                XmlNodeList mats = usedMatInfoXml.GetElementsByTagName("Mat");

                for (int m = 0; m < mats.Count; m++)
                {
                    XmlNode mat = mats.Item(m);
                    XmlNodeList parameters = mat.ChildNodes;
                    Mode3Data.MaterialData material = new Mode3Data.MaterialData();

                    for (int p = 0; p < parameters.Count; p++)
                    {
                        XmlNode parameter = parameters.Item(p);

                        switch (parameter.Name)
                        {
                            case "SheetCode":
                                material.SheetCode = parameter.InnerText;
                                break;

                            case "UsedSheetNum":
                                int UsedSheetNum;
                                if (!Int32.TryParse(parameter.InnerText, out UsedSheetNum))
                                {
                                    this.listener.AddToLog("Blad parsowania UsedSheetNum");
                                    return false;
                                }
                                material.UsedSheetNum = UsedSheetNum;
                                break;

                            case "MatName":
                                material.MatName = parameter.InnerText;
                                break;

                            case "thickness":
                                material.thickness = parameter.InnerText;
                                break;

                            case "SheetSize":
                                material.SheetSize = parameter.InnerText;
                                break;
                        }

                    }

                    this.materials.Add(material);
                }
            }
            catch (Exception ex)
            {
                this.listener.AddToLog("Blad UsedMatInfo: " + ex.Message);
                return false;
            }

            return true;
        }

        private void ImageUpload(string path)
        {
            this.listener.sftp.Upload(path);
        }

        private bool ScheduleSheet()
        {
            try
            {
                XmlDocument scheduleSheetXML = new XmlDocument();
                scheduleSheetXML.Load(this.dir + "/ScheduleSheet.xml");
                XmlNodeList detailsNode = scheduleSheetXML.GetElementsByTagName("Sheet");

                for (int n = 0; n < detailsNode.Count; n++)
                {
                    XmlNode detail = detailsNode.Item(n);
                    XmlNodeList parameters = detail.ChildNodes;

                    //Upload obrazku do ramki
                    int imageId = n + 1;
                    this.ImageUpload(this.listener.SheetImageDir + imageId + ".bmp");

                    Mode3Data.ProgramData programData = new Mode3Data.ProgramData();

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
                                programData.SheetName = parameter.InnerText;
                                break;

                            case "UsedSheetNum":
                                int UsedSheetNum = 0;
                                if (!Int32.TryParse(parameter.InnerText, out UsedSheetNum))
                                {
                                    throw new Exception("Brak UsedSheetNum!");
                                }
                                programData.UsedSheetNum = UsedSheetNum;
                                break;

                            case "PreTime":
                                programData.PreTime = parameter.InnerText;
                                break;
                        }

                    }

                    this.programs.Add(programData);

                }
            }
            catch (XmlException ex)
            {
                this.listener.AddToLog("BLAD XML: " + ex.Message);
                return false;
            }

            return true;
        }

        private int ValidDetailName(string name)
        {
            if (name.IndexOf('+') < 0)
            {
                return 0;
            }

            if (name[0] == 'M')
            {
                return 1;
            }

            return 0;
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