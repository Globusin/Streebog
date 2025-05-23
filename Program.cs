using ConsoleApp1.Stribog;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text;

//var strib = new Streebog();

//int bufferSize = 1 * 1024 * 1024;
//byte[] data = new byte[bufferSize];
//new Random().NextBytes(data);

//var stopwatch = Stopwatch.StartNew();
//var hash = strib.H(data, 64);
//stopwatch.Stop();

//double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
//double speedMBperSecond = (bufferSize / (1024.0 * 1024.0)) / elapsedSeconds;

//Console.WriteLine($"Размер данных: {bufferSize / (1024 * 1024)} MB");
//Console.WriteLine($"Время: {elapsedSeconds:F2} сек");
//Console.WriteLine($"Скорость: {speedMBperSecond:F2} MB/s");
//Console.WriteLine($"Хеш: {Convert.ToHexString(hash).ToLower()}");
//return;

class Program
{
    private static Random random = new Random();
    private static Streebog strib = new Streebog();

    static string GenerateRandomMessage()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        int length = random.Next(10, 50);

        var message = new char[length];
        for (int i = 0; i < length; i++)
        {
            message[i] = chars[random.Next(chars.Length)];
        }

        return new string(message);
    }

    static (string, string) FindCollisionBasic(int n)
    {
        var hashes = new Dictionary<string, string>();
        while (true)
        {
            string msg = GenerateRandomMessage();
            byte[] hash = strib.H(Encoding.UTF8.GetBytes(msg), n);

            string hexHash = BitConverter.ToString(hash).ToLower();

            if (hashes.TryGetValue(hexHash, out string existingMsg))
            {
                return (existingMsg, msg);
            }

            hashes[hexHash] = msg;
        }
    }

    static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }

    static (byte[], byte[]) FindCollisionIterative(int n)
    {
        byte[] x0 = GenerateRandomBytes(n + 1);
        byte[] x = x0;
        byte[] x_prime = x0;

        int i = 0;
        while (true)
        {
            x = strib.H(x, n);
            x_prime = strib.H(strib.H(x_prime, n), n);

            if (ByteArraysEqual(x, x_prime))
                break;

            i++;
        }

        x_prime = x;
        x = x0;
        for (int j = 0; j <= i; j++)
        {
            byte[] h_x = strib.H(x, n);
            byte[] h_x_prime = strib.H(x_prime, n);

            if (ByteArraysEqual(h_x, h_x_prime))
            {
                if (!ByteArraysEqual(x, x_prime))
                {
                    return (x, x_prime);
                }
            }

            x = h_x;
            x_prime = h_x_prime;
        }

        throw new Exception("Не найдена");
    }

    static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static void FindMeaningfulCollision(int n)
    {
        Bitmap image1 = new Bitmap("cat.jpg");
        Bitmap image2 = new Bitmap("dog.jpg");
        
        var dict = new Dictionary<string, byte[]>();
        
        for (int i = 0;; i++) //i < (1 << 8 * n)
        {
            var bytes1 = GetBytesFromBitmap(image1);
            var hash1 = BitConverter.ToString(strib.H(bytes1, n)).ToLower();
            if (dict.TryGetValue(hash1, out var val1))
            {
                File.WriteAllBytes($"{n}collision1.jpg", bytes1);
                File.WriteAllBytes($"{n}collision2.jpg", val1);
                Console.WriteLine(i);
                break;
            }
            dict[hash1] = bytes1;
            
            var bytes2 = GetBytesFromBitmap(image2);
            var hash2 = BitConverter.ToString(strib.H(bytes2, n)).ToLower();
            if (dict.TryGetValue(hash2, out var val2))
            {
                File.WriteAllBytes($"{n}collision1.jpg", bytes2);
                File.WriteAllBytes($"{n}collision2.jpg", val2);
                Console.WriteLine(i);
                break;
            }
            dict[hash2] = bytes2;
            
            image1 = ModifyImage(image1, i, n);
            image2 = ModifyImage(image2, i, n);
        }
    }
    
    static Bitmap ModifyImage(Bitmap original, int pattern, int bitsToEncode)
    {
        Bitmap modified = new Bitmap(original);
        
        for (int x = 0; x < modified.Width; x++)
        {
            for (int y = 0; y < modified.Height; y++)
            {
                Color pixel = modified.GetPixel(x, y);
                
                int newBlue = (pixel.B & 0xFE) | ((pattern >> ((x * modified.Height + y) % bitsToEncode)) & 1);
                Color newPixel = Color.FromArgb(pixel.R, pixel.G, newBlue);
                
                modified.SetPixel(x, y, newPixel);
            }
        }
        
        return modified;
    }
    
    static byte[] GetBytesFromBitmap(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Jpeg);
        return stream.ToArray();
    }
    static void Main(string[] args)
    {
        var (msg1, msg2) = FindCollisionBasic(3);
        Console.WriteLine($"Найдена коллизия:\nM1: {msg1}\nM2: {msg2}");

        var (msg_1, msg_2) = FindCollisionIterative(2);
        Console.WriteLine($"Найдена коллизия:\nM1: {BitConverter.ToString(msg_1).ToLower()}\nM2: {BitConverter.ToString(msg_2).ToLower()}");

        FindMeaningfulCollision(2);
    }
}