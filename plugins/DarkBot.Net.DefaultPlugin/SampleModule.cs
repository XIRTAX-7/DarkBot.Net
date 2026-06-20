using DarkBot.Net.Api.Config;
using DarkBot.Net.Api.Config.Types;
using DarkBot.Net.Api.Game.Items;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.DefaultPlugin;

[Feature("Sample module", "Module that does nothing, just to show how to create a module")]
public sealed class SampleModule : IModule, IConfigurable<SampleModule.SampleConfig>
{
    private readonly IMovementApi _movement;
    private SampleConfig _config = new();

    public SampleModule(IMovementApi movement) => _movement = movement;

    public bool CanRefresh() => true;

    public string? Status =>
        $"Sample module - Moving: {_config.MoveShip} - {_config.PercentageValue * 100:0}%";

    public void OnTickModule()
    {
        if (_config.MoveShip && !_movement.IsMoving)
            _movement.MoveRandom();
    }

    public void SetConfig(IConfigSetting<SampleConfig> config) =>
        _config = config.Value;

    public sealed class SampleConfig
    {
        public bool BooleanValue { get; set; } = true;
        public int IntegerLimit { get; set; } = 1;
        public double PercentageValue { get; set; } = 0.5;
        public char Key { get; set; }
        public bool MoveShip { get; set; }
        public ShipMode ShipMode { get; set; } =
            ShipMode.Of(HeroConfiguration.First, ISelectableItem.Formation.Standard);
        public PercentRange PercentRange { get; set; } = PercentRange.Of(0.5, 0.75);
    }
}
