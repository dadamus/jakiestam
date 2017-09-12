using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace ABL.Costing.Plate
{
    public class Production : Costing
    {
        private Listener listener;
        private string plate_dir;
        private List<ProductionData.ProgramData> programs = new List<ProductionData.ProgramData>();

        public Production(Listener listener, string plate_dir)
        {
            this.listener = listener;
            this.plate_dir = plate_dir;
        }

        public bool Process()
        {
			Thread.Sleep(1000);
			if (!this.ConstructInfo()) {
                return false;
            }

            this.listener.AddToLog("Wysylam detale na produckji");
            string data = JsonConvert.SerializeObject(this.programs);

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
                    XmlNode sheet = sheets.Item(s);
                    XmlNodeList sheetParams = sheet.ChildNodes;

                    ProductionData.ProgramData programData = new ProductionData.ProgramData();

                    for (int p = sheetParams.Count - 1; p >= 0; p--) {
                        XmlNode param = sheetParams.Item(p);

                        switch (param.Name) {
                            case "SheetName":
                                string name = param.InnerText;

                                if (this.ValidProgramName(name) == false) {
                                    return false;
                                }

                                programData.SheetName = name;
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
                }

                return true;
            } catch (Exception ex) {
                this.listener.AddToLog("Wystapil blad construct info: " + ex.Message);
                return false;
            }
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
