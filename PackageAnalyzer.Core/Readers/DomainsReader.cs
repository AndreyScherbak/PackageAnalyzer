using PackageAnalyzer.Core.FileSystem;
using PackageAnalyzer.Core.Readers.Model;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PackageAnalyzer.Core.Readers
{
    public class SiteDomainsReader
    {
        public List<Domain> domainsCollection = new List<Domain>();
        public SiteDomainsReader()
        {

        }
        /// <summary>
        /// Read all domains from sites nodes
        /// </summary>
        /// <param name="filepath">Path to package directory</param>
        /// <returns>List of PackageAnalyzer.Core.Readers.Data.Domain</returns>
        public List<Domain> ReadDomains(string filepath)
        {
            string key = "role:define";
            var manager = new FileManager(filepath);
            var files = manager.FindFiles(Constants.webConfig);
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var role = WebConfigInfoReader.ReadAppSetting(file.DirectoryName + @"\\" + file.Name, key);

                    var domain = new Domain(role);
                    if (file.Directory.Name == "Configs")
                    {
                        var manager1 = new FileManager(file.Directory.ToString());

                        var showConfigFile = manager1.FindFiles(Constants.ShowConfigFile);
                        if (showConfigFile == null)
                        {
                            return null;
                        }
                        else
                        {

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.Load(showConfigFile.FirstOrDefault().OpenRead());

                            // Get the "sites" node
                            XmlNode sitesNode = xmlDoc.SelectSingleNode("/sitecore/sites");

                            if (sitesNode != null)
                            {
                                // Loop through each "site" node
                                foreach (XmlNode siteNode in sitesNode.ChildNodes)
                                {
                                    // Check if the node is an element node
                                    if (siteNode.NodeType == XmlNodeType.Element && siteNode.Name == "site")
                                    {
                                        // Read attributes from the "site" node
                                        XmlAttribute siteName = siteNode.Attributes["name"];
                                        XmlAttribute hostName = siteNode.Attributes["hostName"];
                                        XmlAttribute targetHostName = siteNode.Attributes["targetHostName"];
                                        if (siteName != null)
                                        {
                                            if (hostName != null && targetHostName != null)
                                            {
                                                domain.siteDomains.Add(siteName.Value, "hostname: " + hostName.Value + "; targetHostName: " + targetHostName.Value);
                                            }
                                            else
                                            {
                                                if (hostName != null)
                                                {
                                                    domain.siteDomains.Add(siteName.Value, "hostname: " + hostName.Value);
                                                }
                                                if (targetHostName != null)
                                                {
                                                    domain.siteDomains.Add(siteName.Value, " targetHostName: " + targetHostName.Value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            domainsCollection.Add(domain);
                        }
                    }
                }
            }
            return domainsCollection;
        }

    }
}
