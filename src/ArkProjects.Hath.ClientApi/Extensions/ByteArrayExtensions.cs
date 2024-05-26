using System.Security.Cryptography;
using System.Text;

namespace ArkProjects.Hath.ClientApi.Extensions;

public static class ByteArrayExtensions
{
    public static byte[] GetSha1(this byte[] src)
    {
        return SHA1.HashData(src);
    }

    public static string GetSha1AsStr(this byte[] src)
    {
        return BytesToHex(src.GetSha1());
    }

    public static string BytesToHex(this byte[] data)
    {
        var sb = new StringBuilder(data.Length * 2);
        foreach (var b in data)
            sb.Append(b.ToString("X2"));
        return sb.ToString().ToLower();
    }
}