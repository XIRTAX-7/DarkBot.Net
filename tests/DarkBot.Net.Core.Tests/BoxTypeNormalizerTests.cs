using DarkBot.Net.Application.Mappers;

namespace DarkBot.Net.Application.Tests;

public sealed class BoxTypeNormalizerTests
{
    [Theory]
    [InlineData("BONUS_BOX", "BONUS_BOX")]
    [InlineData("bonus_box", "BONUS_BOX")]
    [InlineData("box_PROMETID", "PROMETID")]
    [InlineData("ore_endurium", "ENDURIUM")]
    [InlineData("PROMETID,suffix", "PROMETID")]
    [InlineData("FROM_SHIP", "FROM_SHIP")]
    public void Normalize_maps_game_labels_to_config_keys(string raw, string expected) =>
        Assert.Equal(expected, BoxTypeNormalizer.Normalize(raw));
}
