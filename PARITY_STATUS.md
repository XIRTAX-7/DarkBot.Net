# DarkBot.Net — статус Frida-only (v1)

Обновлено: 2026-06-21

**Единственный поддерживаемый путь игры:** Darkorbit-client (Electron) → Frida `avm_bridge` → .NET через WS/HTTP.  
**Отключено полностью:** DarkMem, JNI (`DarkBotBridge.dll`), Java JVM, KekkaPlayer, внешние `ReadProcessMemory`.

Архив native-кода: `archive/darkmem-native/` (локально, в git не входит).

---

## Архитектура

```
Darkorbit-client (Electron PPAPI)
    → game_bridge_agent.js (Frida agent in Pepper Flash)
    → game_bridge.py + avm_bridge (WS :44570/ws + HTTP :44570)
    → FridaBridgeHostedService + FridaGameApi
    → MapManager / HeroManager / EntityManager / StatsManager (C#)
```

C# **не читает память процесса** — только typed snapshot из Frida.

---

## Слои

| Слой | Компонент | Статус |
|------|-----------|--------|
| Client | Darkorbit-client Electron | ✅ |
| Bridge | avm_bridge + agent.js schema v2 | ✅ entities, stats, map, hero |
| Bot | C# managers from Frida probe | 🟡 EntityManager базовый |
| DarkMem | — | ❌ удалён из runtime |

---

## Проверка

```bash
curl http://127.0.0.1:44570/status
# ready, mapId, heroHp, entities[], credits, schemaVersion: 2
```

Перезагрузите страницу клиента после обновления `game_bridge_agent.js`.

---

## Сделано (2026-06-21)

- [x] DarkMem/JNI/Kekka — архив + удаление из репо
- [x] Frida-only `FridaGameApi` (без `OpenProcess` / `ReadInt`)
- [x] `BotInstaller` только из `/status`
- [x] `StatsManager` из Frida snapshot
- [x] `EntityManager` из `entities[]` в snapshot
- [x] agent.js: heroHp fix (atom pointers), entities, stats, schemaVersion 2
- [x] CI без `build-bridge.ps1`
- [x] Packet bridge `:44569` и `packet_dumper` удалены из prod-пути

---

## Каналы клиента

| Порт | Назначение | Статус |
|------|------------|--------|
| `:44570` | Game API (HTTP + WS status/events) | ✅ |
| `:44568` | Control (reload, pid, window) | ✅ |

---

## Дальше

- [ ] Полная классификация entity (NPC/box/portal) + UI MapCanvas
- [ ] Модули Loot / Map / Gate
- [ ] Бой (select + attacker)
