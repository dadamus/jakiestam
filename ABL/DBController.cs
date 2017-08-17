using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace ABL
{
    public class DBController
    {
        private string pathToAicamBases = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\AicamBases.mdb";
        private string pathToDataReport = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\DataReport.mdb";

        private bool AicamBasesOpen = false;
        private bool DataReportOpen = false;

        private OleDbConnection DataReport;
        private OleDbConnection AicamBases;

        private int maxMemoLength = 600000;

        protected Listener listener;

        public DBController(Listener listener = null)
        {
            this.listener = listener;
            this.DataReport = new OleDbConnection("Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + this.pathToDataReport + ";JET OLEDB:Database Password=sqlusr");
            this.AicamBases = new OleDbConnection("Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + this.pathToAicamBases + ";JET OLEDB:Database Password=sqlusr");
            this.openAicamBases();
            try
            {
                OleDbCommand query = new OleDbCommand("CREATE TABLE `tmaterialsynced` (`MaterialName` VARCHAR(50),`Thickness` FLOAT, `MaterialTypeName` VARCHAR(50), `Clearance` FLOAT,`Comment` Memo, `synced` INTEGER);", this.AicamBases);
                query.ExecuteNonQuery();
            }
            catch (Exception ex) {
            }

            try
            {
                /**CREATE TABLE `platewarehousesynced` 
(   `SheetCode` TEXT(255),
   `MaterialName` TEXT(50),
   `QtyAvailable` INTEGER,
   `GrainDirection` INTEGER,      
   `Width` FLOAT,                        
   `Height` FLOAT,
   `SpecialInfo` TEXT(50),
   `Comment` MEMO,
   `SheetType` TEXT(50),
   `SkeletonFile` TEXT(150),
   `SkeletonData` MEMO,       
   `MD5` TEXT(30),
   `Price` FLOAT,                 
   `Priority` SMALLINT,          
   `synced` INTEGER         
);                    **/
                OleDbCommand query = new OleDbCommand("CREATE TABLE `platewarehousesynced` ( `MaterialName` VARCHAR(50),`Thickness` FLOAT,`MaterialTypeName` VARCHAR(50),`Clearance` FLOAT,`Comment` text,`synced` INTEGER)");
                query.ExecuteNonQuery();
            }
            catch (Exception)
            {
            }

            OleDbCommand query3 = new OleDbCommand("DELETE FROM platewarehousesynced", this.AicamBases);
            //query3.ExecuteNonQuery();
            OleDbCommand query2 = new OleDbCommand("DELETE FROM tmaterialsynced", this.AicamBases);
            //query2.ExecuteNonQuery();
            
            this.closeAicamBases();
        }

        protected void addToLog(string text)
        {
            if (this.listener != null)
            {
                this.listener.AddToLog(text);
            }
        }

        public void openAicamBases()
        {
            if (this.AicamBases.State != System.Data.ConnectionState.Open)
            {
                this.AicamBases.Open();
                this.AicamBasesOpen = true;
            }
        }

        public void closeAicamBases()
        {
            if (this.AicamBases.State == System.Data.ConnectionState.Open)
            {
                this.AicamBases.Close();
                this.AicamBasesOpen = false;
            }
        }

        public void openDataReport()
        {
            if (this.DataReport.State != System.Data.ConnectionState.Open)
            {
                this.DataReport.Open();
                this.DataReportOpen = true;
            }
        }

        public void closeDataReport()
        {
            if (this.DataReport.State == System.Data.ConnectionState.Open)
            {
                this.DataReport.Close();
                this.DataReportOpen = false;
            }
        }

        public void insertMemo(string Table, string cellName, string SheetCode, string memo, bool deleteExist = true)
        {
            int cutLength = memo.Length;
            if (cutLength > this.maxMemoLength)
            {
                cutLength = this.maxMemoLength;
            }

            string data = memo.Substring(0, cutLength);

            string query = "UPDATE " + Table + " SET " + cellName + " = '" + data + "' WHERE SheetCode = '" + SheetCode + "'";

            if (!deleteExist)
            {
                query = "UPDATE " + Table + " SET [" + cellName + "] = [" + cellName + "] & '" + data + "' WHERE SheetCode = '" + SheetCode + "'";
            }

            this.executeQueryAicamBases(query);

            query = null;
            data = null;
            GC.Collect();
            if (memo.Length >= this.maxMemoLength) {
                this.insertMemo(Table, cellName, SheetCode, memo.Substring(cutLength), false);
            }
        }

        public Dictionary<string, string> getProcessTimeTable(string sheetName)
        {
            this.DataReport.Open();
            OleDbCommand command = new OleDbCommand("SELECT CutPathTime, MoveTime, SHCutTime, PierceTime, NumberPierces, ProcessLength, SHLength FROM ProcessTime WHERE SheetName = '" + sheetName + "'", this.DataReport);
            OleDbDataReader reader = command.ExecuteReader();
            reader.Read();

            Dictionary<string, string> d = new Dictionary<string, string>();
            if (reader.HasRows)
            {
                d["db_CutPathTime"] = reader.GetValue(0).ToString();
                d["db_MoveTime"] = reader.GetValue(1).ToString();
                d["db_SHCutTime"] = reader.GetValue(2).ToString();
                d["db_PierceTime"] = reader.GetValue(3).ToString();
                d["db_NumberPierces"] = reader.GetValue(4).ToString();
                d["db_ProcessLength"] = reader.GetValue(5).ToString();
                d["db_SHLength"] = reader.GetValue(6).ToString();
            }

            this.DataReport.Close();
            return d;
        }

        public Dictionary<string, string> getSheetTable(string sheetName)
        {
            this.openDataReport();
            OleDbCommand command = new OleDbCommand("SELECT SheetCount, Image, MaterialName, LaserMaterialName, SheetSize, Thickness, UsedRatio FROM Sheet WHERE SheetName = '" + sheetName + "'", this.DataReport);
            Dictionary<string, string> d = new Dictionary<string, string>();

            try
            {
                OleDbDataReader reader = command.ExecuteReader();
                reader.Read();
                if (reader.HasRows)
                {
                    d["db_SheetCount"] = reader.GetValue(0).ToString();
                    d["db_Image"] = reader.GetValue(1).ToString();
                    d["db_MaterialName"] = reader.GetValue(2).ToString();
                    d["db_LaserMaterialName"] = reader.GetValue(3).ToString();
                    d["db_SheetSize"] = reader.GetValue(4).ToString();
                    d["db_Thickness"] = reader.GetValue(5).ToString();
                    d["db_UsedRatio"] = reader.GetValue(6).ToString();
                }
            }
            catch (Exception ex)
            {
                listener.AddToLog(ex.ToString());
            }
            
            this.closeDataReport();
            return d;
        }

        public List<int> executeQueryAicamBases(string query)
        {
            this.openAicamBases();
            string[] querys = query.Split(';');

            List<int> query_errors = new List<int>();

            try
            {
                for (int i = 0; i < querys.Length; i++)
                {
                    OleDbCommand command = new OleDbCommand(querys[i], this.AicamBases);
                    if (command.ExecuteNonQuery() == 0)
                    {
                        query_errors.Add(i);
                    }
                }
            } catch (Exception ex)
            {
                addToLog(ex.ToString());
            }

            this.closeAicamBases();

            return query_errors;
        }

        public bool InsertPlate(T_MaterialSheet plate)
        {
            this.openAicamBases();
            string sCommand = "INSERT INTO `T_MaterialSheet` " + plate.GenerateInsertSQL();
            try
            {
                OleDbCommand command = new OleDbCommand(sCommand, this.AicamBases);
                int response = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return false;
            }

            this.closeAicamBases();
            return true;
        }

        public void InsertPlateSynced(T_MaterialSheet plate)
        {
            this.AicamBases.Open();
            string sCommand = "INSERT INTO `platewarehousesynced` " + plate.GenerateInsertSQL();

            OleDbCommand command = new OleDbCommand(sCommand, this.AicamBases);
            int response = command.ExecuteNonQuery();
            this.AicamBases.Close();
        }

        public List<T_material> GetNotSynchronizedMaterial(Listener listner)
        {
            T_material material_sql = new T_material();
            string sql = "SELECT tm.* FROM `T_material` tm LEFT JOIN `tmaterialsynced` s ON ";
            sql += material_sql.GenerateCheckSyncedSql("tm", "s");
            sql += " WHERE s.synced IS NULL";

            this.openAicamBases();
            OleDbCommand command = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = command.ExecuteReader();

            List<T_material> materials = new List<T_material>();

            while(reader.Read())
            {
                if (!reader.HasRows)
                {
                    continue;
                }
                
                T_material material = new T_material();
                material.MaterialName = reader.GetValue(0).ToString();
                material.Thickness = (float)reader.GetDouble(1);
                material.MaterialTypeName = reader.GetValue(2).ToString();
                material.Clearance = (float)reader.GetDouble(3);
                material.Comment = reader.GetValue(4).ToString();

                materials.Add(material);
            }

            reader.Close();
            this.closeAicamBases();

            return materials;
        }

        public List<T_MaterialSheet> GetNotSynchronized(Listener listner)
        {
            T_MaterialSheet plate_sql = new T_MaterialSheet();
            string sql = "SELECT tms.* FROM `T_MaterialSheet` tms LEFT JOIN `plateWarehouseSynced` s ON ";
            sql += plate_sql.GenerateCheckSyncedSql("tms", "s");
            sql += " WHERE s.synced IS NULL";

            this.openAicamBases();
            OleDbCommand command = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = command.ExecuteReader();

            List<T_MaterialSheet> plates = new List<T_MaterialSheet>();

            while (reader.Read())
            {
                if (!reader.HasRows)
                {
                    continue;
                }

                T_MaterialSheet plate = new T_MaterialSheet();
                plate.SheetCode = reader.GetString(0);
                plate.MaterialName = reader.GetString(1);
                plate.QtyAvailable = reader.GetInt32(2);
                plate.GrainDirection = reader.GetInt32(3);
                plate.Width = (float)reader.GetDouble(4);
                plate.Height = (float)reader.GetDouble(5);
                plate.SpecialInfo = reader.GetString(6);
                plate.Comment = "";
                plate.SheetType = reader.GetString(8);
                plate.SkeletonFile = reader.GetValue(9).ToString();
                plate.MD5 = reader.GetValue(11).ToString();
                plate.Price = (float)reader.GetDouble(12);
                plate.Priority = reader.GetInt16(13);

                plates.Add(plate);
            }

            reader.Close();
            this.closeAicamBases();
            return plates;
        }

        public List<T_MaterialSheet> getNotSyncedSkeletonData(string from, string to)
        {
            string sql = "SELECT f.SheetCode, f.SkeletonData, f.Comment FROM " + from + " f LEFT JOIN " + to + " t ON f.SheetCode = t.SheetCode WHERE (t.SkeletonData is null OR t.SkeletonData <> f.SkeletonData) AND f.SkeletonData is not null";

            if (this.AicamBasesOpen == false)
            {
                throw new Exception("Baza nie otwarta!");
            }

            OleDbCommand oleDb = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = oleDb.ExecuteReader();

            List<T_MaterialSheet> data = new List<T_MaterialSheet>();
            while (reader.Read())
            {
                T_MaterialSheet ms = new T_MaterialSheet();
                ms.SheetCode = reader.GetString(0);
                ms.SkeletonData = reader.GetString(1);
                ms.Comment = "";
                data.Add(ms);
            }
            reader.Close();
            return data;
        }
    }
}
