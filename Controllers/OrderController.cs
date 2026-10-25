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
        var query = _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(x => x.LaundryCategory)
            .AsQueryable();

        switch (filter?.ToLowerInvariant())
        {
            case "active":
                query = query.Where(x =>
                    x.Status != OrderStatus.Finished);
                break;

            case "finished":
                query = query.Where(x =>
                    x.Status == OrderStatus.Finished &&
                    !x.PickedUp);
                break;

            case "delivered":
                query = query.Where(x => x.PickedUp);
                break;

            case "cancelled":
                query = query.Where(x =>
                    x.Status == OrderStatus.Cancelled &&
                    !x.PickedUp);
                break;

            case "all":
                // Без филтер: прикажи ги сите нарачки.
                break;

            default:
                filter = "active";
                query = query.Where(x =>
                    x.Status != OrderStatus.Finished);
                break;
        }

        var orders = await query
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
        var order = await _context.Orders
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
            ViewBag.SelectedCustomer = await _context.Customers
                .Where(x => x.Id == customerId.Value)
                .Select(x => new
                {
                    Id = x.Id,
                    Display = x.FirstName + " " +
                              x.LastName + " - " +
                              x.Phone
                })
                .FirstOrDefaultAsync();
        }

        await LoadData(true);

        var order = new Order
        {
            CustomerId = customerId ?? 0,
            ReceivedDate = DateTime.Now,
            ExpectedFinishDate = DateTime.Now.AddDays(2),
            Status = OrderStatus.Received,
            PaymentStatus = PaymentStatus.Unpaid
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

        if (!ModelState.IsValid ||
            categoryIds == null ||
            categoryIds.Length == 0)
        {
            await LoadData(true);
            return View(order);
        }

        order.OrderNumber = DateTime.Now.ToString("yyyyMMddHHmm");
        order.Items = new List<OrderItem>();

        for (int i = 0; i < categoryIds.Length; i++)
        {
            order.Items.Add(new OrderItem
            {
                LaundryCategoryId = categoryIds[i],
                Quantity = quantities.Length > i
                    ? quantities[i]
                    : 1,
                Price = prices.Length > i
                    ? prices[i]
                    : 0
            });
        }

        order.TotalPrice = order.Items.Sum(x => x.Price * x.Quantity);

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
        var order = await _context.Orders
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

        var dbOrder = await _context.Orders
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id);

        if (order.PickedUp)
        {
            dbOrder.Status = OrderStatus.Finished;
        }
        else
        {
            dbOrder.Status = order.Status;
        }

        dbOrder.PaymentStatus = order.PaymentStatus;
        dbOrder.IsExpress = order.IsExpress;
        dbOrder.CustomerContacted = order.CustomerContacted;
        dbOrder.PickedUp = order.PickedUp;
        dbOrder.ExpectedFinishDate = order.ExpectedFinishDate;
        dbOrder.FinishedDate = order.FinishedDate;
        dbOrder.Notes = order.Notes;

        // REMOVE OLD ITEMS
        dbOrder.Items.Clear();

        // ADD NEW ITEMS
        for (int i = 0; i < categoryIds.Length; i++)
        {
            dbOrder.Items.Add(new OrderItem
            {
                LaundryCategoryId = categoryIds[i],
                Quantity = quantities.Length > i
                    ? quantities[i]
                    : 1,
                Price = prices.Length > i
                    ? prices[i]
                    : 0
            });
        }

        dbOrder.TotalPrice = dbOrder.Items.Sum(x => x.Price * x.Quantity);

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
            ViewBag.Customers = await _context.Customers
                .OrderBy(x => x.FirstName)
                .Select(x => new
                {
                    x.Id,
                    Display = x.FirstName + " " +
                              x.LastName + " - " +
                              x.Phone
                })
                .ToListAsync();
        }

        ViewBag.Categories = await _context.LaundryCategories
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
            result += map.TryGetValue(c, out var value)
                ? value
                : c.ToString();
        }

        return result;
    }

    // =============================
    // EXPORT EXCEL
    // =============================

    public async Task<IActionResult> ExportExcel(
        string filter = "active",
        string? ids = null)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.LaundryCategory);

        // Ако има IDs од пребарувањето,
        // извези ги само тие нарачки.
        if (!string.IsNullOrWhiteSpace(ids))
        {
            var orderIds = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var parsed)
                    ? (int?)parsed
                    : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (orderIds.Count == 0)
                return BadRequest(
                    "Нема валидни нарачки за Excel export.");

            query = query.Where(o => orderIds.Contains(o.Id));
        }
        else
        {
            // Без IDs: користи го избраниот филтер.
            switch (filter?.ToLowerInvariant())
            {
                case "active":
                    query = query.Where(o =>
                        o.Status != OrderStatus.Finished);
                    break;

                case "finished":
                    query = query.Where(o =>
                        o.Status == OrderStatus.Finished &&
                        !o.PickedUp);
                    break;

                case "delivered":
                    query = query.Where(o => o.PickedUp);
                    break;

                case "cancelled":
                    query = query.Where(o =>
                        o.Status == OrderStatus.Cancelled &&
                        !o.PickedUp);
                    break;

                case "all":
                    // Сите нарачки, без ограничување.
                    break;

                default:
                    filter = "active";
                    query = query.Where(o =>
                        o.Status != OrderStatus.Finished);
                    break;
            }
        }

        var orders = await query
            .OrderByDescending(o => o.ReceivedDate)
            .ToListAsync();

        if (orders.Count == 0)
            return BadRequest("Нема нарачки за Excel export.");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Нарачки");

        string[] headers =
        {
            "Број",
            "Клиент",
            "Телефон",
            "Примено",
            "Очекувано",
            "Завршено",
            "Статус",
            "Подигнато",
            "Express",
            "Ставки",
            "Вкупно",
            "Забелешка"
        };

        for (int col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        var header = worksheet.Range(1, 1, 1, headers.Length);

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        int row = 2;

        foreach (var order in orders)
        {
            worksheet.Cell(row, 1).Value =
                order.OrderNumber ?? "-";

            worksheet.Cell(row, 2).Value =
                order.Customer != null
                    ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                    : "Непознат клиент";

            worksheet.Cell(row, 3).Value =
                order.Customer?.Phone ?? "";

            worksheet.Cell(row, 4).Value = order.ReceivedDate;
            worksheet.Cell(row, 4).Style.DateFormat.Format =
                "dd.MM.yyyy HH:mm";

            if (order.ExpectedFinishDate.HasValue)
            {
                worksheet.Cell(row, 5).Value =
                    order.ExpectedFinishDate.Value;

                worksheet.Cell(row, 5).Style.DateFormat.Format =
                    "dd.MM.yyyy HH:mm";
            }

            if (order.FinishedDate.HasValue)
            {
                worksheet.Cell(row, 6).Value =
                    order.FinishedDate.Value;

                worksheet.Cell(row, 6).Style.DateFormat.Format =
                    "dd.MM.yyyy HH:mm";
            }

            worksheet.Cell(row, 7).Value = order.Status switch
            {
                OrderStatus.Received => "Примено",
                OrderStatus.InProgress => "Во обработка",
                OrderStatus.Finished => "Завршено",
                OrderStatus.Cancelled => "Откажано",
                _ => order.Status.ToString()
            };

            worksheet.Cell(row, 8).Value =
                order.PickedUp ? "ДА" : "НЕ";

            worksheet.Cell(row, 9).Value =
                order.IsExpress ? "ДА" : "НЕ";

            var items = order.Items?
                .Select(item =>
                    $"{item.LaundryCategory?.Name ?? "Непознато"} " +
                    $"x{item.Quantity} @ {item.Price} ден")
                ?? Enumerable.Empty<string>();

            worksheet.Cell(row, 10).Value =
                string.Join(", ", items);

            worksheet.Cell(row, 11).Value =
                order.TotalPrice ?? 0;

            worksheet.Cell(row, 12).Value =
                order.Notes ?? "";

            row++;
        }

        worksheet.Columns().AdjustToContents();

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

        worksheet.Column(10).Style.Alignment.WrapText = true;
        worksheet.Column(12).Style.Alignment.WrapText = true;

        worksheet.Rows().Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        worksheet.SheetView.FreezeRows(1);
        worksheet.Range(1, 1, row - 1, 12).SetAutoFilter();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);
        stream.Position = 0;

        string filterName = filter?.ToLowerInvariant() switch
        {
            "active" => "Aktivni",
            "finished" => "Zavrseni",
            "delivered" => "Podignati",
            "cancelled" => "Otkazani",
            "all" => "Site",
            _ => "Naracki"
        };

        string fileName =
            $"Naracki_{filterName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}