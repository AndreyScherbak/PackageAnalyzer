using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HFList
{
    class Data
    {
        public string hotfixes { get; set; }
        public string assemblies { get; set; }
        public string filename { get; set; }
        public DataTable table = new DataTable();

        public Data()
        {
            table.Columns.Add("Name");
            table.Columns.Add("Hotfix");
        }
        
    }
}
