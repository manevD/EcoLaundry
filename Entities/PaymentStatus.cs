namespace EcoLaundry.Entities
{
    using System.ComponentModel.DataAnnotations;

    public enum PaymentStatus
    {
        [Display(Name = "Неплатено")]
        Unpaid = 1,


        [Display(Name = "Платено")]
        Paid = 2
    }
}
