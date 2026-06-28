# DarkBot.Net — Agent instructions

Правила разработки: [`.cursor/rules/Cursor-AI-NET-10-C-14.mdc`](../.cursor/rules/Cursor-AI-NET-10-C-14.mdc)  
Unity client: [`.cursor/rules/DarkBot-Net-Darkorbit-Client.mdc`](../.cursor/rules/DarkBot-Net-Darkorbit-Client.mdc)  
Оркестрация: [`.cursor/rules/DarkBot-Orchestration.mdc`](../.cursor/rules/DarkBot-Orchestration.mdc)  
Wiki policy: [`.cursor/rules/DarkBot-Wiki-Context.mdc`](../.cursor/rules/DarkBot-Wiki-Context.mdc)

## Wiki

Проектная wiki — `.context/wiki/` (создаётся subagent **`context-keeper`** по запросу, не заранее).

Для architecture context: `.context/wiki/_index.md` → 1–3 релевантные страницы.

## Subagents (`.cursor/agents/`)

| Subagent | Когда |
|----------|-------|
| `dotnet-developer` | C# features, modules, managers, WPF |
| `frida-bridge-developer` | Frida agent, RPC, IL2CPP hooks |
| `dotnet-test-writer` | xUnit tests |
| `dotnet-code-reviewer` | Read-only C# review |
| `solution-architect` | Architecture, migration scope |
| `context-keeper` | Обновление `.context/wiki` после checks |

## Навигация по коду

| Задача | Где искать |
|--------|------------|
| Bot loop / tick | `Application/BotEngine/Loop/`, `Runtime/` |
| Bot modules | `Application/BotEngine/Modules/` (Java ref: `DarkBotAPI/shared/modules/`) |
| Managers | `Application/BotEngine/Managers/` |
| Frida bridge C# | `Infrastructure/Game/Bridge/` |
| Frida agent TS | `darkorbit-unity-bridge/agent/src/` |
| WPF UI | `Presentation/Views/`, `ViewModels/`, `Controls/` |
| DI | `Presentation/Extensions/`, `Application/Extensions/` |
| Java reference | `DarkBot/`, `DarkBotAPI/` (repo root) |

## Проверки

```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
```

## Frida skills

Bridge-работа: skills в `.agents/skills/frida-*/` (workflow, agent-builder, troubleshooting).
