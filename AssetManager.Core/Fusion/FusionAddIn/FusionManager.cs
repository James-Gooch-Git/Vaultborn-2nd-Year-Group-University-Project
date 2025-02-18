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
    }
}