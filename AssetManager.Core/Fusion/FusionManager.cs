using Python.Runtime;
using System;
using System.IO;

namespace AssetManager.Core;

public class FusionManager
{
    public static readonly string parentPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));

    public static void InitializePythonEngine()
    {
        string pythonPath = Path.GetFullPath(Path.Combine(parentPath, "PythonEmbedded"));
        string pythonDll = Path.Combine(pythonPath, "python311.dll");

        Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
        Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath + "\\Lib");
        
        // Console.WriteLine($"parentPath: {parentPath}");
        // Console.WriteLine("pythonPath: " + pythonPath);
        // Console.WriteLine("pythonDll: " + pythonDll);
        // Console.WriteLine("PYTHONPATH: " + pythonPath + "\\Lib");

        Runtime.PythonDLL = pythonDll;

        try
        {
            PythonEngine.Initialize();
            //Console.WriteLine($"Python.NET Initialized! Using Python {PythonEngine.Version}");

            using (Py.GIL())
            {
                string scriptDir = parentPath + "\\AssetManager.core\\Fusion";
                dynamic sys = Py.Import("sys");
                sys.path.append(scriptDir);
                //Console.WriteLine($"Added {scriptDir} to sys.path");
                
                dynamic pyModule = Py.Import("testscript"); // Import testscript.py
                dynamic hwFunction = pyModule.hw;
                string result = hwFunction();
                Console.WriteLine($"Python script: {result}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Python Initialisation Failed: {e.Message}");
        } 
        finally 
        {
            PythonEngine.Shutdown();
        }
    }

    public void RunViewer()
    {
        try
        {
            PythonEngine.Initialize();
            //Console.WriteLine($"Python.NET Initialized! Using Python {PythonEngine.Version}");

            using (Py.GIL())
            {
                string scriptDir = parentPath + "\\AssetManager.core\\Fusion";
                dynamic sys = Py.Import("sys");
                sys.path.append(scriptDir);
                //Console.WriteLine($"Added {scriptDir} to sys.path");
                
                dynamic pyModule = Py.Import("testscript"); // Import testscript.py
                dynamic hwFunction = pyModule.hw;
                string result = hwFunction();
                Console.WriteLine($"Python script: {result}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Python Initialisation Failed: {e.Message}");
        } 
        finally 
        {
            PythonEngine.Shutdown();
        }
    }
}