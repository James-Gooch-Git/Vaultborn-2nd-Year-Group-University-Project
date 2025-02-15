using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AssetManager.Infrastructure.Services
{
    public class ModelUpload
    {
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "https://developer.api.autodesk.com";

        public ModelUpload(string accessToken)
        {
            _accessToken = accessToken;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Step 1: Create a storage location for the file in Forge.
        /// </summary>
        public async Task<string> CreateStorageLocation(string projectId, string folderId, string fileName)
        {
            Console.WriteLine(
                $"🔍 Debug: Creating Storage Location for {fileName} in Project {projectId}, Folder {folderId}...");

            string url = $"{API_BASE_URL}/data/v1/projects/{projectId}/storage";

            var payload = new
            {
                jsonapi = new { version = "1.0" },
                data = new
                {
                    type = "objects",
                    attributes = new { name = fileName },
                    relationships = new
                    {
                        target = new
                        {
                            data = new { type = "folders", id = folderId }
                        }
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            Console.WriteLine($"📤 Request JSON: {jsonPayload}");

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = content;

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"📥 Response: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error creating storage location: {response.StatusCode}");
                Console.WriteLine($"Error details: {responseBody}");
                return null;
            }

            dynamic responseData = JsonConvert.DeserializeObject(responseBody);
            string storageUrn = responseData.data.id;
            Console.WriteLine($"✅ Storage Location Created: {storageUrn}");

            return storageUrn;
        }

        public async Task<(string signedUrl, string uploadKey)> GetSignedS3UploadUrl(string storageUrn)
        {
            var (bucketKey, objectKey) = ExtractBucketAndObjectKey(storageUrn);
            if (string.IsNullOrEmpty(bucketKey) || string.IsNullOrEmpty(objectKey))
            {
                Console.WriteLine("❌ Failed to extract bucket and object key.");
                return (null, null);
            }

            string url = $"{API_BASE_URL}/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3upload";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error getting signed S3 upload URL: {response.StatusCode}");
                Console.WriteLine($"Error details: {json}");
                return (null, null);
            }

            // ✅ Parse Response
            dynamic responseData = JsonConvert.DeserializeObject(json);
            string signedUrl = responseData.urls[0];
            string uploadKey = responseData.uploadKey;

            Console.WriteLine($"✅ Debug: Signed URL: {signedUrl}");
            Console.WriteLine($"✅ Debug: Upload Key: {uploadKey}");

            return (signedUrl, uploadKey);
        }



        /// <summary>
        /// Step 2: Upload the file directly to the storage endpoint.
        /// </summary>
        public async Task<bool> UploadFileToForge(string filePath, string projectId, string storageUrn)
        {
            var (signedUrl, uploadKey) = await GetSignedS3UploadUrl(storageUrn);
            if (string.IsNullOrEmpty(signedUrl) || string.IsNullOrEmpty(uploadKey))
            {
                Console.WriteLine("❌ Failed to get signed S3 upload URL.");
                return false;
            }

            try
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                using var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage response = await _httpClient.PutAsync(signedUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✔️ File uploaded successfully: {filePath}");

                    // ✅ Finalize the upload in Forge
                    bool finalized = await CompleteUpload(storageUrn, uploadKey);
                    return finalized;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error uploading file: {response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"Error details: {errorResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred during file upload: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> CompleteUpload(string storageUrn, string uploadKey)
        {
            (var signedUrl, uploadKey) = await GetSignedS3UploadUrl(storageUrn);
            var (bucketKey, objectKey) = ExtractBucketAndObjectKey(storageUrn);
            if (string.IsNullOrEmpty(bucketKey) || string.IsNullOrEmpty(objectKey))
            {
                Console.WriteLine("❌ Failed to extract bucket and object key.");
                return false;
            }

            string url = $"{API_BASE_URL}/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3upload";
            
            var payload = new { uploadKey };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());
            request.Content = content;

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error finalizing upload: {response.StatusCode}");
                Console.WriteLine($"Error details: {responseBody}");
                return false;
            }

            Console.WriteLine($"✅ Upload finalized successfully: {responseBody}");
            return true;
        }

        public Tuple<string, string> ExtractBucketAndObjectKey(string storageUrn)
        {
            if (string.IsNullOrEmpty(storageUrn))
            {
                Console.WriteLine("❌ Invalid storage URN: Null or empty.");
                return new Tuple<string, string>(null, null);
            }

            // Ensure URN starts with "urn:adsk.objects:os.object:"
            string prefix = "urn:adsk.objects:os.object:";
            if (!storageUrn.StartsWith(prefix))
            {
                Console.WriteLine($"❌ Invalid URN format: {storageUrn}");
                return new Tuple<string, string>(null, null);
            }

            // Remove prefix and split by '/'
            string urnBody = storageUrn.Substring(prefix.Length);
            string[] urnParts = urnBody.Split('/');

            if (urnParts.Length < 2) // We need at least "bucketKey/objectKey"
            {
                Console.WriteLine($"❌ Invalid URN format: {storageUrn}");
                return new Tuple<string, string>(null, null);
            }

            string bucketKey = urnParts[0]; // Extract bucket key
            string objectKey = urnParts[1]; // Extract object key

            Console.WriteLine($"✅ Debug: Extracted Bucket Key: {bucketKey}");
            Console.WriteLine($"✅ Debug: Extracted Object Key: {objectKey}");

            return new Tuple<string, string>(bucketKey, objectKey);
        }

       public async Task<bool> CreateItemAndVersion(string projectId, string folderId, string fileName, string objectUrn)
        {
            Console.WriteLine("🔹 Creating Item & Version in Autodesk Forge...");

            string url = $"{API_BASE_URL}/data/v1/projects/{projectId}/items";

            var payload = new
            {
                jsonapi = new { version = "1.0" },
                data = new
                {
                    type = "items",
                    attributes = new
                    {
                        displayName = fileName,
                        extension = new
                        {
                            type = "items:autodesk.bim360:File",
                            version = "1.0"
                        }
                    },
                    relationships = new
                    {
                        tip = new { data = new { type = "versions", id = "1" } },
                        parent = new { data = new { type = "folders", id = folderId } }
                    }
                },
                included = new[]
                {
                    new
                    {
                        type = "versions",
                        id = "1",
                        attributes = new
                        {
                            name = fileName,
                            extension = new
                            {
                                type = "versions:autodesk.bim360:File",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            storage = new { data = new { type = "objects", id = objectUrn } }
                        }
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            Console.WriteLine($"📤 Request JSON: {jsonPayload}");

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = content;

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"📥 Response: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error creating Item & Version: {response.StatusCode}");
                Console.WriteLine($"Error details: {responseBody}");
                return false;
            }

            Console.WriteLine($"✅ Item & Version Created Successfully!");
            return true;
        }



        /// <summary>
        /// Handles the full upload process (storage + file upload).
        /// </summary>
        public async Task<bool> UploadModel(string projectId, string folderId, string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            Console.WriteLine($"📤 Uploading {fileName} to Autodesk Forge...");

            // ✅ Step 1: Create Storage Location in Forge
            Console.WriteLine("🔹 Creating Storage Location...");
            string storageUrn = await CreateStorageLocation(projectId, folderId, fileName);
            if (string.IsNullOrEmpty(storageUrn))
            {
                Console.WriteLine("❌ Failed to create storage location.");
                return false;
            }

            Console.WriteLine($"✅ Storage Location Created: {storageUrn}");

            // ✅ Step 2: Get Signed S3 Upload URL
            Console.WriteLine("🔹 Requesting Signed S3 URL...");
            var (signedUrl, uploadKey) = await GetSignedS3UploadUrl(storageUrn);
            if (string.IsNullOrEmpty(signedUrl) || string.IsNullOrEmpty(uploadKey))
            {
                Console.WriteLine("❌ Failed to get Signed S3 URL.");
                return false;
            }

            Console.WriteLine($"✅ Retrieved Signed S3 URL: {signedUrl}");

            // ✅ Step 3: Upload File to S3
            Console.WriteLine($"📤 Uploading {fileName} to Signed URL...");
            bool uploadSuccess = await UploadFileToForge(filePath, projectId, storageUrn);
            if (!uploadSuccess)
            {
                Console.WriteLine("❌ File upload failed.");
                return false;
            }

            Console.WriteLine($"✅ File Uploaded Successfully: {filePath}");

            // ✅ Step 4: Finalize Upload in Forge
            Console.WriteLine("🔹 Finalizing Upload...");
            bool finalizeSuccess = await CompleteUpload(storageUrn, uploadKey);
            if (!finalizeSuccess)
            {
                Console.WriteLine("❌ Failed to finalize upload.");
                return false;
            }

            Console.WriteLine($"✅ Upload Finalized Successfully!");

            // ✅ Step 5: Create an Item & Version so file appears in Forge
            Console.WriteLine("🔹 Registering file in Autodesk Forge...");
            bool itemCreated = await CreateItemAndVersion(projectId, folderId, fileName, storageUrn);
            if (!itemCreated)
            {
                Console.WriteLine("❌ Failed to create Item & Version. File may not appear in Forge.");
                return false;
            }

            Console.WriteLine($"✅ Upload process completed, and file is visible in Forge.");
            return true;
        }

    }




}
