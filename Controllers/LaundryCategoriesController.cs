using EcoLaundry.Data;
using EcoLaundry.Entities;
using EcoLaundry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLaundry.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LaundryCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;


        public LaundryCategoriesController(
            ApplicationDbContext context)
        {
            _context = context;
        }




        // GET: LaundryCategories

        public async Task<IActionResult> Index()
        {
            var categories =
                await _context.LaundryCategories
                    .OrderBy(x => x.Name)
                    .ToListAsync();


            return View(categories);
        }







        // GET: LaundryCategories/Create

        public IActionResult Create()
        {
            var category = new LaundryCategory
            {
                Active = true
            };


            return View(category);
        }







        // POST: LaundryCategories/Create

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(
            LaundryCategory category)
        {

            if (!ModelState.IsValid)
            {
                return View(category);
            }



            _context.LaundryCategories.Add(category);


            await _context.SaveChangesAsync();



            return RedirectToAction(nameof(Index));
        }









        // GET: LaundryCategories/Edit/5

        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }



            var category =
                await _context.LaundryCategories
                    .FindAsync(id);



            if (category == null)
            {
                return NotFound();
            }



            return View(category);
        }








        // POST: LaundryCategories/Edit

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(
            int id,
            LaundryCategory category)
        {

            if (id != category.Id)
            {
                return NotFound();
            }




            if (!ModelState.IsValid)
            {
                return View(category);
            }



            try
            {

                _context.Update(category);


                await _context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException)
            {

                if (!CategoryExists(category.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }


        private bool CategoryExists(int id)
        {
            return _context.LaundryCategories
                .Any(x => x.Id == id);
        }
    }
}