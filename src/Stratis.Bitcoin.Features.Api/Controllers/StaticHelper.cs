using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePayGUI
{
    
        public class clsStaticHelper
        {
            public static object myLock = "A";
            public static string msReadOnly = "background-color: black;";

            public static object ExtractXML(string sData, string sStartKey, string sEndKey)
            {
                int iPos1 = sData.IndexOf(sStartKey);
                iPos1 = iPos1 + sStartKey.Length;
                int iPos2 = sData.IndexOf(sEndKey, iPos1);
                if (iPos2 == 0) return "";
                string sOut = sData.Substring(iPos1, iPos2 - iPos1);
                return sOut;
            }
            public static double GetDouble(object o)
            {
                if (o == null) return 0;
                if (o.ToString() == "") return 0;
                double d = Convert.ToDouble(o.ToString());
                return d;
            }

        }
    
}

