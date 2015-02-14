using System;
using System.EnterpriseServices.Internal;
using System.IO;
using System.Linq;

namespace PsspInstallUtils
{
    class Program
    {
        /// <summary>
        /// Seznam knihoven, které se musí zaregistrovat do GAC
        /// </summary>
        private static readonly string[] DllArr = new string[] { "NLog.dll" };

        static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
                Console.WriteLine("Bad parameters! Please don't use this application directly.");
                return;
            }

            Gac(args[0], args[1]);
        }

        /// <summary>
        /// Provede registraci knihoven do GAC - nechce se mi v NSIS instalatoru řešit, kde je nainstalovaný .NET framework, 
        /// abych zavolal správný GACUTIL.EXE
        /// </summary>
        /// <param name="action">Očekává jeden parametr "i" - pro instalaci knihoven do GAC a "u" pro odinstalaci knihoven z GAC</param>
        /// <param name="path">Cesta k souborům knihoven</param>
        private static void Gac(string action, string path)
        {
            var p = new Publish();

            if (string.Equals(action, "i", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dll in DllArr)
                {
                    if (!File.Exists(Path.Combine(path, dll)))
                        Console.WriteLine("No such file: " + Path.Combine(path, dll));
                    else
                    {
                        p.GacInstall(Path.Combine(path, dll)); // for gac installation                                                    
                        Console.WriteLine("Libraries installed.");
                    }
                }
            }
            else if (string.Equals(action, "u", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dll in DllArr)
                {
                    if (!File.Exists(Path.Combine(path, dll)))
                        Console.WriteLine("No such file: " + Path.Combine(path, dll));
                    else
                    {
                        p.GacRemove(Path.Combine(path, dll)); // for gac removing
                        Console.WriteLine("Libraries uninstalled.");
                    }
                }
            }
            else
                Console.WriteLine("Bad parameter! Please don't use this application directly.");

            //p.RegisterAssembly(file); // for registering assembly for interop
            //p.UnRegisterAssembly(file); // to unregister assembly
        }
    }
}
