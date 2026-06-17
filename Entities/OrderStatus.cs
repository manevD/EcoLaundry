namespace EcoLaundry.Entities
{
    using System.ComponentModel.DataAnnotations;
    public enum OrderStatus
    {
        [Display(Name = "Примено")]
        Received = 1,


        [Display(Name = "Во обработка")]
        InProgress = 2,


        [Display(Name = "Завршено")]
        Finished = 3,


        [Display(Name = "Откажано")]
        Cancelled = 4
    }
}
