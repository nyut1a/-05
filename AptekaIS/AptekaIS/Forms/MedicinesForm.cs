using AptekaIS.Data;
using AptekaIS.Models;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class MedicinesForm : Form
{
    private readonly DataGridView _grid = new() { ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly TextBox _txtSearch = new() { Width = 180 };

    public MedicinesForm()
    {
        Text = "Лекарства";
        ClientSize = new Size(720, 420);
        _grid.Dock = DockStyle.Bottom;
        _grid.Height = 300;
        _grid.Location = new Point(10, 90);
        _grid.Width = 690;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var btnSearch = new Button { Text = "Поиск", Location = new Point(300, 15), Size = new Size(70, 28) };
        btnSearch.Click += (_, _) => LoadData();
        Controls.Add(new Label { Text = "Поиск по названию:", Location = new Point(10, 18), AutoSize = true });
        _txtSearch.Location = new Point(150, 15);
        Controls.Add(_txtSearch);
        Controls.Add(btnSearch);
        Controls.Add(_grid);

        int x = 10, y = 55;
        if (Session.CanEdit)
        {
            AddBtn("Добавить", x, y, BtnAdd); x += 100;
            AddBtn("Изменить", x, y, BtnEdit); x += 100;
            AddBtn("Удалить", x, y, BtnDelete);
        }
        else
        {
            Controls.Add(new Label
            {
                Text = "Режим просмотра (роль user)",
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.Gray
            });
        }

        LoadData();
    }

    private void AddBtn(string text, int x, int y, EventHandler click)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(90, 28) };
        b.Click += click;
        Controls.Add(b);
    }

    private Medicine? Selected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Medicine m) return m;
        return null;
    }

    private void LoadData()
    {
        _grid.DataSource = null;
        _grid.DataSource = Database.GetMedicines(_txtSearch.Text);
        _grid.Columns["Id"]!.Visible = false;
        if (_grid.Columns["RequiresPrescription"] != null)
            _grid.Columns["RequiresPrescription"]!.HeaderText = "По рецепту";
    }

    private void BtnAdd(object? s, EventArgs e)
    {
        using var dlg = new MedicineEditForm(null);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.InsertMedicine(dlg.Medicine);
            LoadData();
        }
    }

    private void BtnEdit(object? s, EventArgs e)
    {
        var m = Selected();
        if (m == null) { MessageBox.Show("Выберите строку."); return; }
        using var dlg = new MedicineEditForm(m);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.UpdateMedicine(dlg.Medicine);
            LoadData();
        }
    }

    private void BtnDelete(object? s, EventArgs e)
    {
        var m = Selected();
        if (m == null) { MessageBox.Show("Выберите строку."); return; }
        if (MessageBox.Show($"Удалить «{m.Name}»?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        var err = Database.DeleteMedicine(m.Id);
        if (err != null) MessageBox.Show(err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else LoadData();
    }
}

public class MedicineEditForm : Form
{
    private readonly TextBox _name = new() { Width = 250 };
    private readonly TextBox _manufacturer = new() { Width = 250 };
    private readonly TextBox _dosage = new() { Width = 250 };
    private readonly CheckBox _rx = new() { Text = "Отпуск по рецепту", AutoSize = true };

    public Medicine Medicine { get; }

    public MedicineEditForm(Medicine? existing)
    {
        Medicine = existing != null
            ? new Medicine
            {
                Id = existing.Id,
                Name = existing.Name,
                Manufacturer = existing.Manufacturer,
                DosageForm = existing.DosageForm,
                RequiresPrescription = existing.RequiresPrescription
            }
            : new Medicine();

        Text = existing == null ? "Новое лекарство" : "Редактирование";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(400, 240);
        StartPosition = FormStartPosition.CenterParent;

        int y = 15;
        AddRow("Название:", _name, ref y);
        AddRow("Производитель:", _manufacturer, ref y);
        AddRow("Форма:", _dosage, ref y);
        _name.Text = Medicine.Name;
        _manufacturer.Text = Medicine.Manufacturer;
        _dosage.Text = Medicine.DosageForm;
        _rx.Checked = Medicine.RequiresPrescription;
        _rx.Location = new Point(130, y);
        Controls.Add(_rx);
        y += 40;

        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Location = new Point(130, y), Size = new Size(90, 28) };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(230, y), Size = new Size(90, 28) };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show("Укажите название.");
                DialogResult = DialogResult.None;
                return;
            }
            Medicine.Name = _name.Text.Trim();
            Medicine.Manufacturer = _manufacturer.Text.Trim();
            Medicine.DosageForm = _dosage.Text.Trim();
            Medicine.RequiresPrescription = _rx.Checked;
        };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AddRow(string label, Control ctrl, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(15, y + 3), AutoSize = true });
        ctrl.Location = new Point(130, y);
        Controls.Add(ctrl);
        y += 35;
    }
}
