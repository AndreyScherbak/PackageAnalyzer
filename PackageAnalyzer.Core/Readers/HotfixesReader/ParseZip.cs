using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;

namespace HFList
{
    class ParseZip : ParseData
    {
        public override Dictionary<string, string> GetHotfixes(string name)
        {
            var originFileName = Path.GetFileNameWithoutExtension(name);
            var extractedFileName = "_";
            var extractTo = Path.GetDirectoryName(name) + "\\" + extractedFileName;
            var createdDirectory = extractTo;
            if (Directory.Exists(createdDirectory))
            {
                createdDirectory = Path.GetDirectoryName(name) + "\\" + DateTime.Now.Second.ToString();
                extractTo = createdDirectory + "\\" + extractedFileName;
            }
            //Save real names of directories to show where the file is located
            var savedFilePath = new Dictionary<int, string>();
            savedFilePath.Add(extractTo.Split('\\').Length - 1, originFileName);
            ZipFile.ExtractToDirectory(name, extractTo);
            ParseData parseData = new ParseFolder();
            var result = parseData.GetHotfixes(extractTo);
            Directory.Delete(createdDirectory, true);
            if (!string.IsNullOrEmpty(parseData.filename))
            {
                var restoredFilePath = parseData.filename.Split('\\');
                foreach (var x in savedFilePath)
                {
                    restoredFilePath[x.Key] = x.Value;
                }
                filename = String.Join("\\", restoredFilePath);
            }
            return result;
        }
    }
}
