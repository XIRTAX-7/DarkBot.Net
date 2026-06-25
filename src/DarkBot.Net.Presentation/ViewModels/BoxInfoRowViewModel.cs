using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels;

public sealed partial class BoxInfoRowViewModel : ViewModelBase
{
    public BoxInfoRowViewModel(string name, bool collect, int waitTime, int priority)
    {
        Name = name;
        Collect = collect;
        WaitTime = waitTime;
        Priority = priority;
    }

    public string Name { get; }

    [Reactive] private bool _collect;
    [Reactive] private int _waitTime;
    [Reactive] private int _priority;
}
