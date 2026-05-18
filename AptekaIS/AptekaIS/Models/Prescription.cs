namespace AptekaIS.Models;

public class Prescription
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string DoctorName { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public string Status { get; set; } = "Новый";
    public int? SaleId { get; set; }
}
