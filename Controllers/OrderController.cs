using EcoLaundry.Data;
using EcoLaundry.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoLaundry.Controllers;


[Authorize(Roles = "Admin,Member")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;


    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }



    // =============================
    // INDEX
    // =============================


    public async Task<IActionResult> Index(string filter = "active")
    {
        var query =
            _context.Orders

            .Include(x => x.Customer)

            .Include(x => x.Items)
                .ThenInclude(x => x.LaundryCategory)

            .AsQueryable();



        if (filter == "active")
        {
            query =
                query.Where(x =>
                    x.Status != OrderStatus.Finished);
        }



        if (filter == "finished")
        {
            query =
                query.Where(x =>
                    x.Status == OrderStatus.Finished);
        }




        var orders =
            await query

            .OrderBy(x => x.Status == OrderStatus.Finished)

            .ThenByDescending(x => x.IsExpress)

            .ThenBy(x => x.ReceivedDate)

            .ToListAsync();



        ViewBag.Filter = filter;



        return View(orders);
    }









    // =============================
    // DETAILS
    // =============================


    public async Task<IActionResult> Details(int id)
    {
        var order =
            await _context.Orders

            .Include(x => x.Customer)

            .Include(x => x.Items)
                .ThenInclude(x => x.LaundryCategory)

            .FirstOrDefaultAsync(x => x.Id == id);



        if (order == null)
            return NotFound();



        return View(order);
    }





    // =============================
    // CREATE GET
    // =============================


    public async Task<IActionResult> Create(int? customerId)
    {
        if (customerId.HasValue)
        {
            ViewBag.SelectedCustomer =
          await _context.Customers
        .Where(x => x.Id == customerId.Value)
        .Select(x => new
        {
            Id = x.Id,

            Display =
                x.FirstName + " " +
                x.LastName +
                " - " +
                x.Phone
        })
        .FirstOrDefaultAsync();
        }
       
            await LoadData(true);
        

        var order = new Order
        {
            CustomerId =
                customerId ?? 0,


            ReceivedDate =
                DateTime.Now,

            ExpectedFinishDate = DateTime.Now.AddDays(2),
            Status =
                OrderStatus.Received,


            PaymentStatus =
                PaymentStatus.Unpaid
        };



        return View(order);
    }









    // =============================
    // CREATE POST
    // =============================



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    Order order,
    int[] categoryIds,
    int[] quantities,
    int[] prices)
    {
        ModelState.Remove("Customer");
        ModelState.Remove("OrderNumber");

        if (!ModelState.IsValid)
        {
            await LoadData(true);

            return View(order);
        }
        order.OrderNumber =
            DateTime.Now.ToString("yyyyMMddHHmm");

        order.Items =
            new List<OrderItem>();

        for (int i = 0; i < categoryIds.Length; i++)
        {
            order.Items.Add(
                new OrderItem
                {
                    LaundryCategoryId = categoryIds[i],

                    Quantity =
                        quantities.Length > i
                        ? quantities[i]
                        : 1,

                    Price =
                        prices.Length > i
                        ? prices[i]
                        : 0
                });
        }

        order.TotalPrice =
      order.Items.Sum(x => x.Price * x.Quantity);




        _context.Orders.Add(order);



        await _context.SaveChangesAsync();




        return RedirectToAction(
            nameof(Details),
            new { id = order.Id });
    }


    // =============================
    // EDIT GET
    // =============================


    public async Task<IActionResult> Edit(int id)
    {
        var order =
            await _context.Orders

            .Include(x => x.Items)

            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
            return NotFound();

        await LoadData(true);

        return View(order);
    }






    // =============================
    // EDIT POST
    // =============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int id,
    Order order,
    int[] categoryIds,
    int[] quantities,
    int[] prices)
    {
        if (id != order.Id)
            return NotFound();

        var dbOrder =
            await _context.Orders
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id);
        if (order.PickedUp)
        {
            dbOrder.Status = OrderStatus.Finished;
        }
        else
        {
            dbOrder.Status =
         order.Status;
        }



        dbOrder.PaymentStatus =
            order.PaymentStatus;


        dbOrder.IsExpress =
            order.IsExpress;


        dbOrder.CustomerContacted =
            order.CustomerContacted;


        dbOrder.PickedUp =
            order.PickedUp;


        dbOrder.ExpectedFinishDate =
            order.ExpectedFinishDate;


        dbOrder.FinishedDate =
            order.FinishedDate;


        dbOrder.Notes =
            order.Notes;






        // REMOVE OLD ITEMS


        dbOrder.Items.Clear();

        // ADD NEW ITEMS
        for (int i = 0; i < categoryIds.Length; i++)
        {
            dbOrder.Items.Add(
                new OrderItem
                {
                    LaundryCategoryId =
                        categoryIds[i],
                    Quantity =
                        quantities.Length > i
                        ? quantities[i]
                        : 1,

                    Price =
                        prices.Length > i
                        ? prices[i]
                        : 0
                });

        }
        dbOrder.TotalPrice =
            dbOrder.Items.Sum(x => x.Price * x.Quantity);





        await _context.SaveChangesAsync();





        return RedirectToAction(
            nameof(Details),
            new { id });
    }










    // =============================
    // HELPERS
    // =============================



    private async Task LoadData(bool loadCustomers)
    {

        if (loadCustomers)
        {
            ViewBag.Customers =
            await _context.Customers

            .OrderBy(x => x.FirstName)

            .Select(x => new
            {

                x.Id,


                Display =
                    x.FirstName +
                    " " +
                    x.LastName +
                    " - " +
                    x.Phone

            })

            .ToListAsync();

        }
        ViewBag.Categories =
           await _context.LaundryCategories

           .Where(x => x.Active)

           .OrderBy(x => x.Name)

           .ToListAsync();
    }
}