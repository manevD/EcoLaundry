namespace EcoLaundry.Entities
{
    public class LaundryCategory
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public bool Active { get; set; } = true;

        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
    }
}
