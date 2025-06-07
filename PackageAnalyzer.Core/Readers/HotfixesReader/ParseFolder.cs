using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HFList
{
    class ParseFolder : ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            var files = Directory.GetFiles(name);
            var asmInfo = files.Where(x => x.Contains("AssemblyInfo.xml")).Select(x => x).FirstOrDefault();
            //Try to find Assembly.xml file in directory:
            if (!string.IsNullOrEmpty(asmInfo))
            {
                filename = asmInfo;
                return new ParseXml().GetHotfixes(asmInfo);
            }
            //Try to find bin folder in directory:
            coll= new ParseBin().GetHotfixes(name);
            if (coll.Count != 0)
            {
                filename = name;
                return coll;
            }
            //Try to find Logs folder in directory and get the latest log file, it should be .txt file or .txt.{number} file (e.g. .txt.1):
            ParseData parser = new ParseTxt();
            var logs = files.Where(x => x.EndsWith(".txt") || new Regex(@"\.txt\.\d+$").IsMatch(x)).ToList<string>();
            if (logs.Count > 0)
            {
                logs.Sort((x, y) => y.CompareTo(x));
                foreach (var log in logs)
                {
                    var result = parser.GetHotfixes(log);
                    if (result.Count > 0)
                    {
                        filename = log;
                        return result;
                    }
                }
            }
            //Recursion
            var directories = Directory.GetDirectories(name);
            if (directories.Length > 0)
            {
                foreach (var directory in directories)
                {
                    coll = this.GetHotfixes(directory);
                    if (coll.Count > 0)
                    {
                        return coll;
                    }
                }
            }
            //If no hotfixes were found, then unpack .zip and .update files and perform search in unpacked directories
            if (coll.Count == 0)
            {
                var updateFiles = Directory.GetFiles(name).Where(x => x.Contains(".update")).Select(x => x);
                foreach (var updateFile in updateFiles)
                {
                    File.Move(updateFile, Path.ChangeExtension(updateFile, ".zip"));
                }
                var zipFiles = Directory.GetFiles(name).Where(x => x.Contains(".zip")).Select(x => x);
                foreach (var zipFile in zipFiles)
                {
                    ParseData parseZip = new ParseZip();
                    coll = parseZip.GetHotfixes(zipFile);
                    if (coll.Count > 0)
                    {
                        filename = parseZip.filename;
                        return coll;
                    }
                }
            }
            return coll;
        }
    }
}
