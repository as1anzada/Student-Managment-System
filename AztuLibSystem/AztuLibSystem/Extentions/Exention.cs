using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AztuLibSystem.Extentions
{
    public static class Exention
    {
        public static bool IsImage(this IFormFile file)
        {
            return file.ContentType.Contains("image");
        }
        public static bool ImageSize(this IFormFile file, int kb)
        {
            return file.Length / 1024 > kb;
        }

        public static async Task< string> SaveImage(this IFormFile file, IWebHostEnvironment env, string folder)
        {
            string path = env.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + file.FileName;
            string result = Path.Combine(path, folder,  fileName);
            using (FileStream stream = new FileStream(result, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            };
            return fileName;
        }
    }
}
