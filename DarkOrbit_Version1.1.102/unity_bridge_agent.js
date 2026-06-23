'use strict';

/**
 * Unity IL2CPP game bridge agent (Dark Orbit v1.1.102).
 * Frida 17+ — RVAs from Il2CppDumper script.json.
 */
const SCHEMA_VERSION = 1;
const AGENT_VERSION = 'unity-bridge-2026-06-23-6';

const RVA = {
    heroMoveHandlerHandle: 0xF5FAE0,
    moveCommandHandlerHandle: 0xF633D0,
    opera2DHeroMove: 0x35CB80,
    opera2DMouseDown: 0x35B8D0,
    opera2DContinuousMove: 0x35C4E0,
    minimapClickDown: 0x4F21E0,
    minimapClickUp: 0x4F22C0,
    minimapClick: 0x4F23D0,
    mapInfoComponent: 0x2A5320,
    moveHeroToCoordinates: 0x2A9ED0,
    queueMoveRequest: 0x2AA590,
    queueMoveRequestSend: 0x2B7A40,
    sessionSend: 0xECB0D0,
    moveRequestCtor: 0x154ED20,
    opera2DGetMyUnit: 0xECA530,
};

const MOVE_REQUEST_POSITION_X_OFFSET = 0x8;
const MOVE_REQUEST_TARGET_Y_OFFSET = 0xC;
const MOVE_REQUEST_TARGET_X_OFFSET = 0x10;
const MOVE_REQUEST_POSITION_Y_OFFSET = 0x14;

const UNIT_POSITION_WRAP_OFFSET = 0x60;
const WRAP_VECTOR3_VALUE_OFFSET = 0x8;

const MAP_STD_WIDTH = 21000;
const MAP_STD_HEIGHT = 13100;
const MAP_CENTER_X = MAP_STD_WIDTH / 2;
const MAP_CENTER_Y = MAP_STD_HEIGHT / 2;

const ENTITY_DOMAIN_OFFSET = 0x1C;
const ENTITY_PARENT_OFFSET = 0x20;

const HERO_MOVE_CMD_X_OFFSET = 0x8;
const HERO_MOVE_CMD_Y_OFFSET = 0xC;

const MOVE_CMD_USER_OFFSET = 0x8;
const MOVE_CMD_X_OFFSET = 0xC;
const MOVE_CMD_Y_OFFSET = 0x10;
const MOVE_CMD_TTL_OFFSET = 0x14;

const PING_INTERVAL_MS = 5000;

// Vector3 в IL2CPP x64 — by-value; координаты клика по карте не читаем из args.
const ENABLE_MAP_CLICK_COORD_PROBE = false;

const clickCompareStats = {};

const hooks = [];
let pingTimer = null;
let startedAt = Date.now();
let gameAssemblyBase = null;

const moveCache = {
    scene: null,
    session: null,
    moveHeroMethodInfo: null,
    queueMoveMethodInfo: null,
    mapInfoMethodInfo: null,
    operaMyUnitMethodInfo: null,
    sessionSendMethodInfo: null,
};

const netStats = {
    queueMoveRequestIn: 0,
    moveHeroToCoordinatesIn: 0,
    moveRequestCtor: 0,
    sessionSend: 0,
    moveRequestSend: 0,
    lastQueueMove: null,
    lastMoveRequestCtor: null,
    lastSessionSendMove: null,
};

let queueMoveRequestFn = null;
let moveHeroToCoordinatesFn = null;
let mapInfoComponentFn = null;
let getMyUnitFn = null;

function resolveGameAssembly() {
    const mod = Process.findModuleByName('GameAssembly.dll');
    if (!mod) {
        throw new Error('GameAssembly.dll not loaded — open the game map first');
    }
    return mod;
}

function readS32(ptr, offset) {
    return ptr.add(offset).readS32();
}

function readVector3(ptr) {
    return {
        x: ptr.readFloat(),
        y: ptr.add(4).readFloat(),
        z: ptr.add(8).readFloat(),
    };
}

function isPlausibleIntCoord(x, y) {
    return Number.isFinite(x) && Number.isFinite(y)
        && Math.abs(x) <= 200000 && Math.abs(y) <= 200000;
}

function isPlausibleClickPos(v) {
    return Number.isFinite(v.x) && Number.isFinite(v.y) && Number.isFinite(v.z)
        && Math.abs(v.x) <= 200000 && Math.abs(v.y) <= 200000
        && Math.abs(v.z) <= 200000
        && !(v.x === 0 && v.y === 0 && v.z === 0);
}

function emit(payload) {
    send(payload);
}

function trackInterceptor(interceptor) {
    hooks.push(interceptor);
    return interceptor;
}

function installHeroMoveHook(base) {
    const target = base.add(RVA.heroMoveHandlerHandle);
    trackInterceptor(Interceptor.attach(target, {
        onEnter(args) {
            try {
                const message = args[1];
                if (message.isNull()) {
                    return;
                }
                const x = readS32(message, HERO_MOVE_CMD_X_OFFSET);
                const y = readS32(message, HERO_MOVE_CMD_Y_OFFSET);
                if (!isPlausibleIntCoord(x, y)) {
                    return;
                }
                emit({
                    type: 'hero_pos',
                    schemaVersion: SCHEMA_VERSION,
                    x: x,
                    y: y,
                    ts: Date.now(),
                });
            } catch (e) {
                emit({ type: 'warn', message: 'hero_pos hook: ' + e });
            }
        },
    }));
    console.log('[unity_bridge] HeroMoveCommandHandler.Handle @ ' + target);
}

function installMoveCommandHook(base) {
    const target = base.add(RVA.moveCommandHandlerHandle);
    trackInterceptor(Interceptor.attach(target, {
        onEnter(args) {
            try {
                const message = args[1];
                if (message.isNull()) {
                    return;
                }
                const userId = readS32(message, MOVE_CMD_USER_OFFSET);
                const x = readS32(message, MOVE_CMD_X_OFFSET);
                const y = readS32(message, MOVE_CMD_Y_OFFSET);
                const ttl = readS32(message, MOVE_CMD_TTL_OFFSET);
                if (!isPlausibleIntCoord(x, y)) {
                    return;
                }
                emit({
                    type: 'unit_move',
                    schemaVersion: SCHEMA_VERSION,
                    userId: userId,
                    x: x,
                    y: y,
                    ttl: ttl,
                    ts: Date.now(),
                });
            } catch (e) {
                emit({ type: 'warn', message: 'unit_move hook: ' + e });
            }
        },
    }));
    console.log('[unity_bridge] MoveCommandHandler.Handle @ ' + target);
}

function isValidPointer(ptrValue) {
    return !!(ptrValue && !ptrValue.isNull());
}

// QueueMoveRequest / MoveHeroToCoordinates принимают Y с инверсией знака относительно серверных координат.
// Ручной клик: queue targetY=-2612 → MoveRequest targetY=+2612 (см. лог move_request_ctor).
function toClientMoveY(serverY) {
    return -Math.round(serverY);
}

function toServerMoveY(clientY) {
    return -Math.round(clientY);
}

function readEntityDomain(entityPtr) {
    if (!isValidPointer(entityPtr)) {
        return null;
    }
    const domain = entityPtr.add(ENTITY_DOMAIN_OFFSET).readPointer();
    if (isValidPointer(domain)) {
        return domain;
    }
    const parent = entityPtr.add(ENTITY_PARENT_OFFSET).readPointer();
    return isValidPointer(parent) ? parent : null;
}

function snapshotNetStats() {
    return {
        queueMoveRequestIn: netStats.queueMoveRequestIn,
        moveHeroToCoordinatesIn: netStats.moveHeroToCoordinatesIn,
        moveRequestCtor: netStats.moveRequestCtor,
        sessionSend: netStats.sessionSend,
        moveRequestSend: netStats.moveRequestSend,
        lastQueueMove: netStats.lastQueueMove,
        lastMoveRequestCtor: netStats.lastMoveRequestCtor,
        lastSessionSendMove: netStats.lastSessionSendMove,
    };
}

function readMoveRequestFields(messagePtr) {
    return {
        positionX: readS32(messagePtr, MOVE_REQUEST_POSITION_X_OFFSET),
        targetY: readS32(messagePtr, MOVE_REQUEST_TARGET_Y_OFFSET),
        targetX: readS32(messagePtr, MOVE_REQUEST_TARGET_X_OFFSET),
        positionY: readS32(messagePtr, MOVE_REQUEST_POSITION_Y_OFFSET),
    };
}

function looksLikeMoveRequest(messagePtr) {
    try {
        const fields = readMoveRequestFields(messagePtr);
        return isPlausibleIntCoord(fields.targetX, fields.targetY)
            && isPlausibleIntCoord(fields.positionX, fields.positionY);
    } catch (e) {
        return false;
    }
}

function installMoveProbeHooks(base) {
    trackInterceptor(Interceptor.attach(base.add(RVA.queueMoveRequest), {
        onEnter(args) {
            netStats.queueMoveRequestIn++;
            const targetX = args[1].toInt32();
            const targetY = args[2].toInt32();
            const click = args[3].toInt32();
            netStats.lastQueueMove = {
                targetX: targetX,
                targetY: targetY,
                click: click !== 0,
            };
            if (isValidPointer(args[0])) {
                moveCache.scene = args[0];
            }
            if (isValidPointer(args[4])) {
                moveCache.queueMoveMethodInfo = args[4];
            }
            emit({
                type: 'queue_move_request',
                schemaVersion: SCHEMA_VERSION,
                targetX: targetX,
                targetY: targetY,
                click: click !== 0,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] MoveHelper.QueueMoveRequest probe @ ' + base.add(RVA.queueMoveRequest));

    trackInterceptor(Interceptor.attach(base.add(RVA.moveHeroToCoordinates), {
        onEnter(args) {
            netStats.moveHeroToCoordinatesIn++;
            if (isValidPointer(args[0])) {
                moveCache.scene = args[0];
            }
            if (isValidPointer(args[6])) {
                moveCache.moveHeroMethodInfo = args[6];
            }
            emit({
                type: 'move_hero_to_coordinates',
                schemaVersion: SCHEMA_VERSION,
                targetX: args[1].toInt32(),
                targetY: args[2].toInt32(),
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] MoveHelper.MoveHeroToCoordinates probe @ ' + base.add(RVA.moveHeroToCoordinates));

    trackInterceptor(Interceptor.attach(base.add(RVA.mapInfoComponent), {
        onEnter(args) {
            if (isValidPointer(args[0])) {
                moveCache.mapInfoMethodInfo = args[0];
            }
        },
    }));
    console.log('[unity_bridge] ComponentHelper.MapInfoComponent probe @ ' + base.add(RVA.mapInfoComponent));

    trackInterceptor(Interceptor.attach(base.add(RVA.opera2DGetMyUnit), {
        onEnter(args) {
            if (isValidPointer(args[0])) {
                moveCache.operaMyUnitMethodInfo = args[0];
            }
        },
    }));
    console.log('[unity_bridge] Opera2DComponent.get_MyUnit probe @ ' + base.add(RVA.opera2DGetMyUnit));
}

function installNetworkProbeHooks(base) {
    trackInterceptor(Interceptor.attach(base.add(RVA.moveRequestCtor), {
        onEnter(args) {
            netStats.moveRequestCtor++;
            const payload = {
                positionX: args[1].toInt32(),
                targetY: args[2].toInt32(),
                targetX: args[3].toInt32(),
                positionY: args[4].toInt32(),
            };
            netStats.lastMoveRequestCtor = payload;
            emit({
                type: 'move_request_ctor',
                schemaVersion: SCHEMA_VERSION,
                source: 'client_outgoing',
                positionX: payload.positionX,
                targetY: payload.targetY,
                targetX: payload.targetX,
                positionY: payload.positionY,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] MoveRequest.ctor probe @ ' + base.add(RVA.moveRequestCtor));

    trackInterceptor(Interceptor.attach(base.add(RVA.sessionSend), {
        onEnter(args) {
            netStats.sessionSend++;
            if (isValidPointer(args[0])) {
                moveCache.session = args[0];
            }
            if (isValidPointer(args[2])) {
                moveCache.sessionSendMethodInfo = args[2];
            }
            const message = args[1];
            if (!isValidPointer(message) || !looksLikeMoveRequest(message)) {
                return;
            }
            netStats.moveRequestSend++;
            const fields = readMoveRequestFields(message);
            netStats.lastSessionSendMove = fields;
            emit({
                type: 'session_send_move',
                schemaVersion: SCHEMA_VERSION,
                positionX: fields.positionX,
                targetY: fields.targetY,
                targetX: fields.targetX,
                positionY: fields.positionY,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] SessionSystem.Send probe @ ' + base.add(RVA.sessionSend));

    trackInterceptor(Interceptor.attach(base.add(RVA.queueMoveRequestSend), {
        onEnter() {
            emit({
                type: 'queue_move_request_send',
                schemaVersion: SCHEMA_VERSION,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] QueueMoveRequest send callback probe @ ' + base.add(RVA.queueMoveRequestSend));
}

function initMoveNativeFunctions(base) {
    queueMoveRequestFn = new NativeFunction(
        base.add(RVA.queueMoveRequest),
        'void',
        ['pointer', 'int', 'int', 'int', 'pointer'],
    );
    moveHeroToCoordinatesFn = new NativeFunction(
        base.add(RVA.moveHeroToCoordinates),
        'pointer',
        ['pointer', 'int', 'int', 'pointer', 'int', 'pointer', 'pointer'],
    );
    mapInfoComponentFn = new NativeFunction(
        base.add(RVA.mapInfoComponent),
        'pointer',
        ['pointer'],
    );
    getMyUnitFn = new NativeFunction(
        base.add(RVA.opera2DGetMyUnit),
        'pointer',
        ['pointer'],
    );
}

function getHeroUnitPointer() {
    if (getMyUnitFn === null) {
        return null;
    }
    const methodInfo = isValidPointer(moveCache.operaMyUnitMethodInfo)
        ? moveCache.operaMyUnitMethodInfo
        : ptr(0);
    try {
        const unit = getMyUnitFn(methodInfo);
        return isValidPointer(unit) ? unit : null;
    } catch (e) {
        emit({ type: 'warn', message: 'getHeroUnitPointer: ' + e });
        return null;
    }
}

function getHeroMapPosition() {
    const unit = getHeroUnitPointer();
    if (!isValidPointer(unit)) {
        return null;
    }
    try {
        const wrap = unit.add(UNIT_POSITION_WRAP_OFFSET).readPointer();
        if (!isValidPointer(wrap)) {
            return null;
        }
        const pos = readVector3(wrap.add(WRAP_VECTOR3_VALUE_OFFSET));
        const clientY = Math.round(pos.y);
        return {
            x: Math.round(pos.x),
            y: clientY,
            serverY: toServerMoveY(clientY),
        };
    } catch (e) {
        emit({ type: 'warn', message: 'getHeroMapPosition: ' + e });
        return null;
    }
}

function resolveScenePointer() {
    if (isValidPointer(moveCache.scene)) {
        return moveCache.scene;
    }

    const unit = getHeroUnitPointer();
    if (isValidPointer(unit)) {
        const sceneFromHero = readEntityDomain(unit);
        if (isValidPointer(sceneFromHero)) {
            moveCache.scene = sceneFromHero;
            return sceneFromHero;
        }
    }

    if (mapInfoComponentFn === null) {
        return null;
    }

    const methodInfoCandidates = [];
    if (isValidPointer(moveCache.mapInfoMethodInfo)) {
        methodInfoCandidates.push(moveCache.mapInfoMethodInfo);
    }
    methodInfoCandidates.push(ptr(0));

    for (let i = 0; i < methodInfoCandidates.length; i++) {
        try {
            const mapInfo = mapInfoComponentFn(methodInfoCandidates[i]);
            const scene = readEntityDomain(mapInfo);
            if (isValidPointer(scene)) {
                moveCache.scene = scene;
                return scene;
            }
        } catch (e) {
            if (i === 0) {
                emit({ type: 'warn', message: 'resolveScenePointer MapInfoComponent: ' + e });
            }
        }
    }

    return null;
}

function invokeMoveHeroToCoordinates(scene, serverX, clientY) {
    if (moveHeroToCoordinatesFn === null) {
        return { ok: false, error: 'move_hero_native_unavailable' };
    }
    const methodInfo = isValidPointer(moveCache.moveHeroMethodInfo)
        ? moveCache.moveHeroMethodInfo
        : ptr(0);
    try {
        const result = moveHeroToCoordinatesFn(
            scene,
            serverX,
            clientY,
            ptr(0),
            1,
            ptr(0),
            methodInfo,
        );
        return { ok: true, result: result.toString() };
    } catch (e) {
        emit({ type: 'warn', message: 'MoveHeroToCoordinates: ' + e });
        return { ok: false, error: String(e) };
    }
}

function invokeMoveToCoordinates(targetX, targetY) {
    if (gameAssemblyBase === null) {
        throw new Error('GameAssembly base is not initialized');
    }
    if (queueMoveRequestFn === null) {
        throw new Error('QueueMoveRequest native function is not initialized');
    }

    const scene = resolveScenePointer();
    if (!isValidPointer(scene)) {
        return {
            ok: false,
            error: 'scene_unavailable',
            hint: 'Hero unit not loaded — open the map first',
        };
    }

    const serverX = Math.round(targetX);
    const serverY = Math.round(targetY);
    const clientY = toClientMoveY(serverY);
    const heroPos = getHeroMapPosition();
    const statsBefore = snapshotNetStats();
    const queueMoveMethodInfo = isValidPointer(moveCache.queueMoveMethodInfo)
        ? moveCache.queueMoveMethodInfo
        : ptr(0);

    // Как при клике: MoveHeroToCoordinates (клиент) + QueueMoveRequest (сервер).
    const clientMove = invokeMoveHeroToCoordinates(scene, serverX, clientY);
    queueMoveRequestFn(scene, serverX, clientY, 1, queueMoveMethodInfo);

    const statsAfter = snapshotNetStats();
    const serverPacketSent = statsAfter.moveRequestCtor > statsBefore.moveRequestCtor
        || statsAfter.moveRequestSend > statsBefore.moveRequestSend;
    const clientMoveStarted = statsAfter.moveHeroToCoordinatesIn > statsBefore.moveHeroToCoordinatesIn
        || clientMove.ok;
    const lastMoveRequest = statsAfter.lastMoveRequestCtor;
    const moveRequestYValid = lastMoveRequest !== null
        && lastMoveRequest.targetY === serverY;

    const result = {
        ok: true,
        api: 'MoveHeroToCoordinates+QueueMoveRequest',
        x: serverX,
        y: serverY,
        clientY: clientY,
        heroPos: heroPos,
        scene: scene.toString(),
        clientMoveStarted: clientMoveStarted,
        clientMove: clientMove,
        serverPacketSent: serverPacketSent,
        moveRequestYValid: moveRequestYValid,
        lastMoveRequest: lastMoveRequest,
        netStatsBefore: statsBefore,
        netStatsAfter: statsAfter,
    };

    emit({
        type: 'move_sent',
        schemaVersion: SCHEMA_VERSION,
        api: result.api,
        x: serverX,
        y: serverY,
        clientY: clientY,
        heroPos: heroPos,
        scene: scene.toString(),
        clientMoveStarted: clientMoveStarted,
        serverPacketSent: serverPacketSent,
        moveRequestYValid: moveRequestYValid,
        ts: Date.now(),
    });

    return result;
}

function emitClickInput(source) {
    clickCompareStats[source] = (clickCompareStats[source] || 0) + 1;
    emit({
        type: 'click_input',
        schemaVersion: SCHEMA_VERSION,
        source: source,
        heroPos: getHeroMapPosition(),
        ts: Date.now(),
    });
}

function installClickCompareHooks(base) {
    trackInterceptor(Interceptor.attach(base.add(RVA.opera2DMouseDown), {
        onEnter() {
            emitClickInput('main_map_mouse_down');
        },
    }));
    console.log('[unity_bridge] Opera2D.GetMouseLeftButtonDown @ ' + base.add(RVA.opera2DMouseDown));

    trackInterceptor(Interceptor.attach(base.add(RVA.opera2DHeroMove), {
        onEnter() {
            emitClickInput('main_map_hero_move');
        },
    }));
    console.log('[unity_bridge] Opera2D.HeroMove (click compare) @ ' + base.add(RVA.opera2DHeroMove));

    trackInterceptor(Interceptor.attach(base.add(RVA.opera2DContinuousMove), {
        onEnter() {
            emitClickInput('main_map_continuous');
        },
    }));
    console.log('[unity_bridge] Opera2D.HeroContinuousMovement @ ' + base.add(RVA.opera2DContinuousMove));

    trackInterceptor(Interceptor.attach(base.add(RVA.minimapClickDown), {
        onEnter() {
            emitClickInput('minimap_click_down');
        },
    }));
    console.log('[unity_bridge] UIMiniMap.MapClickDown @ ' + base.add(RVA.minimapClickDown));

    trackInterceptor(Interceptor.attach(base.add(RVA.minimapClickUp), {
        onEnter() {
            emitClickInput('minimap_click_up');
        },
    }));
    console.log('[unity_bridge] UIMiniMap.MapClickUp @ ' + base.add(RVA.minimapClickUp));

    trackInterceptor(Interceptor.attach(base.add(RVA.minimapClick), {
        onEnter() {
            emitClickInput('minimap_click');
        },
    }));
    console.log('[unity_bridge] UIMiniMap.MapClick @ ' + base.add(RVA.minimapClick));
}

function installOperaClickHook(base) {
    if (!ENABLE_MAP_CLICK_COORD_PROBE) {
        return;
    }
    const target = base.add(RVA.opera2DHeroMove);
    trackInterceptor(Interceptor.attach(target, {
        onEnter(args) {
            try {
                const clickArg = args[1];
                if (clickArg.isNull()) {
                    return;
                }
                const clickPos = readVector3(clickArg);
                if (!isPlausibleClickPos(clickPos)) {
                    return;
                }
                emit({
                    type: 'map_click',
                    schemaVersion: SCHEMA_VERSION,
                    x: clickPos.x,
                    y: clickPos.y,
                    z: clickPos.z,
                    ts: Date.now(),
                });
            } catch (e) {
                emit({ type: 'warn', message: 'map_click hook: ' + e });
            }
        },
    }));
    console.log('[unity_bridge] Opera2DComponentSystem.HeroMove @ ' + target);
}

function startPing() {
    if (pingTimer !== null) {
        return;
    }
    pingTimer = setInterval(function () {
        emit({
            type: 'ping',
            schemaVersion: SCHEMA_VERSION,
            agentVersion: AGENT_VERSION,
            uptimeMs: Date.now() - startedAt,
            ts: Date.now(),
        });
    }, PING_INTERVAL_MS);
}

function stopAgent(reason) {
    if (pingTimer !== null) {
        clearInterval(pingTimer);
        pingTimer = null;
    }
    while (hooks.length > 0) {
        try {
            hooks.pop().detach();
        } catch (e) {
            // already detached
        }
    }
    emit({
        type: 'detach',
        schemaVersion: SCHEMA_VERSION,
        reason: reason || 'stop',
        ts: Date.now(),
    });
}

function main() {
    const ga = resolveGameAssembly();
    gameAssemblyBase = ga.base;
    initMoveNativeFunctions(ga.base);
    installMoveProbeHooks(ga.base);
    installNetworkProbeHooks(ga.base);

    emit({
        type: 'ready',
        schemaVersion: SCHEMA_VERSION,
        agentVersion: AGENT_VERSION,
        pid: Process.id,
        gameAssemblyBase: ga.base.toString(),
        ts: Date.now(),
    });

    installHeroMoveHook(ga.base);
    installMoveCommandHook(ga.base);
    installClickCompareHooks(ga.base);
    if (ENABLE_MAP_CLICK_COORD_PROBE) {
        installOperaClickHook(ga.base);
    } else {
        console.log('[unity_bridge] map_click coord probe disabled (Vector3 ABI)');
    }
    startPing();

    console.log('[unity_bridge] agent ready pid=' + Process.id);
}

rpc.exports = {
    getStatus: function () {
        return JSON.stringify({
            schemaVersion: SCHEMA_VERSION,
            agentVersion: AGENT_VERSION,
            ready: true,
            pid: Process.id,
            uptimeMs: Date.now() - startedAt,
            hookCount: hooks.length,
            moveCache: {
                hasScene: isValidPointer(moveCache.scene),
                hasSession: isValidPointer(moveCache.session),
                hasMoveHeroMethodInfo: isValidPointer(moveCache.moveHeroMethodInfo),
                hasQueueMoveMethodInfo: isValidPointer(moveCache.queueMoveMethodInfo),
                hasMapInfoMethodInfo: isValidPointer(moveCache.mapInfoMethodInfo),
                hasOperaMyUnitMethodInfo: isValidPointer(moveCache.operaMyUnitMethodInfo),
            },
            heroPos: getHeroMapPosition(),
            netStats: snapshotNetStats(),
            clickCompareStats: clickCompareStats,
            mapCenter: { x: MAP_CENTER_X, y: MAP_CENTER_Y },
        });
    },
    getClickCompareStats: function () {
        return JSON.stringify(clickCompareStats);
    },
    getNetStats: function () {
        return JSON.stringify(snapshotNetStats());
    },
    moveTo: function (x, y) {
        return JSON.stringify(invokeMoveToCoordinates(x, y));
    },
    moveToCenter: function () {
        return JSON.stringify(invokeMoveToCoordinates(MAP_CENTER_X, MAP_CENTER_Y));
    },
    stop: function () {
        stopAgent('rpc_stop');
        return true;
    },
};

setImmediate(main);
