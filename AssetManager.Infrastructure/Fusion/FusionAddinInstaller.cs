using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace AssetManagement.Infrastructure.Fusion
{
    public class FusionAddinInstaller
    {
        public static void InstallFusionAddin(string accessToken = null)
        {
            try
            {
                string tempAddinPath = Path.Combine(Path.GetTempPath(), "SaveToHub2");
                if (Directory.Exists(tempAddinPath))
                {
                    Directory.Delete(tempAddinPath, true);
                }
                Directory.CreateDirectory(tempAddinPath);

                // Copy add-in files
                CopyAddinFiles(tempAddinPath, accessToken);

                // Define user Add-ins folder
                string userAddinsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "Autodesk Fusion 360", "API", "AddIns"
                );

                CopyAddinToPath(tempAddinPath, userAddinsPath);
                InstallPythonRequests();


                // Clean up temp directory
                Directory.Delete(tempAddinPath, true);

                Console.WriteLine("✅ Fusion 360 Add-in installation complete. Please restart Fusion 360 for the add-in to be recognized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error installing Fusion 360 Add-in: {ex.Message}");
            }
        }

        private static void CopyAddinFiles(string addinPath, string accessToken)
        {
            // Define files and directories to create
            string[] files = {
        "SaveToHub2.manifest", "SaveToHub2.py", "config.py",
        "savetohub_palette.html", "AddInIcon.svg", ".env", "embedded_requests.py"
    };

            string[] directories = {
        ".vscode", "commands/commandDialog/resources", "commands/paletteSend/resources",
        "commands/paletteShow/resources/html/static", "lib/fusionAddInUtils", "requests"
    };

            string[] embeddedResources = {
        "commands/commandDialog/__init__.py", "commands/commandDialog/entry.py",
        "commands/commandDialog/resources/16x16.png", "commands/commandDialog/resources/32x32.png",
        "commands/paletteSend/__init__.py", "commands/paletteSend/entry.py",
        "commands/paletteShow/__init__.py", "commands/paletteShow/entry.py",
        "commands/paletteShow/resources/html/index.html", "commands/paletteShow/resources/html/static/palette.js",
        "lib/fusionAddInUtils/__init__.py", "lib/fusionAddInUtils/event_utils.py"
    };

            // Create directories
            foreach (string dir in directories)
            {
                Directory.CreateDirectory(Path.Combine(addinPath, dir));
            }

            // Copy embedded files
            foreach (string file in files)
            {
                CopyEmbeddedResource(file, Path.Combine(addinPath, file));
            }

            foreach (string resource in embeddedResources)
            {
                CopyEmbeddedResource(resource, Path.Combine(addinPath, resource));
            }

            // **Recursively copy the "packages" directory**
            string sourcePackagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages");
            string destinationPackagesPath = Path.Combine(addinPath, "packages");

            if (Directory.Exists(sourcePackagesPath))
            {
                CopyDirectory(sourcePackagesPath, destinationPackagesPath);
                Console.WriteLine($"✅ Copied 'packages' directory from: {sourcePackagesPath}");
            }
            else
            {
                Console.WriteLine($"⚠️ Warning: 'packages' directory not found at {sourcePackagesPath}");
            }

            // If accessToken is provided, save it
            if (!string.IsNullOrEmpty(accessToken))
            {
                File.WriteAllText(Path.Combine(addinPath, "auth_token.txt"), accessToken);
            }
            // Define the source and destination paths for the 'requests' directory
            string sourceRequestsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "requests");
            string destinationRequestsPath = Path.Combine(addinPath, "requests");

            // Check if the 'requests' directory exists and copy it
            if (Directory.Exists(sourceRequestsPath))
            {
                CopyDirectory(sourceRequestsPath, destinationRequestsPath);
                Console.WriteLine($"✅ Copied 'requests' directory from: {sourceRequestsPath}");
            }
            else
            {
                Console.WriteLine($"⚠️ Warning: 'requests' directory not found at {sourceRequestsPath}");
            }

        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string destinationFile = Path.Combine(destinationDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                File.Copy(file, destinationFile, true);
            }
        }


        private static void CopyEmbeddedResource(string resourceName, string destinationPath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string normalizedResourceName = "AssetManager.Infrastructure.Fusion.Resources." + resourceName.Replace("/", ".").Replace("\\", ".");

                using (Stream stream = assembly.GetManifestResourceStream(normalizedResourceName))
                {
                    if (stream == null)
                    {
                        Console.WriteLine($"⚠️ Warning: Resource '{resourceName}' not found. Looking for: {normalizedResourceName}");
                        return;
                    }

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error copying resource '{resourceName}': {ex.Message}");
            }
        }
        private static void CopyAddinToPath(string sourceDir, string destinationDir)
        {
            string targetPath = Path.Combine(destinationDir, "SaveToHub2");

            if (Directory.Exists(targetPath))
            {
                try
                {
                    Directory.Delete(targetPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning: Could not remove existing add-in: {ex.Message}");
                }
            }

            Directory.CreateDirectory(targetPath);

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string destinationFile = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                File.Copy(file, destinationFile, true);
            }

            Console.WriteLine($"✅ Add-in installed at: {targetPath}");

        }

        private static void InstallPythonRequests()
        {
            try
            {
                // Locate Fusion 360's Python installation directory
                string fusionPythonBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Autodesk", "webdeploy", "production"
                );

                // Find the correct Python folder inside Fusion 360
                string[] pythonDirs = Directory.GetDirectories(fusionPythonBasePath, "Python", SearchOption.AllDirectories);
                if (pythonDirs.Length == 0)
                {
                    Console.WriteLine("⚠️ Warning: Could not find Fusion 360’s Python environment.");
                    return;
                }

                // Get the Python executable path
                string pythonExePath = Path.Combine(pythonDirs[0], "python.exe");

                // 🔹 Step 1: Check if `requests` is already installed
                ProcessStartInfo checkInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = "-c \"import requests\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process checkProcess = new Process { StartInfo = checkInfo })
                {
                    checkProcess.Start();
                    checkProcess.WaitForExit();

                    if (checkProcess.ExitCode == 0)
                    {
                        Console.WriteLine("✅ 'requests' is already installed in Fusion 360’s Python environment.");
                        return;
                    }
                    Console.WriteLine("⚠️ 'requests' is missing. Installing in an isolated environment...");
                }

                // 🔹 Step 2: Install `requests` in Fusion 360’s local site-packages (not global)
                ProcessStartInfo installInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = "-m pip install requests --target \"Lib\\site-packages\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process installProcess = new Process { StartInfo = installInfo })
                {
                    installProcess.Start();
                    string output = installProcess.StandardOutput.ReadToEnd();
                    string error = installProcess.StandardError.ReadToEnd();
                    installProcess.WaitForExit();

                    Console.WriteLine($"✅ Pip Output: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"⚠️ Pip Error: {error}");
                    }
                }

                Console.WriteLine("✅ Successfully installed 'requests' in Fusion 360’s local site-packages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error installing 'requests' module: {ex.Message}");
            }
        }







    }
}
