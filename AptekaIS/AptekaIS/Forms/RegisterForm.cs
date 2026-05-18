using AptekaIS.Services;

namespace AptekaIS.Forms;

public class RegisterForm : Form
{
    private readonly TextBox _txtLogin = new() { Width = 220 };
    private readonly TextBox _txtPassword = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly TextBox _txtConfirm = new() { Width = 220, UseSystemPasswordChar = true };

    public RegisterForm()
    {
        Text = "Регистрация";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 220);

        var y = 20;
        Controls.Add(new Label { Text = "Логин:", Location = new Point(30, y), AutoSize = true });
        _txtLogin.Location = new Point(130, y - 3);
        Controls.Add(_txtLogin);
        y += 40;

        Controls.Add(new Label { Text = "Пароль:", Location = new Point(30, y), AutoSize = true });
        _txtPassword.Location = new Point(130, y - 3);
        Controls.Add(_txtPassword);
        y += 40;

        Controls.Add(new Label { Text = "Повтор:", Location = new Point(30, y), AutoSize = true });
        _txtConfirm.Location = new Point(130, y - 3);
        Controls.Add(_txtConfirm);
        y += 50;

        var btnOk = new Button { Text = "Зарегистрироваться", Location = new Point(130, y), Size = new Size(140, 30) };
        btnOk.Click += (_, _) =>
        {
            var res = AuthService.Register(_txtLogin.Text, _txtPassword.Text, _txtConfirm.Text);
            MessageBox.Show(res.Message, "Регистрация", MessageBoxButtons.OK,
                res.Ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (res.Ok) Close();
        };
        Controls.Add(btnOk);
    }
}
