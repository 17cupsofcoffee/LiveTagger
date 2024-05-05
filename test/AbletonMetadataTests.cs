namespace LiveTagger.UnitTests;

public class AbletonMetadataTests
{
    [Theory]
    [InlineData("C:/foo/Ableton Folder Info/file.txt", true)]
    [InlineData("C:/foo/metadata.asd", true)]
    [InlineData("C:/foo/sound.wav", false)]
    public void ShouldDetectMetadata(string path, bool isValid)
    {
        Assert.Equal(isValid, AbletonMetadata.IsMetadata(path));
    }

    [Fact]
    public void ShouldReturnXmpFilePath()
    {
        var xmpPath = AbletonMetadata.GetXmpFilePath("C:/foo");

        Assert.Equivalent(new FileInfo("C:/foo/Ableton Folder Info/dc66a3fa-0fe1-5352-91cf-3ec237e9ee90.xmp"), new FileInfo(xmpPath));
    }

    [Theory]
    [InlineData("sound.wav", true)]
    [InlineData("sound.aiff", true)]
    [InlineData("sound.flac", true)]
    [InlineData("sound.ogg", true)]
    [InlineData("sound.mp3", true)]
    [InlineData("sound.mp4", true)]
    [InlineData("sound.m4a", true)]
    [InlineData("sound.exe", false)]
    public void ShouldDetectSampleFiles(string filename, bool isValid)
    {
        Assert.Equal(isValid, AbletonMetadata.IsSupportedSampleFormat(filename));
    }
}