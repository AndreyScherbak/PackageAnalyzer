using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PackageAnalyzer.Configuration
{
    public class PackageAnalyzerConfiguration
    {
        private readonly string _configFilePath;
        private Dictionary<string, string> _settings;
        private readonly Dictionary<string, List<string>> _configSections;

        public PackageAnalyzerConfiguration()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration/Configuration.xml");
            _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _configSections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            if (!File.Exists(_configFilePath))
                throw new FileNotFoundException("Configuration file not found", _configFilePath);

            XDocument doc = XDocument.Load(_configFilePath);
            _settings.Clear();
            _configSections.Clear();

            if (doc.Root == null) return;

            foreach (var section in doc.Root.Elements())
            {
                if (section.Name.LocalName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                {
                    _settings = section.Elements()
                        .Where(e => e.Attribute("name") != null && e.Attribute("value") != null)
                        .ToDictionary(
                            e => e.Attribute("name")!.Value,
                            e => e.Attribute("value")!.Value
                        );
                }
                else
                {
                    _configSections[section.Name.LocalName] = section.Elements()
                        .Select(e => e.Value)
                        .ToList();
                }
            }
        }
        public void SaveConfiguration()
        {
            XDocument doc = new XDocument(
                new XElement("PackageAnalyzerConfiguration",
                    new XElement("Settings",
                        _settings.Select(setting =>
                            new XElement("Setting",
                                new XAttribute("name", setting.Key),
                                new XAttribute("value", setting.Value)
                            )
                        )
                    ),
                    _configSections.Select(section =>
                        new XElement(section.Key,
                            section.Value.Select(value => new XElement("Item", value))
                        )
                    )
                )
            );

            doc.Save(_configFilePath);
        }
        public void AddToSection(string sectionName, string value)
        {
            if (!_configSections.ContainsKey(sectionName))
                _configSections[sectionName] = new List<string>();

            if (!_configSections[sectionName].Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                _configSections[sectionName].Add(value);
                SaveConfiguration();
            }
        }

        public void RemoveFromSection(string sectionName, string value)
        {
            if (_configSections.TryGetValue(sectionName, out var values))
            {
                if (values.Remove(value))
                {
                    SaveConfiguration();
                }
            }
        }
        public string GetSetting(string settingName)
        {
            return _settings.TryGetValue(settingName, out var value) ? value : null;
        }

        public List<string> GetSection(string sectionName)
        {
            return _configSections.TryGetValue(sectionName, out var values) ? new List<string>(values) : new List<string>();
        }

    }
}
