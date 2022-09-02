namespace Bold_Meeting_Recordings.Models
{
    using Microsoft.AspNetCore.Http;
    using System.ComponentModel.DataAnnotations;

    public class FileUploadFormModel
    {
        [Required]
        [Display(Name = "Recording file")]
        public IFormFile FormFile { get; set; }

        [Display(Name = "Customer / Company")]
        public string Key { get; set; }

        [Display(Name = "Recording link")]
        public string UploadedFileUrl { get; set; }
    }
}
