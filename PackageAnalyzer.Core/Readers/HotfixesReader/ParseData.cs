using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFList
{
    abstract class ParseData
    {
        public Dictionary<string, string> coll=new Dictionary<string, string>();
        public string filename="";
        public abstract Dictionary<string, string> GetHotfixes(string name);

    }
}
