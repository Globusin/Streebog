using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConsoleApp1.Stribog;

public class Streebog
{
    private byte[] N;
    private List<List<byte[]>> tables;
    private byte[] tempBytes;

    public Streebog()
    {
        tables = new List<List<byte[]>>();
        for (int i = 0; i < 64; i++)
        {
            string jsonContent = File.ReadAllText($"T_{i}.json");
            var table = JsonConvert.DeserializeObject<List<byte[]>>(jsonContent);
            tables.Add(table);
        }

        tempBytes = new byte[64];
    }

    public byte[] H(byte[] M, int n = 64)
    {
        N = new byte[64];
        byte[] h = new byte[64];
        byte[] m = new byte[64];
        byte[] sigma = new byte[64];

        var l = M.Length;
        while (l > 64)
        {
            Array.Copy(M, l - 64, m, 0, 64);
            h = g(h, m, N);
            N = AddMod512(N, new byte[64]);
            sigma = AddMod512(sigma, m);
            Array.Copy(M, M, 64);
            l -= 64;
        }

        if (l != 64)
        {
            Array.Copy(new byte[64], m, 64 - l - 1);
            m[64 - l - 1] = 1;
            Array.Copy(M, 0, m, 64 - l, l);
        }
        else
            Array.Copy(M, m, 64);

        h = g(N, h, m);
        byte[] lengthInBytes = new byte[64];
        BitConverter.GetBytes(M.Length * 8).CopyTo(lengthInBytes, 0);
        Array.Reverse(lengthInBytes);
        N = AddMod512(N, lengthInBytes);
        sigma = AddMod512(sigma, m);

        var str = Convert.ToHexString(N).ToLower();

        h = g(new byte[64], h, N);
        h = g(new byte[64], h, sigma);

        var result = new byte[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = h[i];
        }
        return result;
    }

    private byte[] AddMod512(byte[] a, byte[] b)
    {
        int temp = 0;
        for (int i = 63; i >= 0; i--)
        {
            temp = a[i] + b[i] + (temp >> 8);
            tempBytes[i] = (byte)(temp & 0xFF);
        }
        return tempBytes;
    }

    private byte[] g(byte[] N, byte[] h, byte[] m)
    {
        return X(X(E(LPS(X(h, N)), m), h), m);
    }

    private byte[] E(byte[] k, byte[] m)
    {
        var result = X(k, m);

        for (var i = 0; i < 12; i++)
        {
            result = LPS(result);
            k = LPS(X(k, Constants.CVector[i]));
            result = X(result, k);
        }

        return result;
    }

    private byte[] X(byte[] a, byte[] b)
    {
        var c = new byte[64];
        var intern = 0;
        for (var i = 63; i >= 0; i--)
        {
            intern = a[i] + b[i] + (intern >> 8);
            c[i] = (byte)intern;
        }

        return c;
    }

    private byte[] LPS(byte[] data)
    {
        var result = new byte[64];

        for (int i = 0; i < 64; i++)
        {
            tempBytes = tables[i][data[i]];
            for (int j = 0; j < 64; j++)
            {
                result[j] ^= tempBytes[j];
            }
        }

        return result;
    }
}