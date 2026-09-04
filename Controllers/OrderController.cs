using ClosedXML.Excel;
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
                    x.Status == OrderStatus.Finished && !x.PickedUp);
        }

        if (filter == "delivered")
        {
            query =
               query.Where(x => x.PickedUp);
        }

        if (filter == "cancelled")
        {
            query =
                query.Where(x =>
                    x.Status == OrderStatus.Cancelled && !x.PickedUp);
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

        if (!ModelState.IsValid || categoryIds == null || categoryIds.Length == 0)
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
    private string LatinToCyrillic(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.ToLower();

        return text
            .Replace("sh", "ш")
            .Replace("zh", "ж")
            .Replace("ch", "ч")
            .Replace("lj", "љ")
            .Replace("nj", "њ")
            .Replace("gj", "ѓ")
            .Replace("kj", "ќ")
            .Replace("dz", "ѕ")
            .Replace("dj", "џ")

            .Replace("a", "а")
            .Replace("b", "б")
            .Replace("v", "в")
            .Replace("g", "г")
            .Replace("d", "д")
            .Replace("e", "е")
            .Replace("z", "з")
            .Replace("i", "и")
            .Replace("j", "ј")
            .Replace("k", "к")
            .Replace("l", "л")
            .Replace("m", "м")
            .Replace("n", "н")
            .Replace("o", "о")
            .Replace("p", "п")
            .Replace("r", "р")
            .Replace("s", "с")
            .Replace("t", "т")
            .Replace("u", "у")
            .Replace("f", "ф")
            .Replace("h", "х")
            .Replace("c", "ц");
    }
    private string CyrillicToLatin(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.ToLower();

        var map = new Dictionary<char, string>
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "g",
            ['д'] = "d",
            ['ѓ'] = "gj",
            ['е'] = "e",
            ['ж'] = "zh",
            ['з'] = "z",
            ['ѕ'] = "dz",
            ['и'] = "i",
            ['ј'] = "j",
            ['к'] = "k",
            ['л'] = "l",
            ['љ'] = "lj",
            ['м'] = "m",
            ['н'] = "n",
            ['њ'] = "nj",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['ќ'] = "kj",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "h",
            ['ц'] = "c",
            ['ч'] = "ch",
            ['џ'] = "dj",
            ['ш'] = "sh"
        };


        var result = "";

        foreach (var c in text)
        {
            result +=
                map.TryGetValue(c, out var value)
                    ? value
                    : c.ToString();
        }


        return result;
    }
    public async Task<IActionResult> ExportExcel(
       string filter = "active",
       string? ids = null)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.LaundryCategory);


        // =====================================================
        // АКО ИМА IDS -> EXPORT САМО НА ВИДЛИВИТЕ НАРАЧКИ
        // =====================================================

        if (!string.IsNullOrWhiteSpace(ids))
        {
            var orderIds = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id =>
                {
                    return int.TryParse(id, out var result)
                        ? result
                        : (int?)null;
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();


            if (orderIds.Count == 0)
            {
                return BadRequest("Нема валидни нарачки за export.");
            }


            query = query.Where(o =>
                orderIds.Contains(o.Id));
        }
        else
        {
            // =================================================
            // АКО НЕМА IDS -> КОРИСТИ ГО NORMALНИОТ FILTER
            // =================================================

            switch (filter)
            {
                case "active":

                    query = query.Where(o =>
                        !o.PickedUp &&
                        (
                            o.Status == OrderStatus.Received ||
                            o.Status == OrderStatus.InProgress
                        ));

                    break;


                case "finished":

                    query = query.Where(o =>
                        !o.PickedUp &&
                        o.Status == OrderStatus.Finished);

                    break;


                case "delivered":

                    query = query.Where(o =>
                        o.PickedUp);

                    break;


                case "cancelled":

                    query = query.Where(o =>
                        o.Status == OrderStatus.Cancelled);

                    break;


                case "all":

                    // Сите нарачки
                    break;
            }
        }


        // =====================================================
        // LOAD ORDERS
        // =====================================================

        var orders = await query
            .OrderByDescending(o => o.ReceivedDate)
            .ToListAsync();


        if (orders.Count == 0)
        {
            return BadRequest("Нема нарачки за export.");
        }


        // =====================================================
        // CREATE EXCEL
        // =====================================================

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Нарачки");


        // =====================================================
        // HEADER
        // =====================================================

        worksheet.Cell(1, 1).Value = "Број";
        worksheet.Cell(1, 2).Value = "Клиент";
        worksheet.Cell(1, 3).Value = "Телефон";
        worksheet.Cell(1, 4).Value = "Примено";
        worksheet.Cell(1, 5).Value = "Очекувано";
        worksheet.Cell(1, 6).Value = "Завршено";
        worksheet.Cell(1, 7).Value = "Статус";
        worksheet.Cell(1, 8).Value = "Подигнато";
        worksheet.Cell(1, 9).Value = "Express";
        worksheet.Cell(1, 10).Value = "Ставки";
        worksheet.Cell(1, 11).Value = "Вкупно";
        worksheet.Cell(1, 12).Value = "Забелешка";


        // Header style

        var header =
            worksheet.Range(1, 1, 1, 12);

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;


        // =====================================================
        // DATA
        // =====================================================

        int row = 2;


        foreach (var order in orders)
        {
            // -------------------------------------------------
            // БРОЈ
            // -------------------------------------------------

            worksheet.Cell(row, 1).Value =
                order.OrderNumber ?? "-";


            // -------------------------------------------------
            // КЛИЕНТ
            // -------------------------------------------------

            worksheet.Cell(row, 2).Value =
                order.Customer != null
                    ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                    : "Непознат клиент";


            // -------------------------------------------------
            // ТЕЛЕФОН
            // -------------------------------------------------

            worksheet.Cell(row, 3).Value =
                order.Customer?.Phone ?? "";


            // -------------------------------------------------
            // ПРИМЕНО
            // -------------------------------------------------

            worksheet.Cell(row, 4).Value =
                order.ReceivedDate;

            worksheet.Cell(row, 4)
                .Style.DateFormat.Format =
                "dd.MM.yyyy HH:mm";


            // -------------------------------------------------
            // ОЧЕКУВАНО
            // -------------------------------------------------

            if (order.ExpectedFinishDate.HasValue)
            {
                worksheet.Cell(row, 5).Value =
                    order.ExpectedFinishDate.Value;

                worksheet.Cell(row, 5)
                    .Style.DateFormat.Format =
                    "dd.MM.yyyy HH:mm";
            }


            // -------------------------------------------------
            // ЗАВРШЕНО
            // -------------------------------------------------

            if (order.FinishedDate.HasValue)
            {
                worksheet.Cell(row, 6).Value =
                    order.FinishedDate.Value;

                worksheet.Cell(row, 6)
                    .Style.DateFormat.Format =
                    "dd.MM.yyyy HH:mm";
            }


            // -------------------------------------------------
            // STATUS
            // -------------------------------------------------

            worksheet.Cell(row, 7).Value =
                order.Status switch
                {
                    OrderStatus.Received =>
                        "Примено",

                    OrderStatus.InProgress =>
                        "Во обработка",

                    OrderStatus.Finished =>
                        "Завршено",

                    OrderStatus.Cancelled =>
                        "Откажано",

                    _ =>
                        order.Status.ToString()
                };


            // -------------------------------------------------
            // ПОДИГНАТО
            // -------------------------------------------------

            worksheet.Cell(row, 8).Value =
                order.PickedUp
                    ? "ДА"
                    : "НЕ";


            // -------------------------------------------------
            // EXPRESS
            // -------------------------------------------------

            worksheet.Cell(row, 9).Value =
                order.IsExpress
                    ? "ДА"
                    : "НЕ";


            // -------------------------------------------------
            // СТАВКИ
            // -------------------------------------------------

            var items =
                order.Items?
                    .Select(item =>
                        $"{item.LaundryCategory?.Name ?? "Непознато"} " +
                        $"x{item.Quantity} @ {item.Price} ден")
                    ?? Enumerable.Empty<string>();


            worksheet.Cell(row, 10).Value =
                string.Join(", ", items);


            // -------------------------------------------------
            // ВКУПНО
            // -------------------------------------------------

            worksheet.Cell(row, 11).Value =
                order.TotalPrice ?? 0;


            // -------------------------------------------------
            // ЗАБЕЛЕШКА
            // -------------------------------------------------

            worksheet.Cell(row, 12).Value =
                order.Notes ?? "";


            row++;
        }


        // =====================================================
        // FORMAT
        // =====================================================

        worksheet.Columns()
            .AdjustToContents();


        // Максимална ширина

        worksheet.Column(1).Width = 15;
        worksheet.Column(2).Width = 25;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 20;
        worksheet.Column(5).Width = 20;
        worksheet.Column(6).Width = 20;
        worksheet.Column(7).Width = 18;
        worksheet.Column(8).Width = 12;
        worksheet.Column(9).Width = 12;
        worksheet.Column(10).Width = 60;
        worksheet.Column(11).Width = 15;
        worksheet.Column(12).Width = 40;


        // Wrap text

        worksheet.Column(10)
            .Style.Alignment.WrapText = true;

        worksheet.Column(12)
            .Style.Alignment.WrapText = true;


        // Vertical alignment

        worksheet.Rows()
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;


        // Freeze header

        worksheet.SheetView.FreezeRows(1);


        // Auto filter

        worksheet.Range(
            1,
            1,
            row - 1,
            12)
            .SetAutoFilter();


        // =====================================================
        // FILE
        // =====================================================

        using var stream =
            new MemoryStream();


        workbook.SaveAs(stream);

        stream.Position = 0;


        // =====================================================
        // FILE NAME
        // =====================================================

        string filterName =
            filter switch
            {
                "active" => "Aktivni",
                "finished" => "Zavrseni",
                "delivered" => "Podignati",
                "cancelled" => "Otkazani",
                "all" => "Site",
                _ => "Naracki"
            };


        var fileName =
            $"Naracki_{filterName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";


        // =====================================================
        // DOWNLOAD
        // =====================================================

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}