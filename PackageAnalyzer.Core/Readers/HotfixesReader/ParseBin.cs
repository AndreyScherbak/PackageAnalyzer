using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFList
{
    class ParseBin : ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            var files = Directory.GetFiles(name);
            foreach (var fileName in files)
            {
                if (Path.GetExtension(fileName).ToLower() == ".dll")
                {
                    var libraryVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
                    if (libraryVersionInfo.ProductVersion != null && libraryVersionInfo.FileDescription.ToLower().StartsWith("sitecore") && libraryVersionInfo.ProductVersion.ToLower().Contains("hotfix"))
                    {
                        coll.Add(libraryVersionInfo.FileDescription, libraryVersionInfo.ProductVersion.GetHotfixNumber());
                    }
                }
            }
            filename = name;
            return coll;
        }
    }
}
