using EcoLaundry.Data;
using EcoLaundry.Entities;
using EcoLaundry.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EcoLaundry.Controllers;


[Authorize(Roles = "Admin,Member")]
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;


    public StatisticsController(ApplicationDbContext context)
    {
        _context = context;
    }



    public async Task<IActionResult> Index(
     DateTime? from,
     DateTime? to)
    {
        var query =
            _context.OrderItems
                .AsNoTracking()
                .Where(x =>
                    x.Order.PaymentStatus == PaymentStatus.Paid);


        if (from.HasValue)
        {
            query =
                query.Where(x =>
                    x.Order.ReceivedDate >= from.Value.Date);
        }


        if (to.HasValue)
        {
            var toDate =
                to.Value.Date.AddDays(1);

            query =
                query.Where(x =>
                    x.Order.ReceivedDate < toDate);
        }



        var result =
            await query

            .GroupBy(x => new
            {
                x.LaundryCategoryId,
                x.LaundryCategory.Name
            })

            .Select(x =>
                new CategoryStatisticViewModel
                {
                    CategoryId =
                        x.Key.LaundryCategoryId,


                    CategoryName =
                        x.Key.Name,


                    OrderCount =
                        x.Select(a => a.OrderId)
                         .Distinct()
                         .Count(),


                    Quantity =
                        x.Sum(a => a.Quantity),


                    Total =
                        x.Sum(a =>
                            a.Quantity * a.Price)
                })


            .OrderByDescending(x =>
                x.Total)


            .ToListAsync();



        ViewBag.From = from;
        ViewBag.To = to;


        return View(result);
    }






    public async Task<IActionResult> Details(
        int id,
        DateTime? from,
        DateTime? to)
    {


        var query =
            _context.OrderItems

            .Include(x => x.Order)
                .ThenInclude(x => x.Customer)

            .Include(x => x.LaundryCategory)

            .Where(x =>
                x.LaundryCategoryId == id);



        if (from.HasValue)
        {
            query =
                query.Where(x =>
                    x.Order.ReceivedDate.Date >=
                    from.Value.Date);
        }



        if (to.HasValue)
        {
            query =
                query.Where(x =>
                    x.Order.ReceivedDate.Date <=
                    to.Value.Date);
        }

        var items =
            await query

            .OrderByDescending(x =>
                x.Order.ReceivedDate)

            .ToListAsync();



        return View(items);
    }
}