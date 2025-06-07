using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HFList
{

    class ParseXml:ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            using (var sr = new StreamReader(name))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sr.ReadToEnd());             
                var str = "";
                foreach (XmlNode row in doc.SelectNodes("assemblies/assembly"))
                {
                    XmlNode row2 = row.SelectSingleNode(".//productVersion");
                    str = row2.InnerText.ToLower();
                    if (!string.IsNullOrWhiteSpace(str) && str.Contains("hotfix"))
                    {
                        var key = row.Attributes["name"].Value;
                        if (key.ToLower().StartsWith("sitecore") && !coll.ContainsKey(key))
                        {
                            coll.Add(key, str.GetHotfixNumber());
                        }
                    }
                }
            }
            filename = name;
            return coll;
        }      
    }
}
