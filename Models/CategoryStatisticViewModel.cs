namespace EcoLaundry.ViewModels;

public class CategoryStatisticViewModel
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public int OrderCount { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}