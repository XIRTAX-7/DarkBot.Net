using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.BotEngine.Statistics;

internal class StatImpl : IStatsApi.IStat
{
    private readonly Func<bool> _trackDiff;
    private double _initial = double.NaN;
    private double _earned;
    private double _spent;
    private double _current;
    private readonly SimpleTimeSeries _timeSeries = new();

    public StatImpl(Func<bool> trackDiff) => _trackDiff = trackDiff;

    public double Initial => _initial;
    public double Earned => _earned;
    public double Spent => _spent;
    public double Current => _current;
    public IStatsApi.ITimeSeries? TimeSeries => _timeSeries;

    public double Track(double value)
    {
        var diff = value - _current;

        if (double.IsNaN(_initial))
            _initial = value;
        else if (_trackDiff())
        {
            if (diff > 0)
                _earned += diff;
            else
                _spent -= diff;
        }

        _current = value;
        _timeSeries.Track(_earned - _spent);
        return diff;
    }

    public void Reset()
    {
        _earned = 0;
        _spent = 0;
    }
}

internal sealed class AverageStatImpl : StatImpl
{
    private double _average;
    private double _max = double.MinValue;
    private long _lastTime = Environment.TickCount64;

    public AverageStatImpl() : base(() => true) { }

    public double Average => _average;
    public double Max => _max;

    public new double Track(double value)
    {
        var now = Environment.TickCount64;
        var diff = base.Track(value);

        var adjustFactor = (now - _lastTime) / 10_000d;
        _average += adjustFactor * (value - _average);
        _max += adjustFactor * 0.2 * (_average - _max);
        _max = Math.Max(_max, value);

        _lastTime = now;
        return diff;
    }
}

internal sealed class SimpleTimeSeries : IStatsApi.ITimeSeries
{
    private readonly List<long> _time = [];
    private readonly List<double> _value = [];

    public IReadOnlyList<long> Time => _time;
    public IReadOnlyList<double> Value => _value;

    public void Track(double value)
    {
        _time.Add(Environment.TickCount64);
        _value.Add(value);
    }
}

internal sealed record StatKey(string? Namespace, string Category, string Name) : IStatsApi.IStatKey
{
    public static StatKey From(IStatsApi.IStatKey key) =>
        key is StatKey existing ? existing : new(key.Namespace, key.Category, key.Name);
}
