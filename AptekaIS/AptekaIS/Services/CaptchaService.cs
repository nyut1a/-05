namespace AptekaIS.Services;

public static class CaptchaService
{
    private static readonly char[] Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public static string Generate(int length = 5)
    {
        var rnd = Random.Shared;
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = Chars[rnd.Next(Chars.Length)];
        return new string(result);
    }
}
