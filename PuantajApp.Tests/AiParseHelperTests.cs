using System.Text.Json;
using PuantajApp.Services;

namespace PuantajApp.Tests;

public class AiParseHelperTests
{
    // === TemizleJson ===

    [Fact]
    public void TemizleJson_JsonCodeBlock_RemovesMarkers()
    {
        var input = "```json\n{\"key\": \"value\"}\n```";
        Assert.Equal("{\"key\": \"value\"}", AiParseHelper.TemizleJson(input));
    }

    [Fact]
    public void TemizleJson_GenericCodeBlock_RemovesMarkers()
    {
        var input = "```\n{\"key\": 1}\n```";
        Assert.Equal("{\"key\": 1}", AiParseHelper.TemizleJson(input));
    }

    [Fact]
    public void TemizleJson_PlainJson_Unchanged()
    {
        var input = "{\"key\": 1}";
        Assert.Equal("{\"key\": 1}", AiParseHelper.TemizleJson(input));
    }

    [Fact]
    public void TemizleJson_WhitespaceAround_Trimmed()
    {
        var input = "  {\"a\": 1}  ";
        Assert.Equal("{\"a\": 1}", AiParseHelper.TemizleJson(input));
    }

    // === NormalizeSaat ===

    [Theory]
    [InlineData("09:00", "09:00")]
    [InlineData("9:00", "09:00")]
    [InlineData("18:0", "18:00")]
    [InlineData("8:5", "08:05")]
    [InlineData("23:59", "23:59")]
    [InlineData("0:0", "00:00")]
    public void NormalizeSaat_ValidTimes_Normalizes(string input, string expected)
    {
        Assert.Equal(expected, AiParseHelper.NormalizeSaat(input));
    }

    [Fact]
    public void NormalizeSaat_Null_ReturnsNull()
    {
        Assert.Null(AiParseHelper.NormalizeSaat(null));
    }

    [Fact]
    public void NormalizeSaat_InvalidFormat_ReturnsOriginal()
    {
        Assert.Equal("abc", AiParseHelper.NormalizeSaat("abc"));
    }

    [Fact]
    public void NormalizeSaat_OutOfRange_ReturnsOriginal()
    {
        Assert.Equal("25:00", AiParseHelper.NormalizeSaat("25:00"));
    }

    // === NormalizeMiYiR ===

    [Theory]
    [InlineData("mi", "mi")]
    [InlineData("Mi", "mi")]
    [InlineData("MI", "mi")]
    [InlineData("yi", "yi")]
    [InlineData("YI", "yi")]
    [InlineData("r", "r")]
    [InlineData("R", "r")]
    public void NormalizeMiYiR_ValidValues_Normalizes(string input, string expected)
    {
        Assert.Equal(expected, AiParseHelper.NormalizeMiYiR(input));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("izin")]
    [InlineData("")]
    public void NormalizeMiYiR_InvalidValues_ReturnsNull(string input)
    {
        Assert.Null(AiParseHelper.NormalizeMiYiR(input));
    }

    [Fact]
    public void NormalizeMiYiR_Null_ReturnsNull()
    {
        Assert.Null(AiParseHelper.NormalizeMiYiR(null));
    }

    // === ParseAy ===

    [Fact]
    public void ParseAy_NumericValue_ReturnsParsed()
    {
        var json = JsonDocument.Parse("{\"ay\": 3}");
        Assert.Equal(3, AiParseHelper.ParseAy(json.RootElement));
    }

    [Fact]
    public void ParseAy_StringNumber_ReturnsParsed()
    {
        var json = JsonDocument.Parse("{\"ay\": \"5\"}");
        Assert.Equal(5, AiParseHelper.ParseAy(json.RootElement));
    }

    [Theory]
    [InlineData("OCAK", 1)]
    [InlineData("SUBAT", 2)]
    [InlineData("ŞUBAT", 2)]
    [InlineData("MART", 3)]
    [InlineData("NISAN", 4)]
    [InlineData("MAYIS", 5)]
    [InlineData("HAZIRAN", 6)]
    [InlineData("TEMMUZ", 7)]
    [InlineData("AGUSTOS", 8)]
    [InlineData("EYLUL", 9)]
    [InlineData("EKIM", 10)]
    [InlineData("KASIM", 11)]
    [InlineData("ARALIK", 12)]
    public void ParseAy_TurkishMonthNames_ReturnsParsed(string ay, int expected)
    {
        var json = JsonDocument.Parse($"{{\"ay\": \"{ay}\"}}");
        Assert.Equal(expected, AiParseHelper.ParseAy(json.RootElement));
    }

    [Fact]
    public void ParseAy_MissingProperty_ReturnsZero()
    {
        var json = JsonDocument.Parse("{}");
        Assert.Equal(0, AiParseHelper.ParseAy(json.RootElement));
    }

    // === ParseGunNumarasi ===

    [Fact]
    public void ParseGunNumarasi_NumericValue_ReturnsParsed()
    {
        var json = JsonDocument.Parse("{\"gun\": 15}");
        Assert.Equal(15, AiParseHelper.ParseGunNumarasi(json.RootElement));
    }

    [Fact]
    public void ParseGunNumarasi_StringWithText_ExtractsNumber()
    {
        var json = JsonDocument.Parse("{\"gun\": \"1 Ocak 2026 Persembe\"}");
        Assert.Equal(1, AiParseHelper.ParseGunNumarasi(json.RootElement));
    }

    [Fact]
    public void ParseGunNumarasi_StringNumber_ReturnsParsed()
    {
        var json = JsonDocument.Parse("{\"gun\": \"25\"}");
        Assert.Equal(25, AiParseHelper.ParseGunNumarasi(json.RootElement));
    }

    // === ParseJsonResponse ===

    [Fact]
    public void ParseJsonResponse_ValidJson_ReturnsResult()
    {
        var json = """
        {
            "ad_soyad": "AYSEL KOKSALDI",
            "unvan": "Operator",
            "birim": "Pasaport",
            "yil": 2026,
            "ay": 1,
            "gunler": [
                {"gun": 1, "giris": "09:00", "cikis": "18:00", "mi_yi_r": null, "fazla_mesai": null, "aciklama": null},
                {"gun": 2, "giris": "08:30", "cikis": "17:30", "mi_yi_r": "yi", "fazla_mesai": "19:00-22:00", "aciklama": "Ek mesai"}
            ]
        }
        """;

        var result = AiParseHelper.ParseJsonResponse(json);

        Assert.NotNull(result);
        Assert.Equal("AYSEL KOKSALDI", result.AdSoyad);
        Assert.Equal("Operator", result.Unvan);
        Assert.Equal("Pasaport", result.Birim);
        Assert.Equal(2026, result.Yil);
        Assert.Equal(1, result.Ay);
        Assert.Equal(2, result.Gunler.Count);
        Assert.Equal("09:00", result.Gunler[0].Giris);
        Assert.Equal("18:00", result.Gunler[0].Cikis);
        Assert.Null(result.Gunler[0].MiYiR);
        Assert.Equal("yi", result.Gunler[1].MiYiR);
        Assert.Equal("19:00-22:00", result.Gunler[1].FazlaMesai);
    }

    [Fact]
    public void ParseJsonResponse_ArrayWrapped_ReturnsFirstElement()
    {
        var json = """[{"ad_soyad": "TEST", "unvan": "", "birim": "", "yil": 2026, "ay": 1, "gunler": []}]""";
        var result = AiParseHelper.ParseJsonResponse(json);
        Assert.NotNull(result);
        Assert.Equal("TEST", result.AdSoyad);
    }

    [Fact]
    public void ParseJsonResponse_CodeBlockWrapped_ParsesCorrectly()
    {
        var json = "```json\n{\"ad_soyad\": \"TEST\", \"unvan\": \"\", \"birim\": \"\", \"yil\": 2026, \"ay\": 1, \"gunler\": []}\n```";
        var result = AiParseHelper.ParseJsonResponse(json);
        Assert.NotNull(result);
        Assert.Equal("TEST", result.AdSoyad);
    }

    // === GetStringProp ===

    [Fact]
    public void GetStringProp_ExistingProp_ReturnsValue()
    {
        var json = JsonDocument.Parse("{\"name\": \"test\"}");
        Assert.Equal("test", AiParseHelper.GetStringProp(json.RootElement, "name"));
    }

    [Fact]
    public void GetStringProp_NullProp_ReturnsNull()
    {
        var json = JsonDocument.Parse("{\"name\": null}");
        Assert.Null(AiParseHelper.GetStringProp(json.RootElement, "name"));
    }

    [Fact]
    public void GetStringProp_MissingProp_ReturnsNull()
    {
        var json = JsonDocument.Parse("{}");
        Assert.Null(AiParseHelper.GetStringProp(json.RootElement, "name"));
    }

    [Fact]
    public void GetStringProp_WhitespaceProp_ReturnsNull()
    {
        var json = JsonDocument.Parse("{\"name\": \"   \"}");
        Assert.Null(AiParseHelper.GetStringProp(json.RootElement, "name"));
    }
}
