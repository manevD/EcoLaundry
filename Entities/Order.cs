namespace EcoLaundry.Entities
{
    public class Order
    {
        public int Id { get; set; }


        public string OrderNumber { get; set; } = null!;


        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;


        public DateTime ReceivedDate { get; set; }
            = DateTime.Now;


        public DateTime? ExpectedFinishDate { get; set; }


        public DateTime? FinishedDate { get; set; }


        public bool IsExpress { get; set; }


        public OrderStatus Status { get; set; }
            = OrderStatus.Received;


        public bool CustomerContacted { get; set; }


        public bool PickedUp { get; set; }

        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Unpaid;

        // MKD - denari
        public int? TotalPrice { get; set; }

        public string? Notes { get; set; }

        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
    }
}
