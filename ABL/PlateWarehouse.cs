using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace ABL
{
    class PlateWarehouse
    {
        private DBController dbController;
        private string scriptPath = "";

        public PlateWarehouse(DBController dbController)
        {
            this.scriptPath = Form1.phpScript;
            this.dbController = dbController;
        }

        public void sync()
        {
            WebClient webClient = new WebClient();
            string reply = webClient.DownloadString(this.scriptPath + "?p_a=plate_warehouse_sync_data");

            byte[] bytes = Encoding.Default.GetBytes(reply);
            reply = Encoding.UTF8.GetString(bytes);

            dynamic data = JsonConvert.DeserializeObject(reply);


            string synced = "[";

            for (int i = 0; i < data.Count; i++)
            {
                dynamic row = data[i];

                T_MaterialSheet plate = new T_MaterialSheet();
                plate.SheetCode = row.SheetCode;
                plate.MaterialName = row.MaterialName;
                plate.QtyAvailable = (int)row.QtyAvailable;
                plate.GrainDirection = (int)row.GrainDirection;
                plate.Width = (float)row.Width;
                plate.Height = (float)row.Height;
                plate.SpecialInfo = row.SpecialInfo;
                plate.SheetType = row.SheetType;
                plate.SkeletonFile = row.SkeletonFile;
                plate.SkeletonData = row.SkeletonData;
                plate.MD5 = row.MD5;
                plate.Price = (float)row.Price;
                plate.Priority = (int)row.Priority;
                
                if (dbController.InsertPlate(plate))
                {
                    dbController.InsertPlateSynced(plate);
                    if (synced.Length > 2)
                    {
                        synced += ", ";
                    }
                    synced += '"' + plate.SheetCode + '"';
                  
                }
            }
            synced += "]";

            webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string response = webClient.UploadString(this.scriptPath + "?p_a=plate_warehouse_sync_respond", "synced=" + synced);
        }
    }
}
