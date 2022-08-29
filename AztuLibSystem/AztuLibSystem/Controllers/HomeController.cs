using AztuLibSystem.DAL;
using AztuLibSystem.Models;
using AztuLibSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AztuLibSystem.Controllers
{
    public class HomeController : Controller
    {
        private AppDbContext _context;
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            HomeVM homeVM = new HomeVM();
            homeVM.sliders = _context.Sliders.ToList();
            homeVM.pageIntro = _context.PageIntros.FirstOrDefault();
            homeVM.categories = _context.Categories.ToList();

         
            return View(homeVM);
        }
    }
}
