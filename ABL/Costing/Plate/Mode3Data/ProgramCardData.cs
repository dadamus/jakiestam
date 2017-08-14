using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABL.Costing.Plate.Mode3Data
{
    class ProgramCardData
    {
        private List<ProgramCardPartData> parts = new List<ProgramCardPartData>();

        public string SheetName { get; set; }
        public int SheetCount { get; set; }

        public void SetParts(List<ProgramCardPartData> parts)
        {
            this.parts = parts;
        }

        public void AddPart(ProgramCardPartData part)
        {
            this.parts.Add(part);
        }

        public List<ProgramCardPartData> GetParts()
        {
            return this.parts;
        }
    }
}
