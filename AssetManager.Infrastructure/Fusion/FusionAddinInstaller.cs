using System;
using System.IO;
using System.Reflection;

namespace AssetManagement.Infrastructure.Fusion
{
    public class FusionAddinInstaller
    {
        public static void InstallFusionAddin(string accessToken = null)
        {
            try
            {
                // Create temporary directory for preparing the add-in
                string tempAddinPath = Path.Combine(Path.GetTempPath(), "SaveToHub");
                if (Directory.Exists(tempAddinPath))
                {
                    Directory.Delete(tempAddinPath, true);
                }
                Directory.CreateDirectory(tempAddinPath);

                // Create Python files in the temporary directory
                CreatePythonFiles(tempAddinPath, accessToken);

                // 🔹 1️⃣ Copy to USER Add-ins folder (API\AddIns)
                string userAddinsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "Autodesk Fusion 360", "API", "AddIns"
                );

                CopyAddinToPath(tempAddinPath, userAddinsPath);

                // 🔹 2️⃣ Copy to SYSTEM Add-ins folder (webdeploy\InternalAddins)
               /* string systemAddinsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Autodesk", "webdeploy", "production"
                );

                if (Directory.Exists(systemAddinsPath))
                {
                    foreach (var folder in Directory.GetDirectories(systemAddinsPath))
                    {
                        // Look for latest Fusion 360 installation
                        if (Path.GetFileName(folder).StartsWith("0"))
                        {
                            string internalAddinsPath = Path.Combine(folder, "Api", "InternalAddins");
                            if (!Directory.Exists(internalAddinsPath))
                            {
                                Directory.CreateDirectory(internalAddinsPath);
                            }
                            CopyAddinToPath(tempAddinPath, internalAddinsPath);
                            break;
                        }
                    }
                }*/

                // Clean up temp directory
                Directory.Delete(tempAddinPath, true);

                Console.WriteLine("✅ Fusion 360 Add-in installation complete.");
                Console.WriteLine("Please restart Fusion 360 for the add-in to be recognized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error installing Fusion 360 Add-in: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void CreatePythonFiles(string addinPath, string accessToken)
        {
            // Create the manifest file
            string manifestContent = @"{
    ""autodeskProduct"": ""Fusion360"",
    ""type"": ""addin"",
    ""id"": ""com.yourcompany.savetohub"",
    ""author"": ""Your Company"",
    ""description"": {
        """": ""Save Fusion 360 files directly to Autodesk Hub""
    },
    ""version"": ""1.0"",
    ""runOnStartup"": true,
    ""supportedOS"": ""windows|mac"",
    ""editEnabled"": true
}";
            File.WriteAllText(Path.Combine(addinPath, "SaveToHub.manifest"), manifestContent);

            // Create empty __init__.py file
            File.WriteAllText(Path.Combine(addinPath, "__init__.py"), "");

            // If accessToken is provided, save it to a token file
            if (!string.IsNullOrEmpty(accessToken))
            {
                File.WriteAllText(Path.Combine(addinPath, "auth_token.txt"), accessToken);
            }

            // Extract the Python script from embedded resources
            string saveToHubPyContent = GetEmbeddedResourceContent("AssetManager.Infrastructure.Fusion.Resources.SaveToHub.py");

            // Write the SaveToHub.py file
            File.WriteAllText(Path.Combine(addinPath, "SaveToHub.py"), saveToHubPyContent);
        }

        /// <summary>
        /// Reads content from an embedded resource file
        /// </summary>
        private static string GetEmbeddedResourceContent(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // List all available resources for debugging
                        string[] resources = assembly.GetManifestResourceNames();
                        string availableResources = string.Join(", ", resources);
                        throw new InvalidOperationException(
                            $"Resource '{resourceName}' not found. Available resources: {availableResources}");
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read embedded resource '{resourceName}': {ex.Message}", ex);
            }
        }

        private static void CopyAddinToPath(string sourceDir, string destinationDir)
        {
            string targetPath = Path.Combine(destinationDir, "SaveToHub");

            // Remove existing add-in if it exists
            if (Directory.Exists(targetPath))
            {
                try
                {
                    Directory.Delete(targetPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning: Could not remove existing add-in: {ex.Message}");
                    // Continue with installation anyway
                }
            }

            // Create the target directory
            Directory.CreateDirectory(targetPath);

            // Copy all files from source to destination
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFile = Path.Combine(targetPath, fileName);
                File.Copy(filePath, destFile, true);
            }

            // Copy resources directory if it exists
            string sourceResourcesDir = Path.Combine(sourceDir, "resources");
            if (Directory.Exists(sourceResourcesDir))
            {
                string targetResourcesDir = Path.Combine(targetPath, "resources");
                if (!Directory.Exists(targetResourcesDir))
                {
                    Directory.CreateDirectory(targetResourcesDir);
                }

                foreach (string filePath in Directory.GetFiles(sourceResourcesDir))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFile = Path.Combine(targetResourcesDir, fileName);
                    File.Copy(filePath, destFile, true);
                }
            }

            Console.WriteLine($"✅ Fusion 360 Add-in installed at: {targetPath}");
        }
    }
}