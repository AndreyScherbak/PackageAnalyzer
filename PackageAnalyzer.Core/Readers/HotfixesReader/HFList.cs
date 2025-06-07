using HFList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalyzer.Core.Readers.HotfixesReader
{
    class HFList
    {
        class HotfixList
        {
            static ParseData parseData;
            public static void GetData(string name, Data data)
            {
                try
                {
                   var extention = Path.GetExtension(name);

                    if (extention.ToLower() == ".xml")
                        parseData = new ParseXml();
                    else if (name.Contains(".txt"))
                        parseData = new ParseTxt();
                    else if (name.Contains(".dll"))
                        parseData = new ParseDll();
                    else if (name.Contains(".zip"))
                        parseData = new ParseZip();
                    else if (name.Length > 0 && Directory.Exists(name))
                    {
                        parseData = new ParseFolder();
                    }
                    else
                    {
                        data.filename = name;
                       // FileSystemLogger.Log.Warn($"[HotfixChecker]: The file with the '{extention}' extention is not supported. File name: {name}.");
                        return;
                    }
                    var result = parseData.GetHotfixes(name);
                    data.filename = parseData.filename;
                    data.hotfixes = GetString(result);
                    data.assemblies = GetList(result);

                    //FileSystemLogger.Log.Info($"[HotfixChecker]: The {data.filename} file or directory was resolved to retrieve hotfixes. The following file or directory was opened: {name}");

                    if (!string.IsNullOrWhiteSpace(data.hotfixes))
                    {
                        foreach (var x in result)
                        {
                            data.table.Rows.Add(x.Key, x.Value);
                        }
                    }

                }
                catch (Exception e)
                {
                    //FileSystemLogger.Log.Error("[HotfixChecker]: " + e.Message + Environment.NewLine + e.StackTrace);
                    throw e;
                }
            }
            static string GetString(Dictionary<string, string> coll)
            {
                var list = new SortedSet<string>(coll.Values);
                var result = new StringBuilder();
                foreach (var row in list)
                {
                    result.Append(row).Append(", ");
                }
                if (result.Length > 1)
                    result.Length -= 2;
                return result.ToString();
            }
            static string GetList(Dictionary<string, string> coll)
            {
                var result = new StringBuilder();
                foreach (var x in coll)
                {
                    result.Append(string.Format("{0}{1}{2}", x.Key.PadRight(70), x.Value, Environment.NewLine));
                }
                return result.ToString();
            }
        }
    }
}
