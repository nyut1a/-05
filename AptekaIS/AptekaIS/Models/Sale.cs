namespace AptekaIS.Models;

public class Sale
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string MedicineName { get; set; } = "";
    public int UserId { get; set; }
    public string UserLogin { get; set; } = "";
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime SaleDate { get; set; }
}
