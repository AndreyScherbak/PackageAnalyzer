using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalyzer.Data
{
    public class AssemblyData
    {
        private string _assemblyName;
        public string AssemblyName
        {
            get { return _assemblyName; }
            set
            {
                if (_assemblyName != value)
                {
                    _assemblyName = value;
                    OnPropertyChanged(nameof(AssemblyName));
                }
            }
        }

        private object _assemblyVersion;
        public object AssemblyVersion
        {
            get { return _assemblyVersion; }
            set
            {
                if (_assemblyVersion != value)
                {
                    _assemblyVersion = value;
                    OnPropertyChanged(nameof(AssemblyVersion));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
