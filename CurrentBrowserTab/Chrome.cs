using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;
//using System.Windows.Forms;

namespace CurrentBrowserTab
{
    class Chrome
    {

        [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr windowHandle, StringBuilder stringBuilder, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            List<IntPtr> pointers = GCHandle.FromIntPtr(pointer).Target as List<IntPtr>;
            pointers.Add(handle);
            return true;
        }

        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(Win32Callback enumProc, IntPtr lParam);

        public static List<string> CheckChrome(string localApp)
        {
            List<string> urlStrings = new List<string>();
            string returnString = string.Empty;
            string title = string.Empty;
            List<IntPtr> Windows = GetAllWindows();

            foreach (IntPtr window in Windows)
            {
                title = GetTitle(window);
                if (title.Contains(localApp))
                {
                    if (InspectChromeObject(window, localApp, ref returnString))
                    {
                        urlStrings.Add(returnString);
                    }
                }
            }

            return urlStrings;

        }

        private static List<IntPtr> GetAllWindows()
        {
            Win32Callback enumCallback = new Win32Callback(EnumWindow);
            List<IntPtr> pointers = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(pointers);
            try
            {
                EnumWindows(enumCallback, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated) listHandle.Free();
            }
            return pointers;
        }
        public static string GetTitle(IntPtr handle)
        {
            int length = GetWindowTextLength(handle);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(handle, sb, sb.Capacity);
            return sb.ToString();
        }

        public static List<AutomationElement> GetEditElement(AutomationElement rootElement, List<AutomationElement> ret)
        {

            Condition isControlElementProperty = new PropertyCondition(AutomationElement.IsControlElementProperty, true);
            Condition isEnabledProperty = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
            TreeWalker walker = new TreeWalker(new AndCondition(isControlElementProperty, isEnabledProperty));
            AutomationElement elementNode = walker.GetFirstChild(rootElement);
            while (elementNode != null)
            {
                if (elementNode.Current.ControlType.ProgrammaticName == "ControlType.Edit")
                    ret.Add(elementNode);
                GetEditElement(elementNode, ret);
                elementNode = walker.GetNextSibling(elementNode);
            }
            return ret;
        }
        private static bool InspectChromeObject(IntPtr handle, string localApp, ref string urlString)
        {
            AutomationElement elm = AutomationElement.FromHandle(handle);

            AutomationElement elmUrlBar = null;
            try
            {
                var elm1 = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, localApp));
                if (elm1 == null) { return false; }

                List<AutomationElement> ret = new List<AutomationElement>();
                elmUrlBar = GetEditElement(elm1, ret)[0];
            }
            catch
            {
                return false;
            }

            if (elmUrlBar == null)
            {
                return false;
            }

            if ((bool)elmUrlBar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
            {
                return false;
            }
            AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
            if (patterns.Length == 1)
            {
                string ret = "";
                try
                {
                    ret = ((ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0])).Current.Value;
                }
                catch { }
                if (ret != "")
                {
                    if (Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                    {
                        if (!ret.StartsWith("http"))
                        {
                            ret = "http://" + ret;
                        }

                        urlString = ret;

                    }

                    return true;
                }

            }

            return false;

        }

    }
}