using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFList
{
    class ParseDll : ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            var libraryVersionInfo = FileVersionInfo.GetVersionInfo(name);
            if (libraryVersionInfo.ProductVersion != null && libraryVersionInfo.FileDescription.ToLower().StartsWith("sitecore") && libraryVersionInfo.ProductVersion.ToLower().Contains("hotfix"))
            {
                coll.Add(libraryVersionInfo.FileDescription, libraryVersionInfo.ProductVersion.GetHotfixNumber());
            }
            filename = name;
            return coll;
        }
    }
}
