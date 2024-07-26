// Models/FileInformation.cs

using System.ComponentModel.DataAnnotations;

namespace KutesSearch.Models
{
    public class FileInformation
    {
        public int doc_id { get; set; }
        
        [Display(Name = "Dosya Yolu")]
        public string FilePath { get; set; }
        
        [Display(Name = "Dosya İçeriği")]
        public string FileContent { get; set; }
        
        [Display(Name = "Dosya Adı")]
        public string Filename { get; set; }

        [Display(Name = "Dosya Tipi")]
        public string FileType { get; set; }
    }
}
