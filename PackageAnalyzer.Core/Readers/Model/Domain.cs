using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalyzer.Core.Readers.Model
{
    public class Domain
    {
        private Dictionary<string,string> _siteDomains;
        public Dictionary<string, string> siteDomains
        {
            get { return _siteDomains; }
            set
            {
                if (_siteDomains != value)
                {
                    _siteDomains = value;
                }
            }
        }
       
        private string _role;
        public string role
        {
            get { return _role; }
            set
            {
                if (_role != value)
                {
                    _role = value;
                }
            }
        }
        public Domain(string role)
        {
            _siteDomains = new Dictionary<string,string>();
            _role=role;
        }
    }
}
