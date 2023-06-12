using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "mediacontainer";
        private const string _connectionString = "DefaultEndpointsProtocol=https;AccountName=alluresocialconatiner;AccountKey=Vim91SJI/3N51HE2i0QM59p5LtpWdlVijLqX69iObPDuZhuoyOmqdHdWBELchgUe6tlzNynZOsYp+ASte8lhhg==;EndpointSuffix=core.windows.net";

        public FilesController()
        {
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                string fileName = Guid.NewGuid().ToString() + file.FileName;

                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blob = container.GetBlobClient(fileName);

                using (var stream = file.OpenReadStream())
                {
                    await blob.UploadAsync(stream);
                }

                return Ok($"File uploaded successfully. Blob URL: {blob.Uri}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while uploading the file: {ex.Message}");
            }
        }

        [HttpGet("list")]
        public IActionResult GetFiles()
        {
            try
            {
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_containerName);
                var files = container.GetBlobs();

                var fileDataList = new List<FileData>();
                foreach (var file in files)
                {
                    var fileData = new FileData
                    {
                        Name = file.Name,
                        ContentType = file.Properties.ContentType,
                        Content = ReadBlobContent(file)
                    };
                    fileDataList.Add(fileData);
                }

                return Ok(fileDataList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the files: {ex.Message}");
            }
        }

        private string ReadBlobContent(BlobItem blobItem)
        {
            BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(blobItem.Name);

            using (var memoryStream = new MemoryStream())
            {
                blobClient.DownloadTo(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public class FileData
        {
            public string Name { get; set; }
            public string ContentType { get; set; }
            public string Content { get; set; }
        }
    }
}
