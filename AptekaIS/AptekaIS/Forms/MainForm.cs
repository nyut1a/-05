using AptekaIS.Services;

namespace AptekaIS.Forms;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "Аптека — учёт лекарств";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 380);

        var user = Session.CurrentUser!;
        var lbl = new Label
        {
            Text = $"Пользователь: {user.Login}  |  Роль: {user.RoleName}",
            Location = new Point(20, 15),
            AutoSize = true
        };
        Controls.Add(lbl);

        var y = 50;
        AddMenuButton("Лекарства", y, () => OpenForm(new MedicinesForm()));
        y += 45;
        AddMenuButton("Поставщики", y, () => OpenForm(new SuppliersForm()));
        y += 45;
        AddMenuButton("Партии", y, () => OpenForm(new BatchesForm()));
        y += 45;
        AddMenuButton("Продажи", y, () => OpenForm(new SalesForm()));
        y += 45;
        AddMenuButton("Рецепты", y, () => OpenForm(new PrescriptionsForm()));
        y += 55;

        var btnExit = new Button { Text = "Выход", Location = new Point(20, y), Size = new Size(100, 32) };
        btnExit.Click += (_, _) => Close();
        Controls.Add(btnExit);

        if (!Session.CanEdit)
        {
            Text += " (только просмотр)";
        }
    }

    private void AddMenuButton(string text, int y, Action action)
    {
        var btn = new Button { Text = text, Location = new Point(20, y), Size = new Size(360, 36) };
        btn.Click += (_, _) => action();
        Controls.Add(btn);
    }

    private static void OpenForm(Form form)
    {
        form.StartPosition = FormStartPosition.CenterParent;
        form.ShowDialog();
    }
}
