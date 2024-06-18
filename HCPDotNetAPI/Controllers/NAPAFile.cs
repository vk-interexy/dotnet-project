using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using HCPDotNetBLL;
using dotnetscrape_lib.DataObjects;

namespace HCPDotNetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DOTNETFileController : ControllerBase
    {
        private IConfiguration _configuration;

        public DOTNETFileController(IConfiguration configuraiton)
        {
            _configuration = configuraiton;
        }

        private void LogError(Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText(_configuration["ErrorLogPath"].ToString(),
                    $"{DateTime.Now}{Environment.NewLine}" +
                    $"Message: {ex.Message}{Environment.NewLine}" +
                    $"Stack Trace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}" +
                    $"{Environment.NewLine}");
            }
            catch
            {

            }
        }
        private string CompressFile(string fileName)
        {
            
            string compressedFileName = Path.ChangeExtension(fileName,".zip");
            string newFileNameEntryInArchive = Path.GetFileName(fileName);
            using (FileStream fs = new FileStream(compressedFileName, FileMode.Create))
            {
                using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    arch.CreateEntryFromFile(fileName, newFileNameEntryInArchive, CompressionLevel.Optimal);
                }
            }
            System.IO.File.Delete(fileName);
            return compressedFileName;
        }

        private string GenerateCompressedFile()
        {
            try
            {
                string csvFolder = _configuration["ExportPath"].ToString();

                if (!Directory.Exists(csvFolder))
                {
                    Directory.CreateDirectory(csvFolder);
                }

                string fullSearchFileNamePrefix = $"FullDataExport_{Guid.NewGuid()}_{DateTime.Now.ToString("yyyy-MM-dd_HH+mm+ss")}.csv";

                string csvFilePath = Path.Combine(csvFolder, fullSearchFileNamePrefix);

                int partCount = 0;
                using (var bl = new PartsBL() { ConnectionString = _configuration.GetConnectionString("MainConnection") })
                {
                    var parts = bl.GetAllPartsAsOrderedList();
                    partCount = parts.Count;
                    using (var writer = new StreamWriter(csvFilePath,false,Encoding.UTF8))
                    {
                        writer.WriteLine(AutoPart.CSVHeader);
                        foreach (var part in parts)
                        {
                            writer.WriteLine(part.CSVWithDCQuantityOnly());
                        }
                    }
                }
                return CompressFile(csvFilePath);
            }
            catch(Exception ex)
            {
                LogError(ex);
            }
            return string.Empty;

        }

        // GET api/values
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string error = string.Empty;
            try
            {
                var dataFilePath = await Task.Run(() => GenerateCompressedFile());
                var fileName = Path.GetFileName(dataFilePath);
                return PhysicalFile(dataFilePath, "application/octet-stream", fileName); // returns a FileStreamResult
            }
            catch(Exception ex)
            {
                LogError(ex);
            }
            return NotFound();
        }

        
    }
}
