using DarkBot.Net.Application.BotEngine.Safety;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;

namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>MVP сбор ресурсов — порт eu.darkbot.shared.modules.CollectorModule.</summary>
public sealed class CollectorModule(ModuleContext context, SafetyFinder safetyFinder) : IModule
{
    private const int FindBoxReachabilityDistance = 200;
    private const int CollectDistance = 250;
    private const int ArrivedAtDestinationDistance = 20;

    private IBox? _currentBox;
    private long _waitingUntil;

    public IBox? CurrentBox => _currentBox;

    public string? GetStatus()
    {
        if (_currentBox is null)
            return "Roaming";

        if (IsWaiting())
            return $"Collecting {_currentBox.TypeName} {Math.Max(0, _waitingUntil - Environment.TickCount64)}ms";

        return $"Moving to {_currentBox.TypeName}";
    }

    public void OnTickModule()
    {
        if (!IsNotWaiting() || !CheckMapAndSafety())
            return;

        context.Hero.SetRoamMode();
        FindBox();

        if (!TryCollectNearestBox()
            && (context.Hero.DistanceTo(context.Movement.Destination) < ArrivedAtDestinationDistance
                || context.Movement.IsOutOfMap))
        {
            context.Movement.MoveRandom();
        }
    }

    private bool CheckMapAndSafety()
    {
        if (!safetyFinder.Tick())
            return false;

        return IsOnWorkingMap();
    }

    private bool IsOnWorkingMap()
    {
        var workingMap = context.Config.GetConfigValue<int>("general.working_map");
        if (workingMap <= 0)
            return true;

        return context.Map.MapId == workingMap;
    }

    private void FindBox()
    {
        var hero = context.Hero;
        var best = context.Entities.Boxes
            .Where(CanCollect)
            .OrderBy(box => box.Info.Priority)
            .ThenBy(hero.DistanceTo)
            .FirstOrDefault();

        _currentBox = _currentBox is null || best is null || _currentBox.IsCollected || IsBetter(best)
            ? best
            : _currentBox;
    }

    private bool CanCollect(IBox box) =>
        box.Info.ShouldCollect
        && !box.IsCollected
        && box.IsValid
        && context.Movement.GetClosestDistance(box) < FindBoxReachabilityDistance
        && (!IsResource(box.TypeName) || context.Stats.Cargo < context.Stats.MaxCargo);

    private static bool IsResource(string typeName) =>
        typeName.Equals("FROM_SHIP", StringComparison.OrdinalIgnoreCase)
        || typeName.Equals("PROSPEROUS_CARGO", StringComparison.OrdinalIgnoreCase);

    private bool TryCollectNearestBox()
    {
        if (_currentBox is null)
            return false;

        CollectBox();
        return true;
    }

    private void CollectBox()
    {
        if (_currentBox is null)
            return;

        var hero = context.Hero;
        var distance = hero.DistanceTo(_currentBox);

        if (distance < CollectDistance)
        {
            context.Movement.Stop(currentLocation: false);
            if (!_currentBox.TryCollect())
                return;

            _waitingUntil = Environment.TickCount64
                + _currentBox.Info.WaitTime
                + Math.Min(1_000, _currentBox.Retries * 100)
                + hero.TimeTo(distance)
                + 30;
        }
        else
        {
            context.Movement.MoveTo(_currentBox);
        }
    }

    private bool IsWaiting()
    {
        if (_currentBox is null || !_currentBox.IsValid)
        {
            _waitingUntil = 0;
            return false;
        }

        return Environment.TickCount64 <= _waitingUntil;
    }

    private bool IsNotWaiting() => !IsWaiting();

    private bool IsBetter(IBox box)
    {
        if (_currentBox is null)
            return true;

        var hero = context.Hero;
        var currentDistance = hero.DistanceTo(_currentBox);
        return currentDistance > 100 && currentDistance - 150 > hero.DistanceTo(box);
    }
}
