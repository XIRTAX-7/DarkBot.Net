# DarkBot.Net — статус Kekka-parity (v1)

Обновлено: 2026-06-15

**Единственный поддерживаемый путь игры:** Darkorbit-client (Electron) → Frida `avm_move.py` → DarkMem attach.  
**Отключено:** KekkaPlayer.dll, Java JVM launcher, ручной выбор PID.

---

## Как протестировать v1

### Подготовка (один раз)

1. **Darkorbit-client**
   ```bash
   cd Darkorbit-client
   npm install
   ```

2. **DarkBot.Net** — `./lib/` с нативными DLL (`DarkBotBridge.dll`, `DarkMemAPI.dll`) и `DarkBot.jar` (для JNI DarkMem).

3. **Python** (для Frida в клиенте): `pip install frida psutil`

4. **appsettings.json** (уже по умолчанию):
   ```json
   "BrowserApi": "FridaClient",
   "FridaApiPort": 44570,
   "DarkorbitClientPath": ""
   ```
   Путь к клиенту ищется автоматически (`../Darkorbit-client` от exe) или задайте `DarkorbitClientPath` / `DARKORBIT_CLIENT_PATH`.

### Запуск

1. Запустить **DarkBot.Net.Ui**
2. Логин (логин/пароль или SID)
3. Бот сам:
   - стартует **Darkorbit-client** с `dosid`
   - патчит settings (NoSandbox + Movement API)
   - ждёт **Pepper Flash** PID
   - attach DarkMem + опрашивает `http://127.0.0.1:44570/status`
4. В окне клиента дождаться **загрузки карты** (internalMapRevolution)
5. В UI: «Игра подключена (Pepper pid …)» → бот-луп может работать

### Проверка вручную

```bash
curl http://127.0.0.1:44570/status
# "ready": true, screenManager, mainAddress, ...
```

---

## Слои

| Слой | Компонент | Статус v1 |
|------|-----------|-----------|
| Client | Darkorbit-client Electron | ✅ автозапуск из .NET |
| Bridge | darkDev Frida + HTTP :44570 | ✅ Movement/Select/Collect/… |
| Bot | DarkBot.Net managers + loop | 🟡 базово (движение, installer) |

---

## Сделано

### Phase A — Frida Game API
- [x] Общая очередь invoke на `enterFrame`
- [x] `POST /move`, `/collect`, `/select`, `/useItem`, `/refine`, `/invoke`
- [x] `/status` — main/screen/hero/connectionManager, `lastPacketActivityMs`

### Phase B — пакеты / invalid (частично)
- [x] `lastPacketActivityMs` в агенте
- [x] Packet WS → .NET `GamePacketReader` + `GamePacketBridgeHostedService`
- [x] Refresh через Electron control WS `reload` (`ElectronControlClient`)

### Phase C — client hardening (частично)
- [x] Movement + NoSandbox в `defaultSettings.json` и патч при запуске
- [x] Автозапуск `avm_move.py` из `inject/main.js` (при Movement=ON)
- [ ] Документация launcher в README
- [ ] WS `setMaxFps`, proxy

### Phase D — DarkBot.Net wiring
- [x] `FridaClient` — единственный режим игры
- [x] `FridaGameApi` → HTTP darkDev
- [x] `BotInstaller` из `/status`
- [x] `DarkorbitClientLauncher` + `GameClientConnectService` (авто attach)
- [x] Kekka/Java **убраны из DI** (файлы в репо остаются, не используются)
- [x] `GameDirectApi` — select/collect/useItem/refine

### Phase E — модули (заготовки)
- [x] `EntityManager` — только размер списка с карты
- [x] `JumpPortal` — движение к порталу
- [ ] Полный EntityManager (NPC/боксы/порталы)
- [ ] JumpPortal с реальным прыжком
- [ ] Модули Loot/Map/Gate

---

## Не сделано / блокеры

| Задача | Приоритет |
|--------|-----------|
| Полный EntityManager + MapCanvas сущности | Высокий |
| Бой (select + attacker loop в модулях) | Высокий |
| Сбор лута в DefaultPlugin/Loot | Высокий |
| `useItem` / расходники в модулях | Средний |
| Refresh при зависании (координация с клиентом) | Средний |
| Proxy / blocking patterns | Низкий |
| CI smoke-тест Frida agent | Низкий |

---

## Архитектура v1

```
DarkBot.Net UI
    → Login / Backpage
    → DarkorbitClientLauncher (--dosid)
    → GameClientConnectService (Pepper PID + /status)
    → FridaGameApi (HTTP :44570)
    → DarkMem (read memory)
    → BotInstaller / HeroManager / MapManager / MovementApi
```

---

## Устаревший код (не удалён, не подключён)

- `KekkaPlayerGameApi`, `KekkaPlayerProcessLauncher`, `KekkaPlayerLaunchMonitor`
- Java bridge launchers в `DarkBot.Net.Agent.Bridge/java/`
- `GameApiMode.KekkaPlayer`, `DarkMemAttach` — помечены `[Obsolete]`

Удаление файлов — отдельная задача после стабилизации v1.

---

## Связанные файлы

| Файл | Назначение |
|------|------------|
| `.cursor/plans/bot_kekka_parity_plan_078a1567.plan.md` | Исходный план |
| `Darkorbit-client/darkDev/avm_move_agent.js` | Frida agent |
| `Darkorbit-client/darkDev/avm_move.py` | HTTP API |
| `DarkBot.Net/.../FridaGameApi.cs` | .NET ↔ Frida |
| `DarkBot.Net/.../DarkorbitClientLauncher.cs` | Запуск Electron |
| `DarkBot.Net/.../GameClientConnectService.cs` | Авто-подключение |
