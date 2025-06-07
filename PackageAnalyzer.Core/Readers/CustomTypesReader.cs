using System.Xml;
using System.Xml.Linq;

namespace PackageAnalyzer.Core.Readers
{
    public class CustomTypesReader
    {
        static string[] ExcludeAssembliesThatStartFrom = new PackageAnalyzer.Configuration.PackageAnalyzerConfiguration().GetSection("ExcludeAssembliesThatStartFrom").ToArray();
        public string GetCustomTypes(string filePath)
        {
            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                // Select the root node using the XPath expression
                XmlNode rootNode = xmlDoc.SelectSingleNode("/*");
                XmlNodeList allNodes = rootNode.ChildNodes;

                CheckNode(allNodes);
                return XDocument.Parse(xmlDoc.InnerXml).ToString();
            }
            return null;
        }

        private void CheckNode(XmlNodeList nodeList)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                var nodeWithoutAttributesAndChildren = false;
                var nodeWithoutTypeAttributesAndChildren = false;
                var nodeTypeIsNotOverwritten = false;
                if (node.ChildNodes.Count != 0) { CheckNode(node.ChildNodes); }
                if ((node.Attributes == null || node.Attributes.Count == 0) && node.ChildNodes.Count == 0)
                {
                    nodeWithoutAttributesAndChildren = true;
                }
                else if (node.Attributes != null && node.Attributes.Count > 0)
                {
                    if (node.Attributes["type"] == null && node.ChildNodes.Count == 0)
                    {
                        nodeWithoutTypeAttributesAndChildren = true;
                    }
                    else if (node.Attributes["type"] != null)
                    {
                        var attribute = node.Attributes["type"].Value.ToLower();
                        if ((Array.Exists(ExcludeAssembliesThatStartFrom, prefix => attribute.StartsWith(prefix)) || !attribute.Contains(",") || attribute.EndsWith("sitecore.kernel") || attribute.EndsWith(", spe")) && node.ChildNodes.Count == 0)
                        {
                            nodeTypeIsNotOverwritten = true;
                        }
                    }
                }
                if (nodeWithoutAttributesAndChildren || nodeWithoutTypeAttributesAndChildren || nodeTypeIsNotOverwritten)
                {
                    node.ParentNode.RemoveChild(node);
                    i--;
                }
            }
        }

    }
}
