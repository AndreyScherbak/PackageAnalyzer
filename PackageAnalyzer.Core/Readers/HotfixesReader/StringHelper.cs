using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFList
{
    public static class StringHelper
    {
        public static string GetHotfixNumber(this string str)
        {
            var idx = str.ToLower().IndexOf("hotfix") + 6;
            str = str.Substring(idx, str.Length - idx).Replace(" ", "");
            var parts = str.Split('-');
            if (parts.Length > 1)
            {
                if (parts[1] == "2" || parts[1] == "3" || parts[1] == "4")
                    str = parts[0] + "-" + parts[1];
                else
                    str = parts[0];
            }
            return str;
        }
    }
}
