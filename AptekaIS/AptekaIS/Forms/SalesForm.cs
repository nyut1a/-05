using AptekaIS.Data;
using AptekaIS.Models;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class SalesForm : Form
{
    private readonly DataGridView _grid = new() { ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public SalesForm()
    {
        Text = "Продажи";
        ClientSize = new Size(850, 400);
        _grid.Location = new Point(10, 55);
        _grid.Size = new Size(820, 320);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Controls.Add(_grid);

        if (Session.CanEdit)
        {
            AddBtn("Новая продажа", 10, 15, BtnAdd);
            AddBtn("Удалить (отмена)", 140, 15, BtnDelete);
        }

        LoadData();
    }

    private void AddBtn(string t, int x, int y, EventHandler h)
    {
        var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(120, 28) };
        b.Click += h;
        Controls.Add(b);
    }

    private Sale? Selected() => _grid.CurrentRow?.DataBoundItem as Sale;

    private void LoadData()
    {
        _grid.DataSource = null;
        _grid.DataSource = Database.GetSales();
        _grid.Columns["Id"]!.Visible = false;
        _grid.Columns["BatchId"]!.Visible = false;
        _grid.Columns["UserId"]!.Visible = false;
        if (_grid.Columns["UserLogin"] != null) _grid.Columns["UserLogin"]!.HeaderText = "Продавец";
        if (_grid.Columns["SalePrice"] != null) _grid.Columns["SalePrice"]!.HeaderText = "Сумма";
    }

    private void BtnAdd(object? s, EventArgs e)
    {
        using var dlg = new SaleAddForm();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var err = TryCreateSale(dlg.BatchId, dlg.Quantity, dlg.SalePrice);
        if (err != null) MessageBox.Show(err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else LoadData();
    }

    public static string? TryCreateSale(int batchId, int quantity, decimal salePrice)
    {
        var batch = Database.GetBatchById(batchId);
        if (batch == null) return "Партия не найдена.";
        if (batch.ExpiryDate.Date < DateTime.Today)
            return "Нельзя продать: срок годности партии истёк.";
        if (quantity <= 0) return "Укажите количество больше 0.";
        if (quantity > batch.Quantity)
            return $"Недостаточно на складе. Остаток: {batch.Quantity}.";

        var medicine = Database.GetMedicineById(batch.MedicineId);
        if (medicine?.RequiresPrescription == true)
        {
            var rx = Database.GetPrescriptions()
                .FirstOrDefault(p => p.MedicineId == batch.MedicineId && p.Status == "Новый");
            if (rx == null)
                return "Для рецептурного препарата нужен рецепт со статусом «Новый».";

            var sale = new Sale
            {
                BatchId = batchId,
                UserId = Session.CurrentUser!.Id,
                Quantity = quantity,
                SalePrice = salePrice,
                SaleDate = DateTime.Now
            };
            var saleId = Database.InsertSale(sale);
            Database.DecreaseBatchQuantity(batchId, quantity);
            Database.SetPrescriptionSale(rx.Id, saleId, "Отпущен");
            return null;
        }

        var s = new Sale
        {
            BatchId = batchId,
            UserId = Session.CurrentUser!.Id,
            Quantity = quantity,
            SalePrice = salePrice,
            SaleDate = DateTime.Now
        };
        Database.InsertSale(s);
        Database.DecreaseBatchQuantity(batchId, quantity);
        return null;
    }

    private void BtnDelete(object? s, EventArgs e)
    {
        if (!Session.IsAdmin)
        {
            MessageBox.Show("Отмена продажи доступна только администратору.");
            return;
        }
        var item = Selected();
        if (item == null) return;
        if (MessageBox.Show("Отменить продажу и вернуть товар на склад?", "?", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        var err = Database.DeleteSale(item.Id);
        if (err != null) MessageBox.Show(err);
        else LoadData();
    }
}

public class SaleAddForm : Form
{
    private readonly ComboBox _batch = new() { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _qty = new() { Minimum = 1, Maximum = 10000, Width = 80 };
    private readonly NumericUpDown _price = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, Width = 120 };

    public int BatchId { get; private set; }
    public int Quantity { get; private set; }
    public decimal SalePrice { get; private set; }

    public SaleAddForm()
    {
        Text = "Новая продажа";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(480, 180);
        StartPosition = FormStartPosition.CenterParent;

        var batches = Database.GetBatches().Where(b => b.Quantity > 0).ToList();
        _batch.DataSource = batches;
        _batch.DisplayMember = "MedicineName";
        _batch.Format += (_, e) =>
        {
            if (e.ListItem is Batch b)
                e.Value = $"{b.MedicineName} | остаток {b.Quantity} | до {b.ExpiryDate:dd.MM.yyyy}";
        };

        if (batches.Count == 0)
        {
            MessageBox.Show("Нет партий с остатком.");
            Load += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        int y = 15;
        Add("Партия:", _batch, ref y);
        Add("Кол-во:", _qty, ref y);
        Add("Сумма продажи:", _price, ref y);

        var ok = new Button { Text = "Оформить", DialogResult = DialogResult.OK, Location = new Point(150, y) };
        ok.Click += (_, _) =>
        {
            if (_batch.SelectedItem is not Batch b) { DialogResult = DialogResult.None; return; }
            BatchId = b.Id;
            Quantity = (int)_qty.Value;
            SalePrice = _price.Value;
        };
        Controls.Add(ok);
        Controls.Add(new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(260, y) });
    }

    private void Add(string l, Control c, ref int y)
    {
        Controls.Add(new Label { Text = l, Location = new Point(15, y + 3), AutoSize = true });
        c.Location = new Point(150, y);
        Controls.Add(c);
        y += 35;
    }
}
