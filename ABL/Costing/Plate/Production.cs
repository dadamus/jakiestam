using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.Web;
using System.IO;

namespace ABL.Costing.Plate
{
    public class Production : Costing
    {
        private Listener listener;
        private string plate_dir;
        private string assembly_dir;
        private List<ProductionData.ProgramData> programs = new List<ProductionData.ProgramData>();
        private List<ProductionData.MaterialData> materials = new List<ProductionData.MaterialData>();

        public Production(Listener listener, string plate_dir)
        {
            this.listener = listener;
            this.plate_dir = plate_dir;
			this.assembly_dir = Path.Combine(Directory.GetParent(plate_dir).ToString(), "Assembly\\");
        }

        public bool Process()
        {
			Thread.Sleep(1000);
			if (!this.ConstructInfo()) {
                return false;
            }

			if (!this.UsedMatInfo()) {
				return false;
			}

            this.listener.AddToLog("Wysylam detale na produckji");

            ProductionData.PhpData dataContainer = new ProductionData.PhpData();
            dataContainer.materials = this.materials;
            dataContainer.programs = this.programs;

            string data = JsonConvert.SerializeObject(dataContainer);

            WebClient client = new WebClient();
			client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
			string response = client.UploadString(Form1.phpScript + "?p_a=plate_production_sync", "data=" + data);
			byte[] bytes = Encoding.Default.GetBytes(response);
			response = Encoding.UTF8.GetString(bytes);

            this.listener.AddToLog("Odpowiedz php: " + response);
            return true;
        }

        private bool ConstructInfo() 
        {
            try {
				XmlDocument constructInfo = new XmlDocument();
                constructInfo.Load(this.plate_dir + "/ConstructInfo.xml");
				XmlNodeList sheets = constructInfo.GetElementsByTagName("Sheet");

                for (int s = sheets.Count - 1; s >= 0; s--) {
                    
                    //Upload obrazku do ramki
                    int imageId = s + 1;
					this.listener.sftp.Upload(this.listener.SheetImageDir + imageId + ".bmp");

                    XmlNode sheet = sheets.Item(s);
                    XmlNodeList sheetParams = sheet.ChildNodes;

                    ProductionData.ProgramData programData = new ProductionData.ProgramData();
                    programData.SheetId = s;

					string partPath = Mode3.FindAssemblyFile(this.assembly_dir).FullName;

					for (int p = sheetParams.Count - 1; p >= 0; p--) {
                        XmlNode param = sheetParams.Item(p);

                        switch (param.Name) {
                            case "SheetName":
                                string name = param.InnerText;

                                if (this.ValidProgramName(name) == false) {
                                    return false;
                                }

                                programData.SheetName = HttpUtility.UrlEncode(name);
                                break;

                            case "SheetCount":
                                int sheetCount = 0;

                                if (!Int32.TryParse(param.InnerText, out sheetCount)) {
                                    this.listener.AddToLog("Blad parsowania: SheetCount");
                                    return false;
                                }

                                programData.SheetCount = sheetCount;
                                break;

                            case "Part":
                                ProductionData.DetailData detailData = new ProductionData.DetailData();

                                XmlNodeList partData = param.ChildNodes;
                                for (int pd = partData.Count - 1; pd >= 0; pd--) {
                                    XmlNode partParam = partData.Item(pd);

                                    switch(partParam.Name) {
                                        case "PartName":
                                            detailData.PartName = partParam.InnerText;
                                            break;

                                        case "PartCount":
                                            int partCount = 0;
                                            if (!Int32.TryParse(partParam.InnerText, out partCount)) {
                                                this.listener.AddToLog("Blad parsowania: PartCount");
                                                return false;
                                            }

                                            detailData.Quantity = partCount;
                                            break;
                                    }
                                }

                                programData.AddDetail(detailData);
                                break;
                        }
                    }

                    this.programs.Add(programData);
                }

                return true;
            } catch (Exception ex) {
                this.listener.AddToLog("Wystapil blad construct info: " + ex.Message);
                return false;
            }
        }

		private bool UsedMatInfo()
		{
			try
			{
				XmlDocument usedMatInfoXml = new XmlDocument();
                usedMatInfoXml.Load(this.plate_dir + "/UsedMatInfo.xml");
				XmlNodeList mats = usedMatInfoXml.GetElementsByTagName("Mat");

				for (int m = 0; m < mats.Count; m++)
				{
					XmlNode mat = mats.Item(m);
					XmlNodeList parameters = mat.ChildNodes;
                    ProductionData.MaterialData material = new ProductionData.MaterialData();

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
						}

					}

					this.materials.Add(material);
				}
			}
			catch (Exception ex)
			{
                this.listener.AddToLog("Blad Production: UsedMatInfo: " + ex.Message);
				return false;
			}

			return true;
		}

        private bool ValidProgramName(string name)
        {
            if (name[0].ToString() == "A") {
                return true;
            }

            return false;
        }
    }
}
