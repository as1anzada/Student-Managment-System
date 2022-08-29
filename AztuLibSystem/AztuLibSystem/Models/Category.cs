using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AztuLibSystem.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage ="don't empty"), StringLength(10,ErrorMessage ="10dan Yuxari Ola Bilmez")]
        public string Name { get; set; }

        [Required(ErrorMessage = "don't empty"), MaxLength(50, ErrorMessage = "50dan Yuxari Ola Bilmez")]
        public string  Desc { get; set; }

        public IEnumerable<Product> Producs { get; set; }
    }
}
