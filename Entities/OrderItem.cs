namespace EcoLaundry.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;
        public int LaundryCategoryId { get; set; }

        public LaundryCategory LaundryCategory { get; set; }
            = null!;
        public int Price { get; set; }
        public int Quantity { get; set; }

        public string? Notes { get; set; }
    }
}
