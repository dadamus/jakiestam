using System;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ABL
{
    public class SyncController
    {
        private string pathToAicamBases = "C:\\Program Files (x86)\\Amada\\AI-CAM\\AIC_Main\\AicamBases.mdb";
        //private string pathToAicamBases = "Y:\\temp2\\baza\\AicamBases.mdb";
        private OleDbConnection AicamBases;
        protected Listener listener;

        public SyncController(Listener listener = null)
        {
            this.listener = listener;
            this.AicamBases = new OleDbConnection("Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + this.pathToAicamBases + ";JET OLEDB:Database Password=sqlusr");
        }

        public void Sync()
        {
            List<T_MaterialSheet> newSheets = new List<T_MaterialSheet>();
            List<T_MaterialSheet> updatedSheets = new List<T_MaterialSheet>();
            List<T_MaterialSheet> deletedSheets = new List<T_MaterialSheet>();


            newSheets = this.GetNewSheets();
            this.SaveNewSheets(newSheets);

            updatedSheets = this.GetUpdeatedSheets();
            this.UpdatePlates(updatedSheets);

            deletedSheets = this.GetDeletedSheets();
            this.DeletePlates(deletedSheets);

            if (newSheets.Count > 0 || updatedSheets.Count > 0 || deletedSheets.Count > 0)
            {
                this.listener.AddToLog("Nowych blach: " + newSheets.Count + " zaktualizowanych: " + updatedSheets.Count + " usunietych: " + updatedSheets.Count);
            }
        }

        private List<T_MaterialSheet> GetNewSheets()
        {
            this.OpenAicamBases();
            string sql = "SELECT tms.* FROM `T_MaterialSheet` tms LEFT JOIN `platewarehousesynced` s ON s.SheetCode = tms.SheetCode WHERE s.SheetCode IS NULL";
            OleDbCommand query = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = query.ExecuteReader();

            List<T_MaterialSheet> toInsert = new List<T_MaterialSheet>();

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
                plate.SheetType = reader.GetString(8);
                plate.SkeletonFile = reader.GetValue(9).ToString();
                plate.MD5 = reader.GetValue(11).ToString();
                plate.Price = (float)reader.GetDouble(12);
                plate.Priority = reader.GetInt16(13);

                toInsert.Add(plate);
            }

            reader.Close();
            this.CloseAicamBases();

            return toInsert;
        }

        private void SaveNewSheets(List<T_MaterialSheet> toInsert) {
            this.OpenAicamBases();

            foreach (T_MaterialSheet plate in toInsert)
            {
                string insertSQL = "INSERT INTO platewarehousesynced " + plate.GenerateInsertSQL();
                OleDbCommand command = new OleDbCommand(insertSQL, this.AicamBases);
                command.ExecuteNonQuery();

                this.SendMessage("insert_plate_warehouse", plate);
            }

            this.CloseAicamBases();
        }

        private List<T_MaterialSheet> GetUpdeatedSheets()
        {
            this.OpenAicamBases();

            T_MaterialSheet MaterialSheet = new T_MaterialSheet();

            string sql = "SELECT tms.* FROM `T_MaterialSheet` tms LEFT JOIN `platewarehousesynced` s ON ";
            sql += MaterialSheet.GenerateCheckSyncedSql("tms", "s");
            sql += " WHERE s.SheetCode IS NULL AND tms.SheetCode IS NOT NULL";

            OleDbCommand query = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = query.ExecuteReader();

            List<T_MaterialSheet> toUpdate = new List<T_MaterialSheet>();

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
                plate.SheetType = reader.GetString(8);
                plate.SkeletonFile = reader.GetValue(9).ToString();
                plate.MD5 = reader.GetValue(11).ToString();
                plate.Price = (float)reader.GetDouble(12);
                plate.Priority = reader.GetInt16(13);

                toUpdate.Add(plate);
            }

            reader.Close();
            this.CloseAicamBases();

            return toUpdate;
        }

        private void UpdatePlates(List<T_MaterialSheet> toUpdate)
        {
            this.OpenAicamBases();

            foreach (T_MaterialSheet plate in toUpdate)
            {
                string updateSQL = "UPDATE platewarehousesynced SET " + plate.GenerateUpdateSQL() + " WHERE SheetCode = '" + plate.SheetCode + "'";
                OleDbCommand command = new OleDbCommand(updateSQL, this.AicamBases);
                command.ExecuteNonQuery();

                this.SendMessage("update_plate_warehouse", plate);
            }

            this.CloseAicamBases();
        }

        private List<T_MaterialSheet> GetDeletedSheets()
        {
            this.OpenAicamBases();
            string sql = "SELECT s.* FROM `platewarehousesynced` s LEFT JOIN `T_MaterialSheet` tms ON s.SheetCode = tms.SheetCode WHERE tsm.SheetCode IS NULL";
            OleDbCommand query = new OleDbCommand(sql, this.AicamBases);
            OleDbDataReader reader = query.ExecuteReader();

            List<T_MaterialSheet> toDelete = new List<T_MaterialSheet>();

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
                plate.SheetType = reader.GetString(8);
                plate.SkeletonFile = reader.GetValue(9).ToString();
                plate.MD5 = reader.GetValue(11).ToString();
                plate.Price = (float)reader.GetDouble(12);
                plate.Priority = reader.GetInt16(13);

                toDelete.Add(plate);
            }

            reader.Close();

            this.CloseAicamBases();

            return toDelete;
        }

        private void DeletePlates(List<T_MaterialSheet> toDelete)
        {
            this.OpenAicamBases();

            foreach (T_MaterialSheet plate in toDelete)
            {
                string updateSQL = "DELETE FROM platewarehousesynced WHERE SheetCode = '" + plate.SheetCode + "'";
                OleDbCommand command = new OleDbCommand(updateSQL, this.AicamBases);
                command.ExecuteNonQuery();

                this.SendMessage("delete_plate_warehouse", plate);
            }

            this.CloseAicamBases();
        }

        public void OpenAicamBases()
        {
            if (this.AicamBases.State != System.Data.ConnectionState.Open)
            {
                this.AicamBases.Open();
            }
        }

        public void CloseAicamBases()
        {
            if (this.AicamBases.State == System.Data.ConnectionState.Open)
            {
                this.AicamBases.Close();
            }
        }

        public void SendMessage(string type, T_MaterialSheet materialSheet)
        {
            WebClient client = new WebClient();
            this.listener.AddToLog("Wysylam wiadomosc");

            string dataSQL = "";

            switch (type)
            {
                case "insert_plate_warehouse":
                    dataSQL = materialSheet.GenerateInsertSQL();
                    break;

                case "update_plate_warehouse":
                    dataSQL = materialSheet.GenerateUpdateSQL();
                    break;

                case "delete_plate_warehouse":
                    dataSQL = "SheetCode = " + materialSheet.SheetCode;
                    break;
            }

            var dataBytes = System.Text.Encoding.UTF8.GetBytes(dataSQL);
            dataSQL = System.Convert.ToBase64String(dataBytes);

            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string webresponse = client.UploadString(Form1.phpScript + "?p_a=" + type, "data=" + dataSQL);
            byte[] bytes = Encoding.Default.GetBytes(webresponse);
            webresponse = Encoding.UTF8.GetString(bytes);

            this.listener.AddToLog("Odpowiedz od php: " + webresponse);
        }

    }
}
