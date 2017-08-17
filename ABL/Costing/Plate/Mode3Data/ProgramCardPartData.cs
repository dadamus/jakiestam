using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABL.Costing.Plate.Mode3Data
{
    class ProgramCardPartData
    {
        public int PartNo { get; set; }
        public string PartName { get; set; }
        public int PartCount { get; set; }
        public float UnfoldXSize { get; set; }
        public float UnfoldYSize { get; set; }
        public float RectangleArea { get; set; }
        public float RectangleAreaW { get; set; }
        public float RectangleAreaWO { get; set; }
        public float Weight { get; set; }
        public string LaserMatName { get; set; }
    }
}
