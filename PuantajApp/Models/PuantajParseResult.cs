using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PuantajApp.Models;

public class PuantajParseResult
{
    [JsonPropertyName("ad_soyad")]
    public string AdSoyad { get; set; } = "";

    [JsonPropertyName("unvan")]
    public string Unvan { get; set; } = "";

    [JsonPropertyName("birim")]
    public string Birim { get; set; } = "";

    [JsonPropertyName("yil")]
    public int Yil { get; set; }

    [JsonPropertyName("ay")]
    public int Ay { get; set; }

    [JsonPropertyName("gunler")]
    public List<GunParseResult> Gunler { get; set; } = [];
}

public class GunParseResult
{
    [JsonPropertyName("gun")]
    public int Gun { get; set; }

    [JsonPropertyName("giris")]
    public string? Giris { get; set; }

    [JsonPropertyName("cikis")]
    public string? Cikis { get; set; }

    [JsonPropertyName("mi_yi_r")]
    public string? MiYiR { get; set; }

    [JsonPropertyName("fazla_mesai")]
    public string? FazlaMesai { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}
