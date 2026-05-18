using AptekaIS.Data;
using AptekaIS.Models;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class PrescriptionsForm : Form
{
    private readonly DataGridView _grid = new() { ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public PrescriptionsForm()
    {
        Text = "Рецепты";
        ClientSize = new Size(850, 400);
        _grid.Location = new Point(10, 55);
        _grid.Size = new Size(820, 320);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Controls.Add(_grid);

        if (Session.CanEdit)
        {
            AddBtn("Добавить", 10, 15, BtnAdd);
            AddBtn("Изменить", 110, 15, BtnEdit);
            AddBtn("Удалить", 210, 15, BtnDelete);
            AddBtn("Отпустить", 310, 15, BtnDispense);
        }

        LoadData();
    }

    private void AddBtn(string t, int x, int y, EventHandler h)
    {
        var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(90, 28) };
        b.Click += h;
        Controls.Add(b);
    }

    private Prescription? Selected() => _grid.CurrentRow?.DataBoundItem as Prescription;

    private void LoadData()
    {
        _grid.DataSource = null;
        _grid.DataSource = Database.GetPrescriptions();
        _grid.Columns["Id"]!.Visible = false;
        _grid.Columns["MedicineId"]!.Visible = false;
        _grid.Columns["SaleId"]!.Visible = false;
        if (_grid.Columns["MedicineName"] != null) _grid.Columns["MedicineName"]!.HeaderText = "Лекарство";
    }

    private void BtnAdd(object? s, EventArgs e)
    {
        using var dlg = new PrescriptionEditForm(null);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.InsertPrescription(dlg.Item);
            LoadData();
        }
    }

    private void BtnEdit(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) return;
        if (item.Status == "Отпущен")
        {
            MessageBox.Show("Отпущенный рецепт нельзя редактировать.");
            return;
        }
        using var dlg = new PrescriptionEditForm(item);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.UpdatePrescription(dlg.Item);
            LoadData();
        }
    }

    private void BtnDelete(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) return;
        if (MessageBox.Show("Удалить рецепт?", "?", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var err = Database.DeletePrescription(item.Id);
        if (err != null) MessageBox.Show(err);
        else LoadData();
    }

    private void BtnDispense(object? s, EventArgs e)
    {
        var rx = Selected();
        if (rx == null) { MessageBox.Show("Выберите рецепт."); return; }
        if (rx.Status != "Новый")
        {
            MessageBox.Show("Рецепт уже обработан.");
            return;
        }

        var batch = Database.GetBatches()
            .FirstOrDefault(b => b.MedicineId == rx.MedicineId && b.Quantity > 0 && b.ExpiryDate.Date >= DateTime.Today);
        if (batch == null)
        {
            MessageBox.Show("Нет подходящей партии на складе.");
            return;
        }

        var price = batch.PurchasePrice * 1.2m;
        var err = SalesForm.TryCreateSale(batch.Id, 1, price);
        if (err != null) MessageBox.Show(err);
        else
        {
            MessageBox.Show("Рецепт отпущен, продажа оформлена.");
            LoadData();
        }
    }
}

public class PrescriptionEditForm : Form
{
    private readonly ComboBox _medicine = new() { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _patient = new() { Width = 250 };
    private readonly TextBox _doctor = new() { Width = 250 };
    private readonly DateTimePicker _date = new() { Width = 150, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _status = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

    public Prescription Item { get; }

    public PrescriptionEditForm(Prescription? existing)
    {
        Item = existing != null
            ? new Prescription
            {
                Id = existing.Id,
                MedicineId = existing.MedicineId,
                PatientName = existing.PatientName,
                DoctorName = existing.DoctorName,
                IssueDate = existing.IssueDate,
                Status = existing.Status
            }
            : new Prescription { IssueDate = DateTime.Today, Status = "Новый" };

        Text = existing == null ? "Новый рецепт" : "Редактирование рецепта";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(420, 260);
        StartPosition = FormStartPosition.CenterParent;

        _medicine.DataSource = Database.GetMedicines().Where(m => m.RequiresPrescription).ToList();
        _medicine.DisplayMember = "Name";
        _medicine.ValueMember = "Id";
        _status.Items.AddRange(new object[] { "Новый", "Отменён" });
        if (existing == null) _status.SelectedIndex = 0;
        else _status.SelectedItem = Item.Status;

        _patient.Text = Item.PatientName;
        _doctor.Text = Item.DoctorName;
        _date.Value = Item.IssueDate;
        if (existing != null) _medicine.SelectedValue = Item.MedicineId;

        int y = 15;
        Add("Лекарство:", _medicine, ref y);
        Add("Пациент:", _patient, ref y);
        Add("Врач:", _doctor, ref y);
        Add("Дата:", _date, ref y);
        Add("Статус:", _status, ref y);

        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Location = new Point(130, y) };
        ok.Click += (_, _) =>
        {
            if (_medicine.SelectedValue is not int mid) return;
            if (string.IsNullOrWhiteSpace(_patient.Text) || string.IsNullOrWhiteSpace(_doctor.Text))
            {
                MessageBox.Show("Заполните пациента и врача.");
                DialogResult = DialogResult.None;
                return;
            }
            Item.MedicineId = mid;
            Item.PatientName = _patient.Text.Trim();
            Item.DoctorName = _doctor.Text.Trim();
            Item.IssueDate = _date.Value.Date;
            Item.Status = _status.SelectedItem?.ToString() ?? "Новый";
        };
        Controls.Add(ok);
        Controls.Add(new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(230, y) });
    }

    private void Add(string l, Control c, ref int y)
    {
        Controls.Add(new Label { Text = l, Location = new Point(15, y + 3), AutoSize = true });
        c.Location = new Point(120, y);
        Controls.Add(c);
        y += 35;
    }
}
