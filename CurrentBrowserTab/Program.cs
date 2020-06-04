using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace CurrentBrowserTab
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var strUrl in Chrome.CheckChrome("Google Chrome"))
            {
                Console.WriteLine(strUrl);
               
            }

            Console.ReadKey();
        }
    
    }

}
