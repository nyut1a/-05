using AptekaIS.Models;
using AptekaIS.Services;
using Microsoft.Data.Sqlite;

namespace AptekaIS.Data;

public static class Database
{
    private static string DbPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");

    private static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        using var conn = OpenConnection();
        conn.Open();
        RunScript(conn, """
            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Login TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                PasswordSalt TEXT NOT NULL,
                RoleId INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id)
            );
            CREATE TABLE IF NOT EXISTS LoginAttempts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserLogin TEXT NOT NULL,
                IsSuccess INTEGER NOT NULL,
                Message TEXT,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Medicines (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Manufacturer TEXT,
                DosageForm TEXT,
                RequiresPrescription INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS Suppliers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT,
                Address TEXT
            );
            CREATE TABLE IF NOT EXISTS Batches (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MedicineId INTEGER NOT NULL,
                SupplierId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                ExpiryDate TEXT NOT NULL,
                PurchasePrice REAL NOT NULL,
                ReceivedDate TEXT NOT NULL,
                FOREIGN KEY (MedicineId) REFERENCES Medicines(Id),
                FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
            );
            CREATE TABLE IF NOT EXISTS Sales (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BatchId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                SalePrice REAL NOT NULL,
                SaleDate TEXT NOT NULL,
                FOREIGN KEY (BatchId) REFERENCES Batches(Id),
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            CREATE TABLE IF NOT EXISTS Prescriptions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MedicineId INTEGER NOT NULL,
                PatientName TEXT NOT NULL,
                DoctorName TEXT NOT NULL,
                IssueDate TEXT NOT NULL,
                Status TEXT NOT NULL,
                SaleId INTEGER,
                FOREIGN KEY (MedicineId) REFERENCES Medicines(Id),
                FOREIGN KEY (SaleId) REFERENCES Sales(Id)
            );
            """);

        SeedIfEmpty(conn);
    }

    private static void SeedIfEmpty(SqliteConnection conn)
    {
        if (Scalar<int>(conn, "SELECT COUNT(*) FROM Roles") > 0) return;

        RunScript(conn, """
            INSERT INTO Roles (Name) VALUES ('admin'), ('operator'), ('user');
            """);

        var (h1, s1) = PasswordHasher.HashPassword("Admin123!");
        var (h2, s2) = PasswordHasher.HashPassword("Oper123!");
        var (h3, s3) = PasswordHasher.HashPassword("User123!");

        RunScript(conn, """
            INSERT INTO Users (Login, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES
            ('admin', @h1, @s1, 1, 1),
            ('operator', @h2, @s2, 2, 1),
            ('user', @h3, @s3, 3, 1);
            """,
            new SqliteParameter("@h1", h1), new SqliteParameter("@s1", s1),
            new SqliteParameter("@h2", h2), new SqliteParameter("@s2", s2),
            new SqliteParameter("@h3", h3), new SqliteParameter("@s3", s3));

        RunScript(conn, """
            INSERT INTO Medicines (Name, Manufacturer, DosageForm, RequiresPrescription) VALUES
            ('Парацетамол 500 мг', 'Фармстандарт', 'таблетки', 0),
            ('Нурофен', 'Reckitt', 'таблетки', 0),
            ('Амоксициллин', 'Сандоз', 'капсулы', 1),
            ('Инсулин', 'Ново Нордиск', 'раствор', 1);
            INSERT INTO Suppliers (Name, Phone, Address) VALUES
            ('ФармОпт', '+7-495-111-22-33', 'Москва, ул. Складская, 1'),
            ('МедСнаб', '+7-812-444-55-66', 'СПб, пр. Поставок, 10');
            INSERT INTO Batches (MedicineId, SupplierId, Quantity, ExpiryDate, PurchasePrice, ReceivedDate) VALUES
            (1, 1, 200, '2027-06-01', 45.50, '2026-01-10'),
            (2, 1, 150, '2027-03-15', 120.00, '2026-02-01'),
            (3, 2, 80, '2026-12-01', 95.00, '2026-01-20'),
            (4, 2, 30, '2026-04-01', 890.00, '2025-11-01');
            INSERT INTO Prescriptions (MedicineId, PatientName, DoctorName, IssueDate, Status, SaleId) VALUES
            (3, 'Иванов П.С.', 'Петрова А.В.', '2026-05-10', 'Новый', NULL),
            (4, 'Сидорова М.К.', 'Козлов И.И.', '2026-05-12', 'Новый', NULL);
            """);
    }

    public static SqliteConnection OpenConnection() => new(ConnectionString);

    public static bool LoginExists(string login)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = @l";
        cmd.Parameters.AddWithValue("@l", login);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static void CreateUser(string login, string hash, string salt, int roleId)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Users (Login, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES (@l,@h,@s,@r,1)";
        cmd.Parameters.AddWithValue("@l", login);
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.Parameters.AddWithValue("@s", salt);
        cmd.Parameters.AddWithValue("@r", roleId);
        cmd.ExecuteNonQuery();
    }

    public static User? GetUserByLogin(string login)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT u.Id, u.Login, u.PasswordHash, u.PasswordSalt, u.RoleId, r.Name, u.IsActive
            FROM Users u JOIN Roles r ON u.RoleId = r.Id
            WHERE u.Login = @l
            """;
        cmd.Parameters.AddWithValue("@l", login);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new User
        {
            Id = r.GetInt32(0),
            Login = r.GetString(1),
            PasswordHash = r.GetString(2),
            PasswordSalt = r.GetString(3),
            RoleId = r.GetInt32(4),
            RoleName = r.GetString(5),
            IsActive = r.GetInt32(6) == 1
        };
    }

    public static void AddLoginAttempt(string login, bool success, string message)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO LoginAttempts (UserLogin, IsSuccess, Message, CreatedAt) VALUES (@l,@s,@m,@t)";
        cmd.Parameters.AddWithValue("@l", login);
        cmd.Parameters.AddWithValue("@s", success ? 1 : 0);
        cmd.Parameters.AddWithValue("@m", message);
        cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public static int CountFailedAttempts(string login, TimeSpan window)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        var since = DateTime.Now.Subtract(window).ToString("o");
        cmd.CommandText = """
            SELECT COUNT(*) FROM LoginAttempts
            WHERE UserLogin = @l AND IsSuccess = 0 AND CreatedAt >= @since
            """;
        cmd.Parameters.AddWithValue("@l", login);
        cmd.Parameters.AddWithValue("@since", since);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static DateTime? GetLastFailedAttemptTime(string login)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CreatedAt FROM LoginAttempts
            WHERE UserLogin = @l AND IsSuccess = 0
            ORDER BY Id DESC LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@l", login);
        var val = cmd.ExecuteScalar()?.ToString();
        return val != null && DateTime.TryParse(val, out var dt) ? dt : null;
    }

    // --- Medicines ---
    public static List<Medicine> GetMedicines(string? search = null)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Manufacturer, DosageForm, RequiresPrescription FROM Medicines";
        if (!string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText += " WHERE Name LIKE @s";
            cmd.Parameters.AddWithValue("@s", $"%{search.Trim()}%");
        }
        cmd.CommandText += " ORDER BY Name";
        return ReadMedicines(cmd);
    }

    public static void InsertMedicine(Medicine m)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Medicines (Name, Manufacturer, DosageForm, RequiresPrescription) VALUES (@n,@mf,@d,@r)";
        AddMedicineParams(cmd, m);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateMedicine(Medicine m)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Medicines SET Name=@n, Manufacturer=@mf, DosageForm=@d, RequiresPrescription=@r WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", m.Id);
        AddMedicineParams(cmd, m);
        cmd.ExecuteNonQuery();
    }

    public static string? DeleteMedicine(int id)
    {
        if (HasReference("SELECT COUNT(*) FROM Batches WHERE MedicineId=@id", id))
            return "Нельзя удалить: есть связанные партии.";
        if (HasReference("SELECT COUNT(*) FROM Prescriptions WHERE MedicineId=@id", id))
            return "Нельзя удалить: есть связанные рецепты.";
        ExecuteDelete("Medicines", id);
        return null;
    }

    // --- Suppliers ---
    public static List<Supplier> GetSuppliers(string? search = null)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Phone, Address FROM Suppliers";
        if (!string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText += " WHERE Name LIKE @s";
            cmd.Parameters.AddWithValue("@s", $"%{search.Trim()}%");
        }
        cmd.CommandText += " ORDER BY Name";
        var list = new List<Supplier>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Supplier { Id = r.GetInt32(0), Name = r.GetString(1), Phone = r.GetString(2), Address = r.GetString(3) });
        return list;
    }

    public static void InsertSupplier(Supplier s)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Suppliers (Name, Phone, Address) VALUES (@n,@p,@a)";
        cmd.Parameters.AddWithValue("@n", s.Name);
        cmd.Parameters.AddWithValue("@p", s.Phone);
        cmd.Parameters.AddWithValue("@a", s.Address);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateSupplier(Supplier s)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Suppliers SET Name=@n, Phone=@p, Address=@a WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", s.Id);
        cmd.Parameters.AddWithValue("@n", s.Name);
        cmd.Parameters.AddWithValue("@p", s.Phone);
        cmd.Parameters.AddWithValue("@a", s.Address);
        cmd.ExecuteNonQuery();
    }

    public static string? DeleteSupplier(int id)
    {
        if (HasReference("SELECT COUNT(*) FROM Batches WHERE SupplierId=@id", id))
            return "Нельзя удалить: есть связанные партии.";
        ExecuteDelete("Suppliers", id);
        return null;
    }

    // --- Batches ---
    public static List<Batch> GetBatches()
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT b.Id, b.MedicineId, m.Name, b.SupplierId, s.Name, b.Quantity,
                   b.ExpiryDate, b.PurchasePrice, b.ReceivedDate
            FROM Batches b
            JOIN Medicines m ON b.MedicineId = m.Id
            JOIN Suppliers s ON b.SupplierId = s.Id
            ORDER BY b.ReceivedDate DESC
            """;
        var list = new List<Batch>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(ReadBatch(r));
        return list;
    }

    public static Batch? GetBatchById(int id)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT b.Id, b.MedicineId, m.Name, b.SupplierId, s.Name, b.Quantity,
                   b.ExpiryDate, b.PurchasePrice, b.ReceivedDate
            FROM Batches b
            JOIN Medicines m ON b.MedicineId = m.Id
            JOIN Suppliers s ON b.SupplierId = s.Id
            WHERE b.Id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadBatch(r) : null;
    }

    public static void InsertBatch(Batch b)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Batches (MedicineId, SupplierId, Quantity, ExpiryDate, PurchasePrice, ReceivedDate)
            VALUES (@mid,@sid,@q,@exp,@pr,@rec)
            """;
        AddBatchParams(cmd, b);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateBatch(Batch b)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Batches SET MedicineId=@mid, SupplierId=@sid, Quantity=@q,
            ExpiryDate=@exp, PurchasePrice=@pr, ReceivedDate=@rec WHERE Id=@id
            """;
        cmd.Parameters.AddWithValue("@id", b.Id);
        AddBatchParams(cmd, b);
        cmd.ExecuteNonQuery();
    }

    public static string? DeleteBatch(int id)
    {
        if (HasReference("SELECT COUNT(*) FROM Sales WHERE BatchId=@id", id))
            return "Нельзя удалить: по партии есть продажи.";
        ExecuteDelete("Batches", id);
        return null;
    }

    public static void DecreaseBatchQuantity(int batchId, int qty)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Batches SET Quantity = Quantity - @q WHERE Id = @id";
        cmd.Parameters.AddWithValue("@q", qty);
        cmd.Parameters.AddWithValue("@id", batchId);
        cmd.ExecuteNonQuery();
    }

    // --- Sales ---
    public static List<Sale> GetSales()
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT s.Id, s.BatchId, m.Name, s.UserId, u.Login, s.Quantity, s.SalePrice, s.SaleDate
            FROM Sales s
            JOIN Batches b ON s.BatchId = b.Id
            JOIN Medicines m ON b.MedicineId = m.Id
            JOIN Users u ON s.UserId = u.Id
            ORDER BY s.SaleDate DESC
            """;
        var list = new List<Sale>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Sale
            {
                Id = r.GetInt32(0),
                BatchId = r.GetInt32(1),
                MedicineName = r.GetString(2),
                UserId = r.GetInt32(3),
                UserLogin = r.GetString(4),
                Quantity = r.GetInt32(5),
                SalePrice = (decimal)r.GetDouble(6),
                SaleDate = DateTime.Parse(r.GetString(7))
            });
        return list;
    }

    public static int InsertSale(Sale sale)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Sales (BatchId, UserId, Quantity, SalePrice, SaleDate)
            VALUES (@b,@u,@q,@p,@d);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@b", sale.BatchId);
        cmd.Parameters.AddWithValue("@u", sale.UserId);
        cmd.Parameters.AddWithValue("@q", sale.Quantity);
        cmd.Parameters.AddWithValue("@p", sale.SalePrice);
        cmd.Parameters.AddWithValue("@d", sale.SaleDate.ToString("o"));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static string? DeleteSale(int id)
    {
        var sale = GetSaleById(id);
        if (sale == null) return "Продажа не найдена.";
        using var conn = OpenConnection();
        conn.Open();
        using var tr = conn.BeginTransaction();
        try
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;
                cmd.CommandText = "UPDATE Batches SET Quantity = Quantity + @q WHERE Id = @b";
                cmd.Parameters.AddWithValue("@q", sale.Quantity);
                cmd.Parameters.AddWithValue("@b", sale.BatchId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;
                cmd.CommandText = "DELETE FROM Sales WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            tr.Commit();
            return null;
        }
        catch (Exception ex)
        {
            tr.Rollback();
            return ex.Message;
        }
    }

    private static Sale? GetSaleById(int id)
    {
        foreach (var s in GetSales())
            if (s.Id == id) return s;
        return null;
    }

    // --- Prescriptions ---
    public static List<Prescription> GetPrescriptions()
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT p.Id, p.MedicineId, m.Name, p.PatientName, p.DoctorName, p.IssueDate, p.Status, p.SaleId
            FROM Prescriptions p JOIN Medicines m ON p.MedicineId = m.Id
            ORDER BY p.IssueDate DESC
            """;
        var list = new List<Prescription>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(ReadPrescription(r));
        return list;
    }

    public static void InsertPrescription(Prescription p)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Prescriptions (MedicineId, PatientName, DoctorName, IssueDate, Status, SaleId)
            VALUES (@mid,@pn,@dn,@dt,@st,NULL)
            """;
        AddPrescriptionParams(cmd, p);
        cmd.ExecuteNonQuery();
    }

    public static void UpdatePrescription(Prescription p)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Prescriptions SET MedicineId=@mid, PatientName=@pn, DoctorName=@dn,
            IssueDate=@dt, Status=@st WHERE Id=@id
            """;
        cmd.Parameters.AddWithValue("@id", p.Id);
        AddPrescriptionParams(cmd, p);
        cmd.ExecuteNonQuery();
    }

    public static void SetPrescriptionSale(int prescriptionId, int saleId, string status)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Prescriptions SET SaleId=@s, Status=@st WHERE Id=@id";
        cmd.Parameters.AddWithValue("@s", saleId);
        cmd.Parameters.AddWithValue("@st", status);
        cmd.Parameters.AddWithValue("@id", prescriptionId);
        cmd.ExecuteNonQuery();
    }

    public static string? DeletePrescription(int id)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Status FROM Prescriptions WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        var st = cmd.ExecuteScalar()?.ToString();
        if (st == "Отпущен")
            return "Нельзя удалить отпущенный рецепт.";
        ExecuteDelete("Prescriptions", id);
        return null;
    }

    public static Medicine? GetMedicineById(int id)
    {
        foreach (var m in GetMedicines())
            if (m.Id == id) return m;
        return null;
    }

    // --- helpers ---
    private static void AddMedicineParams(SqliteCommand cmd, Medicine m)
    {
        cmd.Parameters.AddWithValue("@n", m.Name);
        cmd.Parameters.AddWithValue("@mf", m.Manufacturer);
        cmd.Parameters.AddWithValue("@d", m.DosageForm);
        cmd.Parameters.AddWithValue("@r", m.RequiresPrescription ? 1 : 0);
    }

    private static void AddBatchParams(SqliteCommand cmd, Batch b)
    {
        cmd.Parameters.AddWithValue("@mid", b.MedicineId);
        cmd.Parameters.AddWithValue("@sid", b.SupplierId);
        cmd.Parameters.AddWithValue("@q", b.Quantity);
        cmd.Parameters.AddWithValue("@exp", b.ExpiryDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@pr", b.PurchasePrice);
        cmd.Parameters.AddWithValue("@rec", b.ReceivedDate.ToString("yyyy-MM-dd"));
    }

    private static void AddPrescriptionParams(SqliteCommand cmd, Prescription p)
    {
        cmd.Parameters.AddWithValue("@mid", p.MedicineId);
        cmd.Parameters.AddWithValue("@pn", p.PatientName);
        cmd.Parameters.AddWithValue("@dn", p.DoctorName);
        cmd.Parameters.AddWithValue("@dt", p.IssueDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@st", p.Status);
    }

    private static List<Medicine> ReadMedicines(SqliteCommand cmd)
    {
        var list = new List<Medicine>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Medicine
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Manufacturer = r.GetString(2),
                DosageForm = r.GetString(3),
                RequiresPrescription = r.GetInt32(4) == 1
            });
        return list;
    }

    private static Batch ReadBatch(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        MedicineId = r.GetInt32(1),
        MedicineName = r.GetString(2),
        SupplierId = r.GetInt32(3),
        SupplierName = r.GetString(4),
        Quantity = r.GetInt32(5),
        ExpiryDate = DateTime.Parse(r.GetString(6)),
        PurchasePrice = (decimal)r.GetDouble(7),
        ReceivedDate = DateTime.Parse(r.GetString(8))
    };

    private static Prescription ReadPrescription(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        MedicineId = r.GetInt32(1),
        MedicineName = r.GetString(2),
        PatientName = r.GetString(3),
        DoctorName = r.GetString(4),
        IssueDate = DateTime.Parse(r.GetString(5)),
        Status = r.GetString(6),
        SaleId = r.IsDBNull(7) ? null : r.GetInt32(7)
    };

    private static bool HasReference(string sql, int id)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static void ExecuteDelete(string table, int id)
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {table} WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static void RunScript(SqliteConnection conn, string sql, params SqliteParameter[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        cmd.ExecuteNonQuery();
    }

    private static T Scalar<T>(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var val = cmd.ExecuteScalar();
        return (T)Convert.ChangeType(val!, typeof(T));
    }
}
