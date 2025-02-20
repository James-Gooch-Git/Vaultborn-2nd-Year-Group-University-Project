using System;
using System.IO;
using Python.Runtime;

namespace AssetManager.Core
{
    public class FusionManager
    {
        public static void InitializePythonEngine()
        {
            string pythonPath = @"C:\Users\james\Desktop\AssetManagerTom2\AssetManager\PythonEmbedded"; // Change if Python is installed elsewhere
            string pythonDll = Path.Combine(pythonPath, "python311.dll");

            if (!File.Exists(pythonDll))
            {
                Console.WriteLine($"❌ Python DLL Not Found: {pythonDll}");
                return;
            }

            Console.WriteLine($"✅ Setting Python Runtime DLL: {pythonDll}");

            Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);
            Environment.SetEnvironmentVariable("Path", pythonPath + ";" + Environment.GetEnvironmentVariable("Path"));

            try
            {
                Runtime.PythonDLL = pythonDll;
                PythonEngine.Initialize();
                Console.WriteLine("✅ Python runtime successfully initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Python runtime: {ex.Message}");
                return;
            }
        }

        public static void RunPythonScript()
        {
            using (Py.GIL())
            {
                dynamic pyModule = Py.Import("FusionAddIn");
                dynamic result = pyModule.run();
                Console.WriteLine($"Python script output: {result}");
            }
        }
        
        public static void DeployFusionAddIn()
        {
            string addInSource  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/..", "/..", "/..", "/..", "/..", "AssetManager.Core", "Fusion", "FusionAddIn", "FusionAddIn.py");
            Console.WriteLine("SCRIPT: "+addInSource );
            string fusionAddInsFolder = ("C:\\Users\\tomgr\\AppData\\Roaming\\Autodesk\\Autodesk Fusion 360\\API\\AddIns");
            Console.WriteLine(fusionAddInsFolder);
            string addInDestination = Path.Combine(fusionAddInsFolder, "FusionAddIn");
            
            try
            {
                // Ensure destination exists
                if (Directory.Exists(addInDestination))
                {
                    Directory.Delete(addInDestination, true);
                }
        
                CopyDirectory(addInSource, addInDestination);
                Console.WriteLine("✅ Add-In successfully deployed!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to deploy Add-In: {ex.Message}");
            }
        }
        
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

    }
}