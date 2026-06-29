using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>Строит in-memory дерево IConfigSetting из BotProfileDocument (lowercase dot paths).</summary>
public static class ConfigTreeBuilder
{
    public static ConfigSettingNode Build(BotProfileDocument document)
    {
        var root = new ConfigSettingNode("config", "Configuration");

        var general = root.AddChild(new ConfigSettingNode("general", "General"));
        general.AddLeaf("current_module", "Current module", document.General.CurrentModule);
        general.AddLeaf("working_map", "Working map", document.General.WorkingMap);
        general.AddLeaf("safety_wait", "Safety wait (ms)", document.General.SafetyWait);

        var collect = root.AddChild(new ConfigSettingNode("collect", "Collect"));
        collect.AddLeaf("radius", "Collect radius", document.Collect.Radius);
        collect.AddLeaf("stay_away_from_enemies", "Stay away from enemies", document.Collect.StayAwayFromEnemies);
        collect.AddLeaf("auto_cloak", "Auto cloak", document.Collect.AutoCloak);
        collect.AddLeaf("ignore_contested_boxes", "Ignore contested boxes", document.Collect.IgnoreContestedBoxes);

        var boxInfos = collect.AddChild(new ConfigSettingNode("box_infos", "Box infos"));
        foreach (var (name, info) in document.Collect.BoxInfos)
        {
            var boxNode = boxInfos.AddChild(new ConfigSettingNode(name, name));
            boxNode.AddLeaf("should_collect", "Should collect", info.ShouldCollect);
            boxNode.AddLeaf("priority", "Priority", info.Priority);
            boxNode.AddLeaf("wait_time", "Wait time (ms)", info.WaitTime);
        }

        var meta = root.AddChild(new ConfigSettingNode("meta", "Meta"));
        meta.AddLeaf("display_name", "Display name", document.Meta.DisplayName);
        meta.AddLeaf("owner", "Owner", document.Meta.Owner.ToString().ToLowerInvariant());
        meta.AddLeaf("schema_version", "Schema version", document.Meta.SchemaVersion);

        return root;
    }
}
