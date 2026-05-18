using AptekaIS.Data;
using AptekaIS.Services;

namespace AptekaIS.Forms;

public class LoginForm : Form
{
    private readonly TextBox _txtLogin = new() { Width = 220 };
    private readonly TextBox _txtPassword = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly TextBox _txtCaptcha = new() { Width = 120 };
    private readonly Label _lblCaptcha = new() { AutoSize = true, Font = new Font("Consolas", 14, FontStyle.Bold) };
    private string _captchaCode = "";

    public LoginForm()
    {
        Text = "Вход — Аптека ИС";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 280);

        RefreshCaptcha();

        var y = 20;
        Controls.Add(new Label { Text = "Логин:", Location = new Point(30, y), AutoSize = true });
        _txtLogin.Location = new Point(130, y - 3);
        Controls.Add(_txtLogin);
        y += 40;

        Controls.Add(new Label { Text = "Пароль:", Location = new Point(30, y), AutoSize = true });
        _txtPassword.Location = new Point(130, y - 3);
        Controls.Add(_txtPassword);
        y += 40;

        Controls.Add(new Label { Text = "CAPTCHA:", Location = new Point(30, y), AutoSize = true });
        _lblCaptcha.Location = new Point(130, y);
        Controls.Add(_lblCaptcha);
        var btnRefresh = new Button { Text = "↻", Location = new Point(230, y - 2), Size = new Size(30, 25) };
        btnRefresh.Click += (_, _) => RefreshCaptcha();
        Controls.Add(btnRefresh);
        y += 35;
        _txtCaptcha.Location = new Point(130, y);
        Controls.Add(_txtCaptcha);
        y += 45;

        var btnLogin = new Button { Text = "Войти", Location = new Point(130, y), Size = new Size(90, 30) };
        btnLogin.Click += BtnLogin_Click;
        Controls.Add(btnLogin);

        var btnRegister = new Button { Text = "Регистрация", Location = new Point(230, y), Size = new Size(100, 30) };
        btnRegister.Click += (_, _) =>
        {
            using var f = new RegisterForm();
            f.ShowDialog();
        };
        Controls.Add(btnRegister);

        AcceptButton = btnLogin;
    }

    private void RefreshCaptcha()
    {
        _captchaCode = CaptchaService.Generate();
        _lblCaptcha.Text = _captchaCode;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        _lblCaptcha.Text = _captchaCode;

        if (!_txtCaptcha.Text.Trim().Equals(_captchaCode, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Неверный код CAPTCHA.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshCaptcha();
            _lblCaptcha.Text = _captchaCode;
            _txtCaptcha.Clear();
            return;
        }

        var result = AuthService.Login(_txtLogin.Text, _txtPassword.Text);
        if (!result.Ok)
        {
            MessageBox.Show(result.Message, "Вход", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshCaptcha();
            _lblCaptcha.Text = _captchaCode;
            _txtCaptcha.Clear();
            return;
        }

        Session.CurrentUser = result.User;
        Hide();
        using var main = new MainForm();
        main.ShowDialog();
        Session.CurrentUser = null;
        RefreshCaptcha();
        _lblCaptcha.Text = _captchaCode;
        _txtLogin.Clear();
        _txtPassword.Clear();
        _txtCaptcha.Clear();
        Show();
    }
}
