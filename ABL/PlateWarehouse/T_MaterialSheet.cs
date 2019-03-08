using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABL
{
    public class T_MaterialSheet
    {
        public string SheetCode { get; set; }
        public string MaterialName { get; set; }
        public int QtyAvailable { get; set; }
        public int GrainDirection { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string SpecialInfo { get; set; }
        public string SheetType { get; set; }
        public string SkeletonFile { get; set; }
        public string SkeletonData { get; set; }
        public string MD5 { get; set; }
        public float Price { get; set; }
        public int Priority { get; set; }

        public T_MaterialSheet()
        {
            this.SkeletonData = "";
        }

        public string GenerateUpdateSQL()
        {
            object obj = this;
            var properties = obj.GetType().GetProperties();

            string sql_data = "";

            foreach (var p in properties)
            {

                if (sql_data.Length > 0)
                {
                    sql_data += ", ";
                }

                var value = p.GetValue(this, null);

                string mark = "'";

                if (typeof(int) == value.GetType() || typeof(float) == value.GetType())
                {
                    mark = "";
                }

                sql_data += "`" + p.Name + "` = " + mark + value + mark;
            }

            return sql_data;
        }

        public string GenerateInsertSQL(bool createdDate = false)
        {
            object obj = this;
            var properties = obj.GetType().GetProperties();

            string sql_header = "";
            string sql_data = "";

            foreach(var p in properties)
            {

                if (sql_header.Length > 0)
                {
                    sql_header += ", ";
                    sql_data += ", ";
                }

                sql_header  += "`" + p.Name + "`";
                var value = p.GetValue(this, null);

                string mark = "'";

                if (typeof(int) == value.GetType() || typeof(float) == value.GetType())
                {
                    mark = "";
                }

                sql_data += mark + value + mark;
            }

            if (createdDate)
            {
                DateTime date = DateTime.Now;
                sql_header += ", createdDate";
                sql_data += ", " + date.ToString("yyyy-MM-d HH:mm:s");
            }

            return "(" + sql_header + ") VALUES (" + sql_data + ")";
        }

        public string GenerateCheckSyncedSql(string T_MaterialSheetPrefix, string SyncedTablePrefix)
        {
            object obj = this;
            var properties = obj.GetType().GetProperties();
            int prop_count = properties.Length;

            string sql = "";

            for (int i = 0; i < prop_count; i++)
            {
                string prop_name = properties[i].Name;

                if (prop_name == "Comment" || prop_name == "SkeletonData")
                {
                    continue;
                }

                sql += T_MaterialSheetPrefix + ".`" + prop_name + "` = " + SyncedTablePrefix + ".`" + prop_name + "`";
                if (i < prop_count - 1)
                {
                    sql += " AND ";
                }
            }

            return sql;
        }
    }
}
