using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using AssetManager.Infrastructure.Services;
using ForgeViewerApp;
using System.Net.Http;
using Newtonsoft.Json;

namespace AssetManagement.Infrastructure.Services
{
    public class FolderService
    {
        private readonly string rootPath;
        private readonly ModelUpload modelUpload;
        private readonly string _accessToken;

        public FolderService(string accessToken)
        {
            rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Grand Table Top Game");
            _accessToken = accessToken;
            modelUpload = new ModelUpload(_accessToken);
        }

        public async Task CreateGameFolders()
        {
            // ✅ Set "Game 1" as the Root Folder
            string game1Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Grand Table Top Game", "Game 1");

            // ✅ Define Folder Structure
            string[] folders = new string[]
            {
        "Miniatures",
        "Miniatures/Player Characters (PC's)",
        "Miniatures/Non-Player Characters (NPC)",
        "Miniatures/Monsters",
        "Miniatures/Vehicles & Mounts",

        "Terrain & Battlefields",
        "Terrain & Battlefields/Modular Dungeon Tiles",
        "Terrain & Battlefields/Buildings & Ruins",
        "Terrain & Battlefields/Natural Terrain",
        "Terrain & Battlefields/Maps",

        "Game Assets",
        "Game Assets/Dice, Towers & Counters",
        "Game Assets/Tokens",
        "Game Assets/Spell Effects and Templates",

        "Handouts & Reference Materials",
        "Handouts & Reference Materials/Character Sheets",
        "Handouts & Reference Materials/Rulebooks & Quick Reference Guides",
        "Handouts & Reference Materials/Lore Documents (Quests, Backstories, etc.)",

        "Audio & Ambience",
        "Audio & Ambience/Background Music",
        "Audio & Ambience/Sound Effects"
            };

            // ✅ Create "Game 1" Folder If It Doesn't Exist
            if (!Directory.Exists(game1Path))
            {
                Directory.CreateDirectory(game1Path);
                Console.WriteLine($"✅ Created root folder: {game1Path}");
            }

            // ✅ Create Subfolders Inside "Game 1"
            foreach (string folder in folders)
            {
                string folderPath = Path.Combine(game1Path, folder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine($"✅ Created: {folderPath}");
                }
            }

            // ✅ Upload all folders and files to Autodesk Hub
            await UploadFoldersToAutodeskHub();
        }



        private async Task UploadFoldersToAutodeskHub()
        {
            try
            {
                // ✅ Step 1: Get Hub ID
                string hubID = await DataManagement.GetPersonalHub();
                if (hubID == null)
                {
                    Console.WriteLine("❌ No Hub ID found. Cannot proceed.");
                    return;
                }

                // ✅ Step 2: Find or Create "Grand Table Top Game" Project
                string projectID = await DataManagement.GetProjectIdByName(hubID, "Grand Table Top Game");

                if (projectID == null)
                {
                    Console.WriteLine("🟡 'Grand Table Top Game' project not found, creating it...");
                    projectID = await DataManagement.CreateProject(hubID, "Grand Table Top Game");

                    if (projectID == null)
                    {
                        Console.WriteLine("❌ Failed to create 'Grand Table Top Game' project.");
                        return;
                    }
                }
                Console.WriteLine($"✅ Using Project: 'Grand Table Top Game' (ID: {projectID})");

                // ✅ Step 3: Get Root Folder ID
                string rootFolderId = await GetRootFolderId(projectID);
                if (rootFolderId == null)
                {
                    Console.WriteLine("❌ Could not retrieve root folder ID.");
                    return;
                }

                // ✅ Step 4: Check if "Game 1" Exists in Root Folder
                Dictionary<string, string> projectFolders = await GetAutodeskFolderMapping(projectID, rootFolderId);
                string game1FolderId;

                if (!projectFolders.TryGetValue("game 1", out game1FolderId))
                {
                    Console.WriteLine("🟡 'Game 1' folder not found, creating it...");
                    game1FolderId = await EnsureFolderExists(projectID, rootFolderId, "Game 1");

                    if (game1FolderId == null)
                    {
                        Console.WriteLine("❌ Failed to create 'Game 1' in Autodesk.");
                        return;
                    }
                }
                Console.WriteLine($"✅ 'Game 1' is now the primary folder (ID: {game1FolderId}).");

                // ✅ Step 5: Ensure Subfolders Exist Inside "Game 1"
                Dictionary<string, string> categoryFolders = await GetAutodeskFolderMapping(projectID, game1FolderId);
                string[] categories = new string[]
                {
            "Miniatures",
            "Miniatures/Player Characters (PC's)",
            "Miniatures/Non-Player Characters (NPC)",
            "Miniatures/Monsters",
            "Miniatures/Vehicles & Mounts",
            "Terrain & Battlefields",
            "Terrain & Battlefields/Modular Dungeon Tiles",
            "Terrain & Battlefields/Buildings & Ruins",
            "Terrain & Battlefields/Natural Terrain",
            "Terrain & Battlefields/Maps",
            "Game Assets",
            "Game Assets/Dice, Towers & Counters",
            "Game Assets/Tokens",
            "Game Assets/Spell Effects and Templates",
            "Handouts & Reference Materials",
            "Handouts & Reference Materials/Character Sheets",
            "Handouts & Reference Materials/Rulebooks & Quick Reference Guides",
            "Handouts & Reference Materials/Lore Documents (Quests, Backstories, etc.)",
            "Audio & Ambience",
            "Audio & Ambience/Background Music",
            "Audio & Ambience/Sound Effects"
                };

                foreach (string category in categories)
                {
                    string[] pathParts = category.Split('/');
                    string parentFolderId = game1FolderId; // Start at "Game 1"

                    for (int i = 0; i < pathParts.Length; i++)
                    {
                        string folderName = pathParts[i].Trim();
                        if (!categoryFolders.TryGetValue(folderName.ToLower(), out string folderId))
                        {
                            folderId = await EnsureFolderExists(projectID, parentFolderId, folderName);
                            if (folderId != null)
                            {
                                categoryFolders[folderName.ToLower()] = folderId;
                            }
                        }
                        parentFolderId = folderId; // Move to next level
                    }
                }

                Console.WriteLine("✅ All required folders exist inside 'Game 1'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
        public static async Task<string> GetRootFolderId(string projectId)
        {
            string hubId = await DataManagement.GetPersonalHub();
            string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects/{projectId}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {TokenManager.GetToken()}");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error fetching root folder ID for project {projectId}: {responseContent}");
                    return null;
                }

                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                string rootFolderId = jsonResponse["data"]["relationships"]["rootFolder"]["data"]["id"].ToString();

                Console.WriteLine($"✅ Root folder ID for project {projectId}: {rootFolderId}");
                return rootFolderId;
            }
        }

        private async Task<string> EnsureFolderExists(string projectID, string parentFolderId, string folderName)
        {
            // 🔍 Step 1: Try fetching the existing folder ID first
            string existingFolderId = await GetFolderIdByName(projectID, parentFolderId, folderName);
            if (existingFolderId != null)
            {
                return existingFolderId; // ✅ Found, return ID
            }

            // 🔄 Step 2: Try fetching it directly from Autodesk API
            existingFolderId = await FetchFolderId(projectID, parentFolderId, folderName);
            if (existingFolderId != null)
            {
                return existingFolderId; // ✅ Found, return ID
            }

            // 🚀 Step 3: Create the folder since it doesn't exist
            Console.WriteLine($"🆕 Creating folder '{folderName}' under {parentFolderId}...");
            bool folderCreated = await DataManagement.CreateNewFolder(projectID, parentFolderId, folderName);

            if (!folderCreated)
            {
                Console.WriteLine($"❌ Failed to create folder '{folderName}' in Autodesk.");
                return null;
            }

            // ⏳ Step 4: Wait a bit for Autodesk API to register the folder
            await Task.Delay(3000); // 3 seconds delay

            // 🔍 Step 5: Fetch newly created folder ID
            return await FetchFolderId(projectID, parentFolderId, folderName);
        }


        private async Task UploadFilesInFolder(string folderPath, string projectID, string cloudFolderId)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"⚠️ Skipping sync: Local folder does not exist: {folderPath}");
                return;
            }

            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                Console.WriteLine($"🔍 Uploading file: {file}");
                await modelUpload.UploadModel(projectID, cloudFolderId, file);
            }
        }
        private async Task<Dictionary<string, string>> GetAutodeskFolderMapping(string projectID, string parentFolderId)
        {
            Dictionary<string, string> folderMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var projectItems = await DataManagement.GetProjectItems(projectID, parentFolderId);
            foreach (var (itemId, itemName, isFolder) in projectItems)
            {
                if (isFolder)
                {
                    string normalizedFolderName = itemName.Trim().ToLower(); // Normalize names
                    folderMapping[normalizedFolderName] = itemId;
                }
            }

            return folderMapping;
        }


        private async Task<string> GetFolderIdByName(string projectId, string parentFolderId, string folderName)
        {
            var existingFolders = await GetAutodeskFolderMapping(projectId, parentFolderId);

            string normalizedFolderName = folderName.Trim().ToLower(); // Normalize for consistent lookup

            if (existingFolders.TryGetValue(normalizedFolderName, out string existingFolderId))
            {
                Console.WriteLine($"✅ Folder '{folderName}' already exists with ID: {existingFolderId}");
                return existingFolderId;
            }

            return null; // Folder not found
        }

        private async Task<string> FetchFolderId(string projectId, string parentFolderId, string folderName)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{parentFolderId}/contents";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {TokenManager.GetToken()}");
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error fetching folder ID for '{folderName}': {responseContent}");
                return null;
            }

            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            foreach (var folder in jsonResponse["data"])
            {
                string existingFolderName = folder["attributes"]["name"].ToString().Trim().ToLower();
                string existingFolderId = folder["id"].ToString();

                if (existingFolderName == folderName.Trim().ToLower())
                {
                    Console.WriteLine($"✅ Found existing folder '{folderName}' with ID: {existingFolderId}");
                    return existingFolderId;
                }
            }

            return null; // Folder not found
        }


    }
}
