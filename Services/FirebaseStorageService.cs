using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Services
{
    public class FirebaseStorageService
    {
        private readonly string _bucketName;
        private readonly StorageClient _storageClient;
        private readonly FirestoreSettings _settings;

        public FirebaseStorageService(IOptions<FirestoreSettings> firestoreSettings)
        {
            _settings = firestoreSettings.Value;

            try
            {
                if (!File.Exists(_settings.CredentialsPath))
                {
                    throw new FileNotFoundException($"Firebase credentials file not found at: {_settings.CredentialsPath}");
                }

                var credential = GoogleCredential.FromFile(_settings.CredentialsPath);
                _storageClient = StorageClient.Create(credential);
                _bucketName = $"{_settings.ProjectId}.appspot.com";

                Console.WriteLine("✅ Firebase Storage Service initialized successfully");
                Console.WriteLine($"✅ Using bucket: {_bucketName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Firebase Storage Service initialization failed: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize Firebase Storage service: {ex.Message}", ex);
            }
        }

        // Download file using Google Cloud Storage
        //public async Task<Stream> DownloadFileAsync(string storagePath)
        //{
        //    try
        //    {
        //        Console.WriteLine($"📥 Downloading file from storage path: {storagePath}");

        //        var memoryStream = new MemoryStream();
        //        await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream);
        //        memoryStream.Position = 0;

        //        Console.WriteLine($"✅ File downloaded successfully: {storagePath}");
        //        return memoryStream;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error downloading file: {ex.Message}");
        //        throw;
        //    }
        //}

        // Download file as bytes
        //public async Task<byte[]> DownloadFileBytesAsync(string storagePath)
        //{
        //    try
        //    {
        //        using var memoryStream = new MemoryStream();
        //        await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream);
        //        return memoryStream.ToArray();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error downloading file bytes: {ex.Message}");
        //        throw;
        //    }
        //}

        // List employee documents using Google Cloud Storage
        public async Task<List<StorageFileInfo>> ListEmployeeDocumentsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Listing documents for employee: {employeeId}");
                var documents = new List<StorageFileInfo>();

                // List certificates directory
                var certificatesPrefix = $"documents/{employeeId}/certificates/";
                await ListObjectsWithPrefixAsync(certificatesPrefix, documents);

                // List qualifications directory
                var qualificationsPrefix = $"documents/{employeeId}/qualifications/";
                await ListObjectsWithPrefixAsync(qualificationsPrefix, documents);

                Console.WriteLine($"✅ Found {documents.Count} documents in storage for employee {employeeId}");
                return documents;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing employee documents: {ex.Message}");
                return new List<StorageFileInfo>();
            }
        }

        // Helper method to list objects with a prefix
        private async Task ListObjectsWithPrefixAsync(string prefix, List<StorageFileInfo> documents)
        {
            try
            {
                var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);
                await foreach (var storageObject in objects)
                {
                    // Skip if it's a directory (ends with /) or if it's the prefix itself
                    if (storageObject.Name.EndsWith("/") || storageObject.Name == prefix)
                        continue;

                    var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storageObject.Name)}";

                    documents.Add(new StorageFileInfo
                    {
                        Name = Path.GetFileName(storageObject.Name),
                        Path = storageObject.Name,
                        Size = (long)(storageObject.Size ?? 0),
                        Updated = storageObject.Updated ?? DateTime.Now,
                        ContentType = storageObject.ContentType,
                        DownloadUrl = downloadUrl
                    });

                    Console.WriteLine($"📄 Found: {storageObject.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error listing objects with prefix {prefix}: {ex.Message}");
            }
        }

        // Get download URL using Google Cloud Storage
        //public async Task<string> GetDownloadUrlAsync(string storagePath)
        //{
        //    try
        //    {
        //        // For public files, we can construct the URL directly
        //        var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storagePath)}";

        //        Console.WriteLine($"✅ Generated download URL: {downloadUrl}");
        //        return downloadUrl;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting download URL: {ex.Message}");
        //        throw;
        //    }
        //}

        // Generate signed URL for private files (if needed)
        public async Task<string> GenerateSignedUrlAsync(string storagePath, TimeSpan expiration)
        {
            try
            {
                var urlSigner = UrlSigner.FromServiceAccountPath(_settings.CredentialsPath);
                var signedUrl = await urlSigner.SignAsync(_bucketName, storagePath, expiration, HttpMethod.Get);

                Console.WriteLine($"✅ Generated signed URL: {signedUrl}");
                return signedUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating signed URL: {ex.Message}");
                throw;
            }
        }

        // Check if file exists
        //public async Task<bool> FileExistsAsync(string storagePath)
        //{
        //    try
        //    {
        //        await _storageClient.GetObjectAsync(_bucketName, storagePath);
        //        return true;
        //    }
        //    catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        //    {
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error checking file existence: {ex.Message}");
        //        return false;
        //    }
        //}

        // Get file metadata
        public async Task<StorageFileInfo> GetFileInfoAsync(string storagePath)
        {
            try
            {
                var storageObject = await _storageClient.GetObjectAsync(_bucketName, storagePath);
                var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storagePath)}";

                return new StorageFileInfo
                {
                    Name = Path.GetFileName(storageObject.Name),
                    Path = storageObject.Name,
                    Size = (long)(storageObject.Size ?? 0),
                    Updated = storageObject.Updated ?? DateTime.Now,
                    ContentType = storageObject.ContentType,
                    DownloadUrl = downloadUrl
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting file info: {ex.Message}");
                throw;
            }
        }

        // Upload file to storage
        public async Task<string> UploadFileAsync(IFormFile file, string fileName = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty");
                }

                fileName ??= $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadedObject = await _storageClient.UploadObjectAsync(
                    bucket: _bucketName,
                    objectName: $"uploads/{fileName}",
                    contentType: file.ContentType,
                    source: memoryStream
                );

                var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/uploads/{fileName}";
                Console.WriteLine($"✅ File uploaded successfully: {downloadUrl}");

                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File upload failed: {ex.Message}");
                throw;
            }
        }

        // Delete file by fileName
        public async Task DeleteFileAsync(string fileName)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(_bucketName, $"uploads/{fileName}");
                Console.WriteLine($"✅ File deleted: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File deletion failed: {ex.Message}");
                throw;
            }
        }

        // Upload training material with validation
        public async Task<string> UploadTrainingMaterialAsync(IFormFile file, string programId, string materialType)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty");
                }

                var allowedExtensions = GetAllowedExtensions(materialType);
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException($"Invalid file type for {materialType}. Allowed: {string.Join(", ", allowedExtensions)}");
                }

                var maxSize = GetMaxFileSize(materialType);
                if (file.Length > maxSize)
                {
                    throw new ArgumentException($"File size too large for {materialType}. Maximum size: {maxSize / (1024 * 1024)}MB");
                }

                var fileName = $"{materialType}_{programId}_{Guid.NewGuid()}{fileExtension}";
                var downloadUrl = await UploadFileAsync(file, $"training-materials/{programId}/{fileName}");

                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Training material upload failed: {ex.Message}");
                throw;
            }
        }

        // Delete file by URL
        public async Task DeleteFileByUrlAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return;

                var uri = new Uri(fileUrl);
                var filePath = uri.AbsolutePath;

                if (filePath.StartsWith($"/{_bucketName}/"))
                {
                    filePath = filePath.Substring(_bucketName.Length + 2);
                }
                else if (filePath.StartsWith("/"))
                {
                    filePath = filePath.Substring(1);
                }

                await _storageClient.DeleteObjectAsync(_bucketName, filePath);
                Console.WriteLine($"✅ File deleted: {filePath}");
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"⚠️ File not found, might be already deleted: {fileUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File deletion failed: {ex.Message}");
                throw;
            }
        }

        // Get allowed file extensions based on material type
        private string[] GetAllowedExtensions(string materialType)
        {
            return materialType.ToLower() switch
            {
                "cover" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                "syllabus" => new[] { ".pdf", ".doc", ".docx", ".txt" },
                "video" => new[] { ".mp4", ".mov", ".avi", ".webm" },
                "materials" => new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".zip" },
                _ => new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" }
            };
        }

        // Get maximum file size based on material type
        private long GetMaxFileSize(string materialType)
        {
            return materialType.ToLower() switch
            {
                "cover" => 5 * 1024 * 1024,
                "syllabus" => 10 * 1024 * 1024,
                "video" => 100 * 1024 * 1024,
                "materials" => 50 * 1024 * 1024,
                _ => 10 * 1024 * 1024
            };
        }

        // Upload training certificate
        //public async Task<string> UploadTrainingCertificateAsync(IFormFile file, string employeeId, string trainingId)
        //{
        //    try
        //    {
        //        if (file == null || file.Length == 0)
        //        {
        //            throw new ArgumentException("File is empty");
        //        }

        //        // Validate file type
        //        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        //        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
        //        }

        //        // Validate file size (10MB max)
        //        if (file.Length > 10 * 1024 * 1024)
        //        {
        //            throw new ArgumentException("File size must be less than 10MB");
        //        }

        //        var fileName = $"training_certificate_{trainingId}_{Guid.NewGuid()}{fileExtension}";
        //        var storagePath = $"training-certificates/{employeeId}/{fileName}";

        //        using var memoryStream = new MemoryStream();
        //        await file.CopyToAsync(memoryStream);
        //        memoryStream.Position = 0;

        //        var uploadedObject = await _storageClient.UploadObjectAsync(
        //            bucket: _bucketName,
        //            objectName: storagePath,
        //            contentType: file.ContentType,
        //            source: memoryStream
        //        );

        //        var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{storagePath}";
        //        Console.WriteLine($"✅ Training certificate uploaded: {downloadUrl}");

        //        return downloadUrl;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Training certificate upload failed: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<Stream> DownloadTrainingCertificateAsync(string employeeId, string trainingId, string fileName = null)
        {
            try
            {
                // If specific filename is provided
                if (!string.IsNullOrEmpty(fileName))
                {
                    var storagePath = $"training-certificates/{employeeId}/{fileName}";
                    return await DownloadFileAsync(storagePath);
                }

                // Otherwise, find the certificate for this training
                var prefix = $"training-certificates/{employeeId}/training_certificate_{trainingId}_";
                var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);

                await foreach (var storageObject in objects)
                {
                    if (!storageObject.Name.EndsWith("/"))
                    {
                        return await DownloadFileAsync(storageObject.Name);
                    }
                }

                throw new FileNotFoundException($"No certificate found for training {trainingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading training certificate: {ex.Message}");
                throw;
            }
        }

        // Fixed method using Google Cloud Storage
        //public async Task<List<StorageFileInfo>> ListTrainingCertificatesAsync(string employeeId)
        //{
        //    try
        //    {
        //        var files = new List<StorageFileInfo>();
        //        var prefix = $"training-certificates/{employeeId}/";
                
        //        try
        //        {
        //            var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);
        //            await foreach (var storageObject in objects)
        //            {
        //                if (!storageObject.Name.EndsWith("/"))
        //                {
        //                    var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storageObject.Name)}";
                            
        //                    files.Add(new StorageFileInfo
        //                    {
        //                        Name = Path.GetFileName(storageObject.Name),
        //                        Path = storageObject.Name,
        //                        Size = (long)(storageObject.Size ?? 0),
        //                        Updated = storageObject.Updated ?? DateTime.Now,
        //                        ContentType = storageObject.ContentType ?? "application/octet-stream",
        //                        DownloadUrl = downloadUrl
        //                    });
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"⚠️ No certificates directory found for employee {employeeId}: {ex.Message}");
        //        }

        //        Console.WriteLine($"✅ Found {files.Count} training certificate files for employee {employeeId}");
        //        return files;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error listing training certificates: {ex.Message}");
        //        return new List<StorageFileInfo>();
        //    }
        //}
        #region Certificate Methods - Updated for correct storage location

        // Download certificate by direct URL or storage path
        public async Task<Stream> DownloadCertificateAsync(string fileUrlOrPath)
        {
            try
            {
                Console.WriteLine($"📥 Downloading certificate: {fileUrlOrPath}");

                // Extract storage path from URL if it's a full URL
                var storagePath = ExtractStoragePathFromUrl(fileUrlOrPath);

                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream);
                memoryStream.Position = 0;

                Console.WriteLine($"✅ Certificate downloaded successfully: {storagePath}");
                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading certificate: {ex.Message}");
                throw new Exception($"Failed to download certificate: {ex.Message}", ex);
            }
        }

        // Download certificate by employee and training ID
        public async Task<Stream> DownloadCertificateAsync(string employeeId, string trainingId)
        {
            try
            {
                Console.WriteLine($"🔍 Looking for certificate - Employee: {employeeId}, Training: {trainingId}");

                // Try to find the certificate in the certificates folder
                var prefix = $"certificates/{employeeId}/";
                var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);

                var certificateFiles = new List<Google.Apis.Storage.v1.Data.Object>();

                await foreach (var storageObject in objects)
                {
                    if (!storageObject.Name.EndsWith("/") &&
                        storageObject.Name.Contains(trainingId) &&
                        IsCertificateFile(storageObject.Name))
                    {
                        certificateFiles.Add(storageObject);
                        Console.WriteLine($"📄 Found potential certificate: {storageObject.Name}");
                    }
                }

                if (!certificateFiles.Any())
                {
                    throw new FileNotFoundException($"No certificate found for training {trainingId}");
                }

                // Get the most recent certificate
                var latestCertificate = certificateFiles
                    .OrderByDescending(f => f.Updated ?? DateTime.MinValue)
                    .First();

                Console.WriteLine($"✅ Using certificate: {latestCertificate.Name}");
                return await DownloadCertificateAsync(latestCertificate.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error finding certificate: {ex.Message}");
                throw;
            }
        }

        // List all certificates for an employee
        public async Task<List<StorageFileInfo>> ListEmployeeCertificatesAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Listing certificates for employee: {employeeId}");
                var certificates = new List<StorageFileInfo>();

                var prefix = $"certificates/{employeeId}/";
                await ListCertificateObjectsAsync(prefix, certificates);

                Console.WriteLine($"✅ Found {certificates.Count} certificates for employee {employeeId}");
                return certificates;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing employee certificates: {ex.Message}");
                return new List<StorageFileInfo>();
            }
        }

        // List all training certificates (from the certificates folder)
        public async Task<List<StorageFileInfo>> ListTrainingCertificatesAsync(string employeeId)
        {
            try
            {
                var files = new List<StorageFileInfo>();
                var prefix = $"certificates/{employeeId}/";

                try
                {
                    var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);
                    await foreach (var storageObject in objects)
                    {
                        if (!storageObject.Name.EndsWith("/") && IsCertificateFile(storageObject.Name))
                        {
                            var downloadUrl = GetDirectDownloadUrl(storageObject.Name);

                            files.Add(new StorageFileInfo
                            {
                                Name = Path.GetFileName(storageObject.Name),
                                Path = storageObject.Name,
                                Size = (long)(storageObject.Size ?? 0),
                                Updated = storageObject.Updated ?? DateTime.Now,
                                ContentType = GetContentTypeFromFileName(storageObject.Name),
                                DownloadUrl = downloadUrl
                            });

                            Console.WriteLine($"📄 Found certificate: {storageObject.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ No certificates directory found for employee {employeeId}: {ex.Message}");
                }

                Console.WriteLine($"✅ Found {files.Count} training certificate files for employee {employeeId}");
                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing training certificates: {ex.Message}");
                return new List<StorageFileInfo>();
            }
        }

        // Download all certificates for an employee as ZIP
        public async Task<byte[]> DownloadAllCertificatesAsZipAsync(string employeeId)
        {
            try
            {
                var certificates = await ListTrainingCertificatesAsync(employeeId);
                if (!certificates.Any())
                {
                    throw new FileNotFoundException("No certificates found for employee");
                }

                using var memoryStream = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var certificate in certificates)
                    {
                        try
                        {
                            var fileStream = await DownloadCertificateAsync(certificate.Path);
                            var zipEntry = archive.CreateEntry(certificate.Name, System.IO.Compression.CompressionLevel.Optimal);

                            using var entryStream = zipEntry.Open();
                            await fileStream.CopyToAsync(entryStream);

                            Console.WriteLine($"✅ Added to ZIP: {certificate.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Error adding {certificate.Name} to ZIP: {ex.Message}");
                        }
                    }
                }

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating ZIP file: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        // Extract storage path from full URL
        private string ExtractStoragePathFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return fileUrl;

            try
            {
                // If it's already a storage path (not a full URL), return as is
                if (!fileUrl.StartsWith("http"))
                    return fileUrl;

                var uri = new Uri(fileUrl);

                // Handle storage.googleapis.com URLs
                if (uri.Host == "storage.googleapis.com")
                {
                    var path = uri.AbsolutePath.Substring(1); // Remove leading slash
                    if (path.StartsWith($"{_bucketName}/"))
                    {
                        return path.Substring(_bucketName.Length + 1); // Remove bucket name
                    }
                    return path;
                }

                // Handle firebasestorage.googleapis.com URLs
                if (uri.Host == "firebasestorage.googleapis.com")
                {
                    var pathSegments = uri.AbsolutePath.Split('/');
                    if (pathSegments.Length >= 5 && pathSegments[1] == "v0" && pathSegments[2] == "b" && pathSegments[3] == _bucketName && pathSegments[4] == "o")
                    {
                        var encodedPath = pathSegments[5];
                        return Uri.UnescapeDataString(encodedPath);
                    }
                }

                // If we can't parse it, try to extract the path after the bucket name
                var bucketIndex = fileUrl.IndexOf(_bucketName);
                if (bucketIndex > 0)
                {
                    return fileUrl.Substring(bucketIndex + _bucketName.Length + 1);
                }

                return fileUrl;
            }
            catch
            {
                // If URL parsing fails, return the original string
                return fileUrl;
            }
        }

        // Check if file is a certificate file
        private bool IsCertificateFile(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            return allowedExtensions.Contains(extension);
        }

        // Get content type from file name
        private string GetContentTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        // Get direct download URL for a storage path
        private string GetDirectDownloadUrl(string storagePath)
        {
            return $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storagePath)}";
        }

        // Helper method to list certificate objects
        private async Task ListCertificateObjectsAsync(string prefix, List<StorageFileInfo> certificates)
        {
            try
            {
                var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);
                await foreach (var storageObject in objects)
                {
                    if (!storageObject.Name.EndsWith("/") && IsCertificateFile(storageObject.Name))
                    {
                        var downloadUrl = GetDirectDownloadUrl(storageObject.Name);

                        certificates.Add(new StorageFileInfo
                        {
                            Name = Path.GetFileName(storageObject.Name),
                            Path = storageObject.Name,
                            Size = (long)(storageObject.Size ?? 0),
                            Updated = storageObject.Updated ?? DateTime.Now,
                            ContentType = storageObject.ContentType ?? GetContentTypeFromFileName(storageObject.Name),
                            DownloadUrl = downloadUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error listing objects with prefix {prefix}: {ex.Message}");
            }
        }

        #endregion

        #region Existing Methods (Keep these for backward compatibility)

        // Download file using Google Cloud Storage
        public async Task<Stream> DownloadFileAsync(string storagePath)
        {
            try
            {
                Console.WriteLine($"📥 Downloading file from storage path: {storagePath}");

                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream);
                memoryStream.Position = 0;

                Console.WriteLine($"✅ File downloaded successfully: {storagePath}");
                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading file: {ex.Message}");
                throw;
            }
        }

        // Download file as bytes
        public async Task<byte[]> DownloadFileBytesAsync(string storagePath)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading file bytes: {ex.Message}");
                throw;
            }
        }

        // Get download URL using Google Cloud Storage
        public async Task<string> GetDownloadUrlAsync(string storagePath)
        {
            try
            {
                var downloadUrl = $"https://storage.googleapis.com/{_bucketName}/{Uri.EscapeDataString(storagePath)}";
                Console.WriteLine($"✅ Generated download URL: {downloadUrl}");
                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting download URL: {ex.Message}");
                throw;
            }
        }

        // Check if file exists
        public async Task<bool> FileExistsAsync(string storagePath)
        {
            try
            {
                await _storageClient.GetObjectAsync(_bucketName, storagePath);
                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error checking file existence: {ex.Message}");
                return false;
            }
        }

        // Upload training certificate (updated for correct location)
        public async Task<string> UploadTrainingCertificateAsync(IFormFile file, string employeeId, string trainingId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty");
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
                }

                // Validate file size (10MB max)
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new ArgumentException("File size must be less than 10MB");
                }

                var fileName = $"certificate_{trainingId}_{Guid.NewGuid()}{fileExtension}";
                var storagePath = $"certificates/{employeeId}/{fileName}"; // Updated path

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadedObject = await _storageClient.UploadObjectAsync(
                    bucket: _bucketName,
                    objectName: storagePath,
                    contentType: file.ContentType,
                    source: memoryStream
                );

                var downloadUrl = GetDirectDownloadUrl(storagePath);
                Console.WriteLine($"✅ Training certificate uploaded: {downloadUrl}");

                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Training certificate upload failed: {ex.Message}");
                throw;
            }
        }

        // ... Keep other existing methods as they are ...

        #endregion
    }
}