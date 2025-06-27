using System.Text;
using System.Xml.Linq;
using System.Xml;
using PackageAnalyzer.Configuration;

public class SettingsReader
{
    public Dictionary<string, string> ReadAllXmlFiles(string folderPath)
    {
        var xmlContents = new Dictionary<string, string>();

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
        }

        // Get the list of allowed XML files from the configuration
        var config = new PackageAnalyzerConfiguration();
        List<string> allowedFiles = config.GetSection("DefaultSettingsFiles");

        foreach (var fileName in allowedFiles)
        {
            string filePath = Path.Combine(folderPath, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    var xDocument = XDocument.Load(filePath);
                    string xmlContent = xDocument.ToString();
                    xmlContents.Add(fileName, xmlContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing the file '{filePath}': {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"File '{filePath}' not found in the specified folder.");
            }
        }

        return xmlContents;
    }

    public Dictionary<string, string> MatchDictionaryValuesWithXml(Dictionary<string, string> dictionary, XmlDocument xmlDoc)
    {
        Dictionary<string, string> resultDictionary = new Dictionary<string, string>();

        foreach (var kvp in dictionary)
        {
            List<string> dictValue = GetAllSettingNames(kvp.Value);
            List<string> updatedDictValue = new List<string>();

            foreach (string key in dictValue)
            {
                string updatedValueFromShowconfig;
                string patchSourceValue;

                updatedValueFromShowconfig = GetSettingByNameFromShowconfig(xmlDoc, key, out patchSourceValue);

                if (!string.IsNullOrEmpty(patchSourceValue))
                {
                    updatedDictValue.Add($"<setting name=\"{key}\" value=\"{updatedValueFromShowconfig}\" patch:source=\"{patchSourceValue}\" />");
                }
                else
                {
                    updatedDictValue.Add($"<setting name=\"{key}\" value=\"{updatedValueFromShowconfig}\" />");
                }
            }

            string resultDictionaryValue = ListToString(updatedDictValue);
            resultDictionary.Add(kvp.Key, resultDictionaryValue);
        }

        return resultDictionary;
    }

    public static string GetSettingByNameFromShowconfig(XmlDocument xmlDoc, string settingName, out string patchSource)
    {
        string xpathQuery = $"//sitecore/settings/setting[@name='{settingName}']";
        XmlNode settingNode = xmlDoc.SelectSingleNode(xpathQuery);
        patchSource = null;

        if (settingNode != null)
        {
            XmlAttribute valueAttribute = settingNode.Attributes["value"];
            XmlAttribute patchSourceAttribute = settingNode.Attributes["patch:source"];

            if (patchSourceAttribute != null)
            {
                patchSource = patchSourceAttribute.Value;
            }

            if (valueAttribute != null)
            {
                return valueAttribute.Value;
            }
        }
        return "Setting not found";
    }

    public static List<string> GetAllSettingNames(string xmlContent)
    {
        List<string> settingNames = new List<string>();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        XmlNodeList settingNodes = xmlDoc.SelectNodes("//setting");
        if (settingNodes != null)
        {
            foreach (XmlNode node in settingNodes)
            {
                XmlAttribute nameAttribute = node.Attributes["name"];
                if (nameAttribute != null)
                {
                    settingNames.Add(nameAttribute.Value);
                }
            }
        }
        return settingNames;
    }

    public static string ListToString(List<string> list)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string item in list)
        {
            sb.AppendLine(item);
        }
        return sb.ToString();
    }
}