using System;
using System.IO;

namespace PuantajApp.Services;

public static class EnvService
{
    public static void Load(string envDosyaYolu = ".env")
    {
        if (!File.Exists(envDosyaYolu)) return;

        foreach (var satir in File.ReadAllLines(envDosyaYolu))
        {
            var temiz = satir.Trim();
            if (string.IsNullOrEmpty(temiz) || temiz.StartsWith('#')) continue;

            var esitPos = temiz.IndexOf('=');
            if (esitPos < 0) continue;

            var key = temiz[..esitPos].Trim();
            var val = temiz[(esitPos + 1)..].Trim();
            Environment.SetEnvironmentVariable(key, val);
        }
    }

    public static string? Get(string key) => Environment.GetEnvironmentVariable(key);

    public static void Set(string key, string value, string envDosyaYolu = ".env")
    {
        Environment.SetEnvironmentVariable(key, value);

        // .env dosyasina yaz
        var satirlar = File.Exists(envDosyaYolu)
            ? new System.Collections.Generic.List<string>(File.ReadAllLines(envDosyaYolu))
            : new System.Collections.Generic.List<string>();

        var bulundu = false;
        for (int i = 0; i < satirlar.Count; i++)
        {
            var temiz = satirlar[i].Trim();
            if (temiz.StartsWith(key + "=") || temiz.StartsWith(key + " ="))
            {
                satirlar[i] = $"{key}={value}";
                bulundu = true;
                break;
            }
        }

        if (!bulundu)
            satirlar.Add($"{key}={value}");

        File.WriteAllLines(envDosyaYolu, satirlar);
    }
}
