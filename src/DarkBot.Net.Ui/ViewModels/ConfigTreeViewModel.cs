using CommunityToolkit.Mvvm.ComponentModel;
using DarkBot.Net.Api.Config;
using DarkBot.Net.Api.Managers;

namespace DarkBot.Net.Ui.ViewModels;

public sealed partial class ConfigTreeNodeViewModel : ViewModelBase
{
    public ConfigTreeNodeViewModel(IConfigSetting<object> setting, string path)
    {
        Setting = setting;
        Path = path;
        Name = setting.Name;
        Key = setting.Key;
        IsParent = setting is IConfigSettingParent parent && parent.Children.Count > 0;

        if (setting is IConfigSettingParent p)
        {
            foreach (var child in p.Children.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                var childPath = string.IsNullOrEmpty(path) ? child.Key : $"{path}.{child.Key}";
                Children.Add(new ConfigTreeNodeViewModel(child, childPath));
            }
        }

        UpdateEditorValue();
    }

    public IConfigSetting<object> Setting { get; }
    public string Path { get; }
    public string Name { get; }
    public string Key { get; }
    public bool IsParent { get; }
    public IList<ConfigTreeNodeViewModel> Children { get; } = new List<ConfigTreeNodeViewModel>();

    [ObservableProperty]
    private string _editorValue = string.Empty;

    [ObservableProperty]
    private bool _isBoolean;

    [ObservableProperty]
    private bool _boolValue;

    public void UpdateEditorValue()
    {
        if (Setting.ValueType == typeof(bool))
        {
            IsBoolean = true;
            BoolValue = Setting.Value is bool b && b;
            EditorValue = BoolValue.ToString();
            return;
        }

        IsBoolean = false;
        EditorValue = Setting.Value?.ToString() ?? string.Empty;
    }

    partial void OnBoolValueChanged(bool value)
    {
        if (!IsBoolean)
            return;

        Setting.Value = value;
        EditorValue = value.ToString();
    }

    partial void OnEditorValueChanged(string value)
    {
        if (IsBoolean)
            return;

        Setting.Value = ConvertValue(value, Setting.ValueType) ?? Setting.Value;
    }

    private static object? ConvertValue(string text, Type type)
    {
        if (type == typeof(string))
            return text;

        if (type == typeof(int) && int.TryParse(text, out var i))
            return i;

        if (type == typeof(double) && double.TryParse(text, out var d))
            return d;

        return text;
    }
}

public sealed partial class ConfigTreeViewModel : ViewModelBase
{
    public ConfigTreeViewModel(IConfigApi config)
    {
        Profile = config.CurrentProfile;
        RootNodes = config.ConfigRoot is IConfigSettingParent parent
            ? parent.Children.Values
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new ConfigTreeNodeViewModel(c, c.Key))
                .ToList()
            : [];
    }

    public string Profile { get; }
    public IReadOnlyList<ConfigTreeNodeViewModel> RootNodes { get; }

    [ObservableProperty]
    private ConfigTreeNodeViewModel? _selectedNode;
}
