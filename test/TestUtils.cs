using System.Runtime.CompilerServices;

namespace LiveTagger.Tests;

/// <summary>
/// Utilities for testing.
/// 
/// Based on https://www.honlsoft.com/blog/2022-03-26-unit-testing-reading-reference-data/.
/// </summary>
public static class TestUtils
{
    public static string ReadFileAsString(string file, [CallerFilePath] string filePath = "")
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        var fullPath = Path.Join(directoryPath, file);
        return File.ReadAllText(fullPath);
    }
}