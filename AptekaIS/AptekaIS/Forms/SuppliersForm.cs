using AptekaIS.Data;
using AptekaIS.Models;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class SuppliersForm : Form
{
    private readonly DataGridView _grid = new() { ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly TextBox _txtSearch = new() { Width = 180 };

    public SuppliersForm()
    {
        Text = "Поставщики";
        ClientSize = new Size(700, 400);
        _grid.Location = new Point(10, 90);
        _grid.Size = new Size(670, 290);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        Controls.Add(new Label { Text = "Поиск:", Location = new Point(10, 18), AutoSize = true });
        _txtSearch.Location = new Point(70, 15);
        Controls.Add(_txtSearch);
        var btnSearch = new Button { Text = "Найти", Location = new Point(260, 14), Size = new Size(70, 28) };
        btnSearch.Click += (_, _) => LoadData();
        Controls.Add(btnSearch);
        Controls.Add(_grid);

        if (Session.CanEdit)
        {
            AddBtn("Добавить", 10, 55, BtnAdd);
            AddBtn("Изменить", 110, 55, BtnEdit);
            AddBtn("Удалить", 210, 55, BtnDelete);
        }

        LoadData();
    }

    private void AddBtn(string t, int x, int y, EventHandler h)
    {
        var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(90, 28) };
        b.Click += h;
        Controls.Add(b);
    }

    private Supplier? Selected() => _grid.CurrentRow?.DataBoundItem as Supplier;

    private void LoadData()
    {
        _grid.DataSource = null;
        _grid.DataSource = Database.GetSuppliers(_txtSearch.Text);
        _grid.Columns["Id"]!.Visible = false;
    }

    private void BtnAdd(object? s, EventArgs e)
    {
        using var dlg = new SupplierEditForm(null);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.InsertSupplier(dlg.Item);
            LoadData();
        }
    }

    private void BtnEdit(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) { MessageBox.Show("Выберите строку."); return; }
        using var dlg = new SupplierEditForm(item);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Database.UpdateSupplier(dlg.Item);
            LoadData();
        }
    }

    private void BtnDelete(object? s, EventArgs e)
    {
        var item = Selected();
        if (item == null) return;
        if (MessageBox.Show($"Удалить «{item.Name}»?", "?", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var err = Database.DeleteSupplier(item.Id);
        if (err != null) MessageBox.Show(err);
        else LoadData();
    }
}

public class SupplierEditForm : Form
{
    private readonly TextBox _name = new() { Width = 250 };
    private readonly TextBox _phone = new() { Width = 250 };
    private readonly TextBox _address = new() { Width = 250 };
    public Supplier Item { get; }

    public SupplierEditForm(Supplier? existing)
    {
        Item = existing != null
            ? new Supplier { Id = existing.Id, Name = existing.Name, Phone = existing.Phone, Address = existing.Address }
            : new Supplier();
        Text = existing == null ? "Новый поставщик" : "Редактирование";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(400, 210);
        StartPosition = FormStartPosition.CenterParent;

        _name.Text = Item.Name;
        _phone.Text = Item.Phone;
        _address.Text = Item.Address;

        int y = 15;
        Add("Название:", _name, ref y);
        Add("Телефон:", _phone, ref y);
        Add("Адрес:", _address, ref y);
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(130, y) };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Укажите название."); DialogResult = DialogResult.None; return; }
            Item.Name = _name.Text.Trim();
            Item.Phone = _phone.Text.Trim();
            Item.Address = _address.Text.Trim();
        };
        Controls.Add(ok);
        Controls.Add(new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(220, y) });
    }

    private void Add(string l, Control c, ref int y)
    {
        Controls.Add(new Label { Text = l, Location = new Point(15, y + 3), AutoSize = true });
        c.Location = new Point(100, y);
        Controls.Add(c);
        y += 35;
    }
}
