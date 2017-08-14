using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABL.Costing.Plate.Mode3Data
{
    class Response
    {
        private List<ProgramData> programs;
        private List<MaterialData> materials;
        private List<ProgramCardData> programsData;

        public void SetPrograms(List<ProgramData> programs)
        {
            this.programs = programs;
        }

        public void SetMaterials(List<MaterialData> materials)
        {
            this.materials = materials;
        }

        public void SetProgramsData(List<ProgramCardData> programsData)
        {
            this.programsData = programsData;
        }
    }
}
