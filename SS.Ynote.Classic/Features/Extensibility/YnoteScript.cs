using CSScriptLibrary;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SS.Ynote.Classic.Features.Extensibility
{
    public static class YnoteScript
    {
        static string[] GetReferences()
        {
            return new[]
            {
                Assembly.GetExecutingAssembly().FullName,
                Application.StartupPath + @"\FastColoredTextBox.dll",
                Application.StartupPath + @"\WeifenLuo.WinFormsUI.Docking"
            };
        }

        public static void RunScript(IYnote ynote, string ysfile)
        {
            try
            {
                Assembly assembly;
                string assemblyFileName = ysfile + ".cache";
                CSScript.CacheEnabled = true;
                CSScript.GlobalSettings.TargetFramework = "v3.5";
               // var helper =
               //     new AsmHelper(CSScript.LoadMethod(File.ReadAllText(ysfile), GetReferences()));
               // helper.Invoke("*.Run", ynote);
                assembly = !File.Exists(assemblyFileName) ? CSScript.LoadMethod(File.ReadAllText(ysfile),assemblyFileName, false, GetReferences()) : Assembly.LoadFrom(assemblyFileName);
                var execManager = new AsmHelper(assembly);
                execManager.Invoke("*.Main", ynote);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an Error running the script : \r\n" + ex.Message, "YnoteScript Host", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}