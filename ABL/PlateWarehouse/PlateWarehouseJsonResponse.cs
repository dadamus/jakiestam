﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ABL
{
    class PlateWarehouseJsonResponse
    {
        public string insert { get; set; }
        public string[] insert_id { get; set; }
        public string update { get; set; }
        public string[] update_id { get; set; }
        public string delete { get; set; }
    }
}
