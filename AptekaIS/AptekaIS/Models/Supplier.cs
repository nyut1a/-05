namespace AptekaIS.Models;

public class Supplier
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string INN { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
