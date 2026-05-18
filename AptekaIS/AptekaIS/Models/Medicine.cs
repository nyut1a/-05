namespace AptekaIS.Models;

public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string DosageForm { get; set; } = "";
    public bool RequiresPrescription { get; set; }
}
