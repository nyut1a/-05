using AptekaIS.Data;
using AptekaIS.Models;

namespace AptekaIS.Services;

public static class AuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);

    public static (bool Ok, string Message) Register(string login, string password, string confirm)
    {
        login = login.Trim();
        if (login.Length < 3)
            return (false, "Логин должен быть не короче 3 символов.");
        if (password.Length < 6)
            return (false, "Пароль должен быть не короче 6 символов.");
        if (password != confirm)
            return (false, "Пароли не совпадают.");
        if (Database.LoginExists(login))
            return (false, "Такой логин уже занят.");

        var (hash, salt) = PasswordHasher.HashPassword(password);
        Database.CreateUser(login, hash, salt, roleId: 3);
        return (true, "Регистрация выполнена. Можно войти в систему.");
    }

    public static (bool Ok, string Message, User? User) Login(string login, string password)
    {
        login = login.Trim();
        if (IsLockedOut(login))
            return (false, $"Слишком много неудачных попыток. Повторите через {(int)LockoutDuration.TotalMinutes} мин.", null);

        var user = Database.GetUserByLogin(login);
        if (user == null || !user.IsActive)
        {
            Database.AddLoginAttempt(login, false, "Пользователь не найден или заблокирован");
            return (false, "Неверный логин или пароль.", null);
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            Database.AddLoginAttempt(login, false, "Неверный пароль");
            return (false, "Неверный логин или пароль.", null);
        }

        Database.AddLoginAttempt(login, true, "Успешный вход");
        return (true, "OK", user);
    }

    public static bool IsLockedOut(string login)
    {
        var failed = Database.CountFailedAttempts(login, AttemptWindow);
        if (failed < MaxFailedAttempts) return false;
        var lastFail = Database.GetLastFailedAttemptTime(login);
        if (lastFail == null) return false;
        return DateTime.Now - lastFail.Value < LockoutDuration;
    }
}
