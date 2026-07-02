using EcoLaundry.Data;
using EcoLaundry.Entities;
using EcoLaundry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLaundry.Controllers;

[Authorize(Roles = "Admin,Member")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }


    // GET: Customers
    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Redirect("/Identity/Account/Login");
        }

        var customers = await _context.Customers
            .Include(x => x.Orders)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return View(customers);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();


        var customer = await _context.Customers
            .Include(x => x.Orders)
                .ThenInclude(x => x.Items).ThenInclude(x=>x.LaundryCategory)
            .FirstOrDefaultAsync(x => x.Id == id);


        if (customer == null)
            return NotFound();


        return View(customer);
    }



    // GET: Customers/Create
    public IActionResult Create()
    {
        return View();
    }


    // POST: Customers/Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {

        if (!ModelState.IsValid)
            return View(customer);



        customer.CreatedAt = DateTime.Now;


        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();


        return RedirectToAction(nameof(Index));
    }





    // GET: Customers/Edit/5

    public async Task<IActionResult> Edit(int? id)
    {

        if (id == null)
            return NotFound();



        var customer =
            await _context.Customers.FindAsync(id);



        if (customer == null)
            return NotFound();



        return View(customer);
    }





    // POST: Customers/Edit


    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Edit(
        int id,
        Customer customer)
    {

        if (id != customer.Id)
            return NotFound();



        if (!ModelState.IsValid)
            return View(customer);



        try
        {
            _context.Update(customer);

            await _context.SaveChangesAsync();
        }

        catch (DbUpdateConcurrencyException)
        {
            if (!CustomerExists(customer.Id))
                return NotFound();

            throw;
        }



        return RedirectToAction(nameof(Index));
    }
    // GET: Customers/Delete/5

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // POST Delete

    private bool CustomerExists(int id)
    {
        return _context.Customers.Any(x => x.Id == id);
    }

}