using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("A4 Image Cutter")]
[assembly: AssemblyDescription("Interactive A4 image splitting, layout, and printing tool")]
[assembly: AssemblyCompany("ds2lie85")]
[assembly: AssemblyProduct("A4 Image Cutter")]
[assembly: AssemblyCopyright("Copyright © 2026 ds2lie85")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: ComVisible(false)]

namespace A4ImageCutter
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
