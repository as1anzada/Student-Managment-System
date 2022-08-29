using AztuLibSystem.DAL;
using AztuLibSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AztuLibSystem.Controllers
{
    public class ProductController : Controller
    {
        private AppDbContext _context;
        public ProductController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.ProductCount = _context.Products.Count();
            List<Product> products = _context.Products.Include(p => p.Category).Take(2).ToList();
            return View(products);
        }
        public IActionResult LoadMore(int skip)
        {
            List<Product> products = _context.Products.Include(p => p.Category).Skip(skip).Take(2).ToList();
            return PartialView("_ProductPartial", products);
        }
        public IActionResult SearchProduct(string search)
        {
            List<Product> products = _context.Products.Where(p => p.Name.ToLower().Contains(search.ToLower())).ToList();
            return PartialView("_Search", products);
        }
    }
}
