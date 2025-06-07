using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using SharpCompress.Common;

namespace PackageAnalyzer.Data
{
    public class ValueTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SitecoreData sitecoreData)
            {
                var value = sitecoreData.Value;
                if (value is List<AssemblyData>)
                {
                    return (DataTemplate)((FrameworkElement)container).FindResource("ExpanderTemplate");
                }
            }

            return (DataTemplate)((FrameworkElement)container).FindResource("TextTemplate");
        }

    }
}
