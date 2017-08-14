using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABL
{
    public class T_material
    {
        public string MaterialName { get; set; }
        public float Thickness { get; set; }
        public string MaterialTypeName { get; set; }
        public float Clearance { get; set; }
        public string Comment { get; set; }

        public string generateInsertSql()
        {
            object obj = this;
            var properties = obj.GetType().GetProperties();

            string sql_header = "";
            string sql_data = "";

            foreach (var p in properties)
            {
                if (sql_header.Length > 0)
                {
                    sql_header += ", ";
                    sql_data += ", ";
                }

                sql_header += "`" + p.Name + "`";
                var value = p.GetValue(this, null);

                string mark = "'";

                if (typeof(int) == value.GetType() || typeof(float) == value.GetType())
                {
                    mark = "";
                }

                sql_data += mark + value + mark;
            }

            return "(" + sql_header + ") VALUES (" + sql_data + ")";
        }

        public string GenerateCheckSyncedSql(string T_MaterialPrefix, string SyncedMaterialTablePrefix)
        {
            object obj = this;
            var properties = obj.GetType().GetProperties();
            int prop_count = properties.Length;

            string sql = "";

            for (int i = 0; i < prop_count; i++)
            {
                string prop_name = properties[i].Name;
                
                if (prop_name == "Comment")
                {
                    continue;
                }

                if (i < prop_count - 1 && i > 0)
                {
                    sql += " AND ";
                }
                sql += T_MaterialPrefix + ".`" + prop_name + "` = " + SyncedMaterialTablePrefix + ".`" + prop_name + "`";
            }

            return sql;
        }
    }
}
