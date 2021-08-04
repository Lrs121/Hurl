using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace skim
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(RuntimeInformation.OSDescription);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("OneDrive");
                        Console.WriteLine(o.ToString());
                    }
                }
        }
    }
}
