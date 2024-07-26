// Controllers/SearchController.cs

using System.Collections.Generic;
using System.Linq;
using System.Text;
using KutesSearch.Data;
using KutesSearch.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KutesSearch.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SearchController> _logger;

        public SearchController(AppDbContext dbContext, ILogger<SearchController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SearchTerm = searchTerm;

            var searchResults = _dbContext.FileInformation
                .Where(f => f.FileContent.Contains(searchTerm))
                .AsEnumerable()
                .Select(f => new FileInformation
                {
                    doc_id = f.doc_id,
                    FilePath = f.FilePath,
                    Filename = f.Filename,
                    FileType = f.FileType,
                    FileContent = GetContentSnippet(f.FileContent, searchTerm)
                })
                .ToList();

            return View("Index", searchResults);
        }
        /*
        [HttpPost]
        public IActionResult Download(int docId)
        {
            var fileInformation = _dbContext.FileInformation.FirstOrDefault(f => f.doc_id == docId);
            if (fileInformation == null)
            {
                return NotFound(); // Eğer belirtilen DocId'ye sahip dosya bulunamazsa 404 hatası döndür
            }

            // Dosyayı indirme işlemi için gerekli kodu buraya ekleyin
            // Örneğin, dosyayı MemoryStream üzerinden kullanıcıya sunabilirsiniz
            // Bu örnekte FileContent'i kullanarak örnek bir işlem yapılacak

            byte[] fileBytes = Encoding.UTF8.GetBytes(fileInformation.FileContent); // Örnek olarak FileContent'i byte dizisine dönüştürdük

            return File(fileBytes, "text/plain", fileInformation.Filename); // Örnek olarak text/plain olarak dönüştürdük
        }
        */

        [HttpPost]
        public async Task<IActionResult> Download(int docId)
        {
            var fileInfo = await _dbContext.FileInformation.FindAsync(docId);
            if (fileInfo == null)
            {
                _logger.LogError("File information not found for docId: {DocId}", docId);
                return NotFound();
            }

            string filePath = fileInfo.FilePath;
            _logger.LogInformation("Trying to download file from path: {FilePath}", filePath);

            // Decode the file path
            string decodedFilePath = Uri.UnescapeDataString(filePath);

            if (!System.IO.File.Exists(decodedFilePath))
            {
                _logger.LogError("File not found at path: {FilePath}", decodedFilePath);
                return NotFound();
            }

            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(decodedFilePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(decodedFilePath), Path.GetFileName(decodedFilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to download the file: {FilePath}", decodedFilePath);
                return StatusCode(500, "Internal server error");
            }
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
        {
            { ".txt", "text/plain" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/vnd.ms-word" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".csv", "text/csv" }
        };
        }

        [HttpGet]
        public IActionResult GetSnippet(int docId, string searchTerm)
        {
            var fileInformation = _dbContext.FileInformation.FirstOrDefault(f => f.doc_id == docId);
            if (fileInformation == null || string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest();
            }

            var snippet = GetContentSnippet(fileInformation.FileContent, searchTerm);

            return Json(new { snippet });
        }

        private string GetContentSnippet(string content, string searchTerm)
        {
            const int contextWords = 100;
            var words = content.Split(' ');
            var searchIndex = content.IndexOf(searchTerm);
            if (searchIndex == -1) return string.Empty;

            var searchTermIndex = words.Select((word, index) => new { word, index })
                .Where(x => x.word.Contains(searchTerm))
                .Select(x => x.index)
                .FirstOrDefault();

            var start = searchTermIndex - contextWords > 0 ? searchTermIndex - contextWords : 0;
            var end = searchTermIndex + contextWords < words.Length ? searchTermIndex + contextWords : words.Length - 1;

            return string.Join(" ", words.Skip(start).Take(end - start + 1));
        }
    }
}
