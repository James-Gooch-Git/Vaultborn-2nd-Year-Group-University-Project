using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;

namespace AssetManager.Core;

public class FusionManager
{
    public static readonly string parentPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));
    //AssetManager

    public static void InitializePythonEngine()
    {
        string pythonPath = Path.GetFullPath(Path.Combine(parentPath, "PythonEmbedded"));
        string pythonDll = Path.Combine(pythonPath, "python311.dll");

        Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath + "\\Lib", EnvironmentVariableTarget.Process);
        
        // Console.WriteLine($"parentPath: {parentPath}");
         Console.WriteLine("pythonPath: " + pythonPath);
        // Console.WriteLine("pythonDll: " + pythonDll);
        Console.WriteLine("PYTHONPATH EV: " + pythonPath + "\\Lib");

        Runtime.PythonDLL = pythonDll;
        
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = @"C:\Users\tomgr\source\repos\AssetManager\PythonEmbedded\python.exe";
        psi.Arguments = "FusionAddIn.py";  // Make sure the script path is correct!
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        Process p = Process.Start(psi);

        try
        {
            PythonEngine.Initialize();
            //Console.WriteLine($"Python.NET Initialized! Using Python {PythonEngine.Version}");

            using (Py.GIL())
            {
                string scriptDir = parentPath + "\\AssetManager.Core\\Fusion";
                dynamic sys = Py.Import("sys");
                sys.path.append(scriptDir);
                    
                //Console.WriteLine($"Added {scriptDir} to sys.path");
                Console.WriteLine("Python Executable: " + sys.executable);
                Console.WriteLine("Python Version: " + sys.version);
                Console.WriteLine("sys.path: " + string.Join(";", sys.path));
                sys.path.insert(0, @"C:\Users\tomgr\source\repos\AssetManager\PythonEmbedded\Lib");
                PythonEngine.Exec("import _thread");

                try
                {
                    dynamic adsk = Py.Import("adsk");
                    Console.WriteLine("adsk module loaded successfully from: " + adsk.__file__);
                }
                catch (PythonException ex)
                {
                    Console.WriteLine("Failed to import adsk: " + ex.Message);
                }
                
                dynamic pyModule = Py.Import("FusionAddIn"); // Import testscript.py
                dynamic hwFunction = pyModule.run;
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
                
                dynamic pyModule = Py.Import("FusionAddIn"); // Import FusionAddIn.py
                dynamic runFunction = pyModule.run();
                string result = runFunction();
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