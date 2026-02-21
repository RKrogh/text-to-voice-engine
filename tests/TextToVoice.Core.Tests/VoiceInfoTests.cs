namespace TextToVoice.Core.Tests;

public class VoiceInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var voice = new VoiceInfo("Test Voice", "en-US", "Male", "A test voice");

        Assert.Equal("Test Voice", voice.Name);
        Assert.Equal("en-US", voice.Culture);
        Assert.Equal("Male", voice.Gender);
        Assert.Equal("A test voice", voice.Description);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var voice1 = new VoiceInfo("Voice", "en-US", "Female", "Description");
        var voice2 = new VoiceInfo("Voice", "en-US", "Female", "Description");

        Assert.Equal(voice1, voice2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var voice1 = new VoiceInfo("Voice1", "en-US", "Female", "Description");
        var voice2 = new VoiceInfo("Voice2", "en-US", "Female", "Description");

        Assert.NotEqual(voice1, voice2);
    }

    [Fact]
    public void With_CreatesModifiedCopy()
    {
        var original = new VoiceInfo("Original", "en-US", "Male", "Desc");
        var modified = original with { Name = "Modified" };

        Assert.Equal("Original", original.Name);
        Assert.Equal("Modified", modified.Name);
        Assert.Equal(original.Culture, modified.Culture);
    }
}
