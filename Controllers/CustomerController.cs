using ClosedXML.Excel;
using EcoLaundry.Data;
using EcoLaundry.Entities;
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
                .ThenInclude(x => x.Items).ThenInclude(x => x.LaundryCategory)
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
    public async Task<IActionResult> ExportCustomersExcel()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();

        if (customers.Count == 0)
        {
            return BadRequest("Нема клиенти за export.");
        }

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Клиенти");

        // =====================================================
        // HEADER
        // =====================================================

        worksheet.Cell(1, 1).Value = "First Name";
        worksheet.Cell(1, 2).Value = "Last Name";
        worksheet.Cell(1, 3).Value = "Phone";
        worksheet.Cell(1, 4).Value = "Notes";

        var header = worksheet.Range(1, 1, 1, 4);

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        // =====================================================
        // DATA
        // =====================================================

        int row = 2;

        foreach (var customer in customers)
        {
            worksheet.Cell(row, 1).Value = customer.FirstName;
            worksheet.Cell(row, 2).Value = customer.LastName;
            worksheet.Cell(row, 3).Value = customer.Phone;
            worksheet.Cell(row, 4).Value = customer.Notes ?? "";

            row++;
        }

        // =====================================================
        // FORMAT
        // =====================================================

        worksheet.Columns().AdjustToContents();

        // Notes column
        worksheet.Column(4).Width = 40;
        worksheet.Column(4).Style.Alignment.WrapText = true;

        // =====================================================
        // RETURN FILE
        // =====================================================

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Customers_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
        );
    }

    // POST: Customers/Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {

        if (!ModelState.IsValid)
            return View(customer);

        var phone = customer.Phone.Trim();
        var firstName = customer.FirstName.Trim().ToLower();
        var lastName = customer.LastName.Trim().ToLower();

        var exists = await _context.Customers.AnyAsync(c =>
            c.Phone.Trim() == phone ||
            (c.FirstName.Trim().ToLower() == firstName &&
             c.LastName.Trim().ToLower() == lastName));

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "Клиентот веќе постои.");
            return View(customer);
        }

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
    public IActionResult Test()
    {
        return Content($"""
Authenticated: {User.Identity?.IsAuthenticated}
User: {User.Identity?.Name}
Admin: {User.IsInRole("Admin")}
Member: {User.IsInRole("Member")}
""");
    }
    // POST Delete

    private bool CustomerExists(int id)
    {
        return _context.Customers.Any(x => x.Id == id);
    }

}