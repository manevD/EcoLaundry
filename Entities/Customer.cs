namespace EcoLaundry.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string? Email { get; set; }

        public string? Notes { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        // Relations

        public ICollection<Order> Orders { get; set; }
            = new List<Order>();
    }
}
