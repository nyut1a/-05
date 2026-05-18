using AptekaIS.Data;
using AptekaIS.Models;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class BatchesForm : Form
{
    private readonly DataGridView _grid = new() { ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public BatchesForm()
    {
        Text = "Партии";
        ClientSize = new Size(900, 420);
        _grid.Location = new Point(10, 55);
        _grid.Size = new Size(870, 340);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Controls.Add(_grid);

        if (Session.CanEdit)
        {
            AddBtn("Добавить", 10, 15, BtnAdd);
            AddBtn("Изменить", 110, 15, BtnEdit);
            AddBtn("Удалить", 210, 15, BtnDelete);
        }

        LoadData();
    }

    private void AddBtn(string t, int x, int y, EventHandler h)
    {
        var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(90, 28) };
        b.Click += h;
        Controls.Add(b);
    }

    private Batch? Selected() => _grid.CurrentRow?.DataBoundItem as Batch;

    private void LoadData()
    {
        _grid.DataSource = null;
        _grid.DataSource = Database.GetBatches();
        _grid.Columns["Id"]!.Visible = false;
        _grid.Columns["MedicineId"]!.Visible = false;
        _grid.Columns["SupplierId"]!.Visible = false;
    }

    private void BtnAdd(object? s, EventArgs e)
    {
        using var dlg = new BatchEditForm(null);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.InsertBatch(dlg.Item);
            LoadData();
        }
    }

    private void BtnEdit(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) { MessageBox.Show("Выберите строку."); return; }
        using var dlg = new BatchEditForm(item);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.UpdateBatch(dlg.Item);
            LoadData();
        }
    }

    private void BtnDelete(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) return;
        if (MessageBox.Show("Удалить партию?", "?", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var err = Database.DeleteBatch(item.Id);
        if (err != null) MessageBox.Show(err);
        else LoadData();
    }
}

public class BatchEditForm : Form
{
    private readonly ComboBox _medicine = new() { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _supplier = new() { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _qty = new() { Minimum = 1, Maximum = 100000, Width = 100 };
    private readonly DateTimePicker _expiry = new() { Width = 150, Format = DateTimePickerFormat.Short };
    private readonly NumericUpDown _price = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Width = 120 };
    private readonly DateTimePicker _received = new() { Width = 150, Format = DateTimePickerFormat.Short };

    public Batch Item { get; }

    public BatchEditForm(Batch? existing)
    {
        Item = existing != null
            ? new Batch
            {
                Id = existing.Id,
                MedicineId = existing.MedicineId,
                SupplierId = existing.SupplierId,
                Quantity = existing.Quantity,
                ExpiryDate = existing.ExpiryDate,
                PurchasePrice = existing.PurchasePrice,
                ReceivedDate = existing.ReceivedDate
            }
            : new Batch { ReceivedDate = DateTime.Today, ExpiryDate = DateTime.Today.AddYears(1) };

        Text = existing == null ? "Новая партия" : "Редактирование партии";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(420, 280);
        StartPosition = FormStartPosition.CenterParent;

        var meds = Database.GetMedicines();
        var sups = Database.GetSuppliers();
        _medicine.DataSource = meds;
        _medicine.DisplayMember = "Name";
        _medicine.ValueMember = "Id";
        _supplier.DataSource = sups;
        _supplier.DisplayMember = "Name";
        _supplier.ValueMember = "Id";

        if (meds.Count == 0 || sups.Count == 0)
        {
            MessageBox.Show("Сначала добавьте лекарства и поставщиков.");
            Load += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        _qty.Value = Item.Quantity > 0 ? Item.Quantity : 1;
        _price.Value = Item.PurchasePrice > 0 ? Item.PurchasePrice : 1;
        _expiry.Value = Item.ExpiryDate;
        _received.Value = Item.ReceivedDate;
        if (existing != null)
        {
            _medicine.SelectedValue = Item.MedicineId;
            _supplier.SelectedValue = Item.SupplierId;
        }

        int y = 15;
        Add("Лекарство:", _medicine, ref y);
        Add("Поставщик:", _supplier, ref y);
        Add("Кол-во:", _qty, ref y);
        Add("Срок годн.:", _expiry, ref y);
        Add("Цена закупки:", _price, ref y);
        Add("Дата поступл.:", _received, ref y);

        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Location = new Point(130, y) };
        ok.Click += (_, _) =>
        {
            if (_medicine.SelectedValue is not int mid) return;
            if (_supplier.SelectedValue is not int sid) return;
            Item.MedicineId = mid;
            Item.SupplierId = sid;
            Item.Quantity = (int)_qty.Value;
            Item.ExpiryDate = _expiry.Value.Date;
            Item.PurchasePrice = _price.Value;
            Item.ReceivedDate = _received.Value.Date;
        };
        Controls.Add(ok);
        Controls.Add(new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(230, y) });
    }

    private void Add(string l, Control c, ref int y)
    {
        Controls.Add(new Label { Text = l, Location = new Point(15, y + 3), AutoSize = true });
        c.Location = new Point(140, y);
        Controls.Add(c);
        y += 35;
    }
}
