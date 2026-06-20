namespace DarkBot.Net.Api.Game;

public interface IHealth
{
    int Hp { get; }
    int Hull { get; }
    int Shield { get; }
    int MaxHp { get; }
    int MaxHull { get; }
    int MaxShield { get; }

    double HpPercent => MaxHp == 0 ? 1 : (double)Hp / MaxHp;
    double HullPercent => MaxHull == 0 ? 1 : (double)Hull / MaxHull;
    double ShieldPercent => MaxShield == 0 ? 1 : (double)Shield / MaxShield;

    bool HpDecreasedIn(int timeMs);
    bool HullDecreasedIn(int timeMs);
    bool ShieldDecreasedIn(int timeMs);
    bool HpIncreasedIn(int timeMs);
    bool HullIncreasedIn(int timeMs);
    bool ShieldIncreasedIn(int timeMs);
}
