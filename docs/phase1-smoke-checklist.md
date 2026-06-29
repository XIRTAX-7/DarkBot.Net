# Фаза 1 — Manual smoke checklist

Проверка bridge actions в реальной игре (`DarkOrbit.exe` v1.1.103). Один прогон ~15 мин.

## Предусловия

- [ ] Игра установлена: `C:\DarkOrbit_Version1.1.103`
- [ ] `appsettings.json`: `BrowserApi: UnityClient`, `UnityGameInstallPath` корректен
- [ ] Agent собран: `darkorbit-unity-bridge/agent/dist/agent.js` актуален
- [ ] Пользователь **сам** запустил игру и вошёл на карту (бот не стартует клиент)

## Запуск бота

1. Запустить DarkBot.Net WPF
2. Login → attach к `DarkOrbit.exe`
3. Дождаться `Connected` / карта на dashboard
4. Запустить бот (Start)

## Smoke-сценарии

### 1. moveTo

- [ ] Клик по карте в WPF → корабль движется к точке
- [ ] В логах: `Unity MoveToAsync target=(x,y)` + `moveTo RPC response`
- [ ] `heroPos` в status обновляется после движения

### 2. selectEntity

- [ ] На карте 1-1 есть NPC (Streuner)
- [ ] Через dev/отладку или будущий UI: `SelectEntityAsync(npcId, x, y)` → цель выбрана в игре
- [ ] Dashboard Target section показывает NPC (не портит hero HP cache)
- [ ] Лог: `Unity selectEntity RPC response ... ok=True`

### 3. attackLaser

- [ ] При выбранном NPC: `AttackAsync()` → лазер стреляет
- [ ] Лог: `Unity attackLaser RPC response ... ok=True`

### 4. collectTo

- [ ] На карте есть box (bonus box / ore)
- [ ] Подлететь в радиус ~250, вызвать `CollectBoxAsync(boxId, x, y)`
- [ ] Box собран, cargo/credits обновились в stats
- [ ] Лог: `Unity collectTo RPC response ... ok=True`
- [ ] При отсутствии Unit в cache: `entity_pointer_not_found` (честный fail, не crash)

## Threading (code review)

| Путь | Ожидание |
|------|----------|
| UI map click | `MoveToAsync` + `await` (MainWindowViewModel) |
| Bot loop 10 Hz | sync wrapper `.GetAwaiter().GetResult()` только на background thread |
| `RefreshStatus` | bot loop / installer, не UI |

## Не в scope Ф1 (ожидаемые stub)

- [ ] `UseItemAsync` → `false` + warning (cloak — Phase 2 Collector)
- [ ] `SetRoamMode` / `SetRunMode` → `false` (ship-mode RPC позже)
- [ ] Legacy `IGameConnection.SelectEntity` → warning, не crash

## Результат

| Дата | Карта | moveTo | select | attack | collect | Примечания |
|------|-------|--------|--------|--------|---------|------------|
| | | | | | | |

**Gate Ф1 закрыт**, когда все P0-сценарии (move/select/attack/collect) пройдены хотя бы раз на одной сессии.
