using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HFList
{
    class ParseTxt : ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            var regex = new Regex(@".*\\bin\\.*\.dll.*hotfix");
            using (var sr = new StreamReader(name))
            {
                bool start = false;
                while (sr.Peek() >= 0)
                {
                    var str = sr.ReadLine().ToLower();
                    if (!string.IsNullOrEmpty(str))
                    {
                        if (start && str.Contains("\\bin\\") && str.Contains("hotfix"))
                        {
                            if (regex.IsMatch(str))
                            {
                                var assemblyName = str.Substring(str.IndexOf(@"\bin\")+5);
                                assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(' '));
                                if (assemblyName.ToLower().StartsWith("sitecore") && !coll.ContainsKey(assemblyName))
                                {
                                    coll.Add(assemblyName, str.GetHotfixNumber());
                                }
                            }
                        }
                        else if (str.Contains("info  sitecore started"))
                            start = true;
                    }
                }
            }
            filename = name;
            return coll;
        }
    }
}
