using System.Text;

namespace ArkProjects.Hath.ClientApi.Extensions;

public static class StringExtensions
{
    public static string GetSha1AsStr(this string src)
    {
        var srcBytes = Encoding.UTF8.GetBytes(src);
        return srcBytes.GetSha1AsStr();
    }

    public static byte[] HexToBytes(this string src)
    {
        return StringToByteArrayFastest(src);
    }

    public static byte[] StringToByteArrayFastest(string hex)
    {
        if (hex.Length % 2 == 1)
            throw new Exception("The binary key cannot have an odd number of digits");

        byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
        }

        return arr;
    }

    public static int GetHexVal(char hex)
    {
        var val = (int)hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58
                ? 48
                : val < 97
                    ? 55
                    : 87
            );
    }
}