using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PackageAnalyzer.Core.FileSystem
{
    /// <summary>
    /// The File Manager
    /// </summary>
    public class FileManager
    {
        public string _filePath;
        public IFileProvider _fileProvider;

        /// <summary>
        /// Initializes a new instance of the FileManager
        /// </summary>
        /// <param name="filePath">path to package folder</param>
        public FileManager(string filePath)
        {
            _filePath = filePath;
        }
        /// <summary>
        /// Find files in package folder
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns></returns>
        public List<FileInfo> FindFiles(string fileName, bool returnFirst = false)
        {
            try
            {
                IFileProvider _fileProvider = null;
                if (IsArchive(Path.GetExtension(_filePath)))
                {
                    _fileProvider = new ArchiveProvider();
                }
                else
                {
                    _fileProvider = new DirectoryProvider();
                }

                return _fileProvider.FindFile(_filePath, fileName, returnFirst);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string FindFolder(string fileName)
        {
            try
            {
                IFileProvider _fileProvider = null;
                if (IsArchive(Path.GetExtension(_filePath)))
                {
                    _fileProvider = new ArchiveProvider();
                }
                else
                {
                    _fileProvider = new DirectoryProvider();
                }

                return _fileProvider.FindFolder(_filePath, fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// Read xml value from file
        /// </summary>
        /// <param name="xmlFilePath">Path to xml file</param>
        /// <param name="nodeName">Name of the node</param>
        /// <returns>Inner text of the node</returns>
        public static string ReadXmlValue(string xmlFilePath, string nodeName)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                // Select the node with the specified name
                XmlNode node = xmlDoc.SelectSingleNode($"//{nodeName}");

                // Check if the node is found
                if (node != null)
                {
                    return node.InnerText;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {

                return null;
            }
        }

        public static Dictionary<string, string> ReadXmlPairValues(string xmlFilePath, string parentNodeName, string childNodeName)
        {
            Dictionary<string, string> pairValues = new Dictionary<string, string>();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);
                XmlNodeList parentNodes = xmlDoc.SelectNodes("//" + parentNodeName);
                
                if (parentNodes != null)
                {
                    foreach (XmlNode parentNode in parentNodes)
                    {
                        string parentNameValue = parentNode.Attributes["name"].Value;
                        string childNodeValue = parentNode.SelectSingleNode(childNodeName).InnerText;

                        if (!pairValues.ContainsKey(parentNameValue))
                        {
                            pairValues.Add(parentNameValue, childNodeValue);
                        }
                    }
                }
                else
                {
                    throw new Exception("No such nodes found in the XML.");
                }
                return pairValues;

            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while parsing XML.", ex);
            }
        }

        public bool IsArchive(string extension)
        {
            return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".7z", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".rar", StringComparison.OrdinalIgnoreCase);
        }
    }
}
