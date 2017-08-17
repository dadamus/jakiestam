using System;

namespace ABL
{
    public class lastEditedFile
    {
        public DateTime date;
        public string dir, name;

        public lastEditedFile(string dir, string name, DateTime date) {
            this.date = date;
            this.dir = dir;
            this.name = name;
        }
    }
}
