using System;
using System.Collections.Generic;

namespace ABL.Costing.Plate.ProductionData
{
    public class ProgramData
    {
        public int SheetId { get; set; }
        public string SheetName { get; set; }
        public int SheetCount { get; set; }
        public List<DetailData> Details;
		public string LaserMatName { get; set; }

        public ProgramData()
        {
            this.Details = new List<DetailData>();
        }

        public void AddDetail(DetailData detail) 
        {
            this.Details.Add(detail);
        }
    }
}
