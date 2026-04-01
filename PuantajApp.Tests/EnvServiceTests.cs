using PuantajApp.Services;

namespace PuantajApp.Tests;

public class EnvServiceTests : IDisposable
{
    private readonly string _testEnvPath;

    public EnvServiceTests()
    {
        _testEnvPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.env");
    }

    public void Dispose()
    {
        if (File.Exists(_testEnvPath))
            File.Delete(_testEnvPath);
    }

    // === Load ===

    [Fact]
    public void Load_ValidFile_SetsEnvironmentVariables()
    {
        File.WriteAllText(_testEnvPath, "TEST_KEY_1=value1\nTEST_KEY_2=value2");
        EnvService.Load(_testEnvPath);

        Assert.Equal("value1", Environment.GetEnvironmentVariable("TEST_KEY_1"));
        Assert.Equal("value2", Environment.GetEnvironmentVariable("TEST_KEY_2"));

        // Cleanup
        Environment.SetEnvironmentVariable("TEST_KEY_1", null);
        Environment.SetEnvironmentVariable("TEST_KEY_2", null);
    }

    [Fact]
    public void Load_CommentsIgnored()
    {
        File.WriteAllText(_testEnvPath, "# This is a comment\nTEST_COMMENT_KEY=val");
        EnvService.Load(_testEnvPath);

        Assert.Equal("val", Environment.GetEnvironmentVariable("TEST_COMMENT_KEY"));
        Environment.SetEnvironmentVariable("TEST_COMMENT_KEY", null);
    }

    [Fact]
    public void Load_EmptyLinesIgnored()
    {
        File.WriteAllText(_testEnvPath, "\n\nTEST_EMPTY_KEY=val\n\n");
        EnvService.Load(_testEnvPath);

        Assert.Equal("val", Environment.GetEnvironmentVariable("TEST_EMPTY_KEY"));
        Environment.SetEnvironmentVariable("TEST_EMPTY_KEY", null);
    }

    [Fact]
    public void Load_NonExistentFile_DoesNotThrow()
    {
        var exception = Record.Exception(() => EnvService.Load("/nonexistent/path/.env"));
        Assert.Null(exception);
    }

    // === Get ===

    [Fact]
    public void Get_ExistingVar_ReturnsValue()
    {
        Environment.SetEnvironmentVariable("TEST_GET_VAR", "hello");
        Assert.Equal("hello", EnvService.Get("TEST_GET_VAR"));
        Environment.SetEnvironmentVariable("TEST_GET_VAR", null);
    }

    [Fact]
    public void Get_NonExistentVar_ReturnsNull()
    {
        Assert.Null(EnvService.Get("NONEXISTENT_VAR_XYZ_123"));
    }

    // === Set ===

    [Fact]
    public void Set_NewKey_AppendsToFile()
    {
        File.WriteAllText(_testEnvPath, "EXISTING=old");
        EnvService.Set("NEW_KEY", "new_value", _testEnvPath);

        var content = File.ReadAllText(_testEnvPath);
        Assert.Contains("EXISTING=old", content);
        Assert.Contains("NEW_KEY=new_value", content);

        // Environment variable da set edilmis olmali
        Assert.Equal("new_value", Environment.GetEnvironmentVariable("NEW_KEY"));
        Environment.SetEnvironmentVariable("NEW_KEY", null);
    }

    [Fact]
    public void Set_ExistingKey_UpdatesValue()
    {
        File.WriteAllText(_testEnvPath, "MY_KEY=old_value");
        EnvService.Set("MY_KEY", "new_value", _testEnvPath);

        var lines = File.ReadAllLines(_testEnvPath);
        Assert.Single(lines);
        Assert.Equal("MY_KEY=new_value", lines[0]);

        Environment.SetEnvironmentVariable("MY_KEY", null);
    }

    [Fact]
    public void Set_NonExistentFile_CreatesFile()
    {
        var newPath = Path.Combine(Path.GetTempPath(), $"new_{Guid.NewGuid()}.env");
        try
        {
            EnvService.Set("CREATED_KEY", "created_value", newPath);

            Assert.True(File.Exists(newPath));
            Assert.Contains("CREATED_KEY=created_value", File.ReadAllText(newPath));
        }
        finally
        {
            if (File.Exists(newPath)) File.Delete(newPath);
            Environment.SetEnvironmentVariable("CREATED_KEY", null);
        }
    }
}
