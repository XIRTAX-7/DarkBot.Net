'use strict';

/**
 * Минимальный Frida-агент: вход на карту из открытого ангара.
 * Запуск: python unity_enter_map.py [--pid N]
 */
const SCRIPT_VERSION = 'enter-map-2026-06-24-6';
const LAUNCH_SHOW_BTN_START_OFFSET = 0x44;
const LAUNCH_SHOW_ITEM_SCRIPT_OFFSET = 0xA4;
const SIMPLE_ITEM_SHIP_MBUTTON_OFFSET = 0x28;
const GLM_LAUNCH_SHOW_OFFSET = 0x1C;
const MIN_IL2CPP_OBJECT_ADDRESS = 0x10000;
const RETRY_MS = 5000;
const MAX_ATTEMPTS = 2;
const MIN_DELAY_AFTER_DISCOVER_MS = 1500;

const IL2CPP_ASSEMBLY_CANDIDATES = [
    'Assembly-CSharp',
    'Assembly-CSharp.dll',
    'UnityEngine.UI',
    'UnityEngine.UI.dll',
];

const RVA = {
    enterGame: 0xF11170,
    startGame: 0xF12B80,
    launchShowOnBtnClick: 0xEE5460,
    launchShowStart: 0xEE6300,
    launchShowStartBtn0: 0xEE6830,
    findObjectsOfType: 0x27BDFB0,
};

let findObjectsOfTypeFn = null;
let launchShowOnBtnClickFn = null;
let launchShowStartBtn0Fn = null;
let launchShowOnBtnClickMethodInfo = null;
let launchShowStartBtn0MethodInfo = null;
let il2cppDomainGetFn = null;
let il2cppThreadAttachFn = null;
let il2cppThreadAttached = false;
let enterGameSeen = false;
let mapLoadStarted = false;
let attemptCount = 0;
let lastLaunchShow = null;
let lastGameLoadingManager = null;
let discoverAt = 0;
let retryTimer = null;

function emit(phase, data) {
    send(Object.assign({
        type: 'enter_map',
        phase: phase,
        ts: Date.now(),
        attempt: attemptCount,
        mapLoadStarted: mapLoadStarted,
        enterGameSeen: enterGameSeen,
    }, data || {}));
}

function readFieldPointer(obj, offset) {
    if (!obj || obj.isNull()) {
        return null;
    }
    try {
        const ptrValue = obj.add(offset).readPointer();
        return ptrValue.isNull() ? null : ptrValue;
    } catch (e) {
        return null;
    }
}

function isLikelyIl2CppObject(ptrValue) {
    if (!ptrValue || ptrValue.isNull()) {
        return false;
    }
    try {
        const addr = ptrValue.toUInt32();
        return addr >= MIN_IL2CPP_OBJECT_ADDRESS;
    } catch (e) {
        return false;
    }
}

function findModuleExport(moduleName, name) {
    try {
        if (typeof Module.findExportByName === 'function') {
            return Module.findExportByName(moduleName, name);
        }
    } catch (e) {
        // API отличается между версиями Frida.
    }

    try {
        const module = Process.findModuleByName(moduleName);
        if (module) {
            if (typeof module.findExportByName === 'function') {
                return module.findExportByName(name);
            }
            if (typeof module.getExportByName === 'function') {
                return module.getExportByName(name);
            }
        }
    } catch (e) {
        // API отличается между версиями Frida.
    }

    try {
        if (typeof Module.getExportByName === 'function') {
            return Module.getExportByName(moduleName, name);
        }
    } catch (e) {
        // API отличается между версиями Frida.
    }

    return null;
}

function resolveIl2CppExport(name) {
    return findModuleExport('GameAssembly.dll', name);
}

function ensureIl2CppThreadAttached() {
    if (il2cppThreadAttached) {
        return true;
    }

    const domainGetAddr = resolveIl2CppExport('il2cpp_domain_get');
    const threadAttachAddr = resolveIl2CppExport('il2cpp_thread_attach');
    if (!domainGetAddr || !threadAttachAddr) {
        emit('thread_attach_error', { reason: 'exports_not_found' });
        return false;
    }

    if (!il2cppDomainGetFn) {
        il2cppDomainGetFn = new NativeFunction(domainGetAddr, 'pointer', []);
    }
    if (!il2cppThreadAttachFn) {
        il2cppThreadAttachFn = new NativeFunction(threadAttachAddr, 'pointer', ['pointer']);
    }

    try {
        const domain = il2cppDomainGetFn();
        if (!domain || domain.isNull()) {
            emit('thread_attach_error', { reason: 'domain_not_found' });
            return false;
        }

        const thread = il2cppThreadAttachFn(domain);
        il2cppThreadAttached = !!(thread && !thread.isNull());
        emit(il2cppThreadAttached ? 'thread_attached' : 'thread_attach_error', {
            thread: thread && !thread.isNull() ? thread.toString() : null,
        });
        return il2cppThreadAttached;
    } catch (e) {
        emit('thread_attach_error', { reason: String(e) });
        return false;
    }
}

function findIl2CppClass(namespaceName, className) {
    const domainGetAddr = resolveIl2CppExport('il2cpp_domain_get');
    const assemblyOpenAddr = resolveIl2CppExport('il2cpp_domain_assembly_open');
    const getImageAddr = resolveIl2CppExport('il2cpp_assembly_get_image');
    const classFromNameAddr = resolveIl2CppExport('il2cpp_class_from_name');
    if (!domainGetAddr || !assemblyOpenAddr || !getImageAddr || !classFromNameAddr) {
        return null;
    }

    const domainGet = new NativeFunction(domainGetAddr, 'pointer', []);
    const assemblyOpen = new NativeFunction(assemblyOpenAddr, 'pointer', ['pointer', 'pointer']);
    const getImage = new NativeFunction(getImageAddr, 'pointer', ['pointer']);
    const classFromName = new NativeFunction(classFromNameAddr, 'pointer', ['pointer', 'pointer', 'pointer']);

    const domain = domainGet();
    if (!domain || domain.isNull()) {
        return null;
    }

    const nsPtr = Memory.allocUtf8String(namespaceName);
    const namePtr = Memory.allocUtf8String(className);
    for (let i = 0; i < IL2CPP_ASSEMBLY_CANDIDATES.length; i++) {
        const assembly = assemblyOpen(domain, Memory.allocUtf8String(IL2CPP_ASSEMBLY_CANDIDATES[i]));
        if (!assembly || assembly.isNull()) {
            continue;
        }
        const image = getImage(assembly);
        if (!image || image.isNull()) {
            continue;
        }
        const klass = classFromName(image, nsPtr, namePtr);
        if (klass && !klass.isNull()) {
            return klass;
        }
    }

    return null;
}

function resolveIl2CppMethodInfo(klass, methodName, paramCount) {
    const getMethodAddr = resolveIl2CppExport('il2cpp_class_get_method_from_name');
    if (!getMethodAddr || !klass || klass.isNull()) {
        return null;
    }

    const getMethod = new NativeFunction(getMethodAddr, 'pointer', ['pointer', 'pointer', 'int']);
    const method = getMethod(klass, Memory.allocUtf8String(methodName), paramCount);
    return method && !method.isNull() ? method : null;
}

function ensureLaunchShowMethodInfos() {
    const launchKlass = findIl2CppClass('DarkOrbit', 'LaunchShow');
    if (!launchKlass || launchKlass.isNull()) {
        return false;
    }

    if (!launchShowOnBtnClickMethodInfo || launchShowOnBtnClickMethodInfo.isNull()) {
        launchShowOnBtnClickMethodInfo = resolveIl2CppMethodInfo(launchKlass, 'OnBtnClick', 0);
    }
    if (!launchShowStartBtn0MethodInfo || launchShowStartBtn0MethodInfo.isNull()) {
        launchShowStartBtn0MethodInfo = resolveIl2CppMethodInfo(launchKlass, '<Start>b__55_0', 0);
    }

    return true;
}

function findObjectOfType(klass) {
    if (!findObjectsOfTypeFn || !klass || klass.isNull()) {
        return null;
    }

    const classGetTypeAddr = resolveIl2CppExport('il2cpp_class_get_type');
    const typeGetObjectAddr = resolveIl2CppExport('il2cpp_type_get_object');
    if (!classGetTypeAddr || !typeGetObjectAddr) {
        return null;
    }

    const classGetType = new NativeFunction(classGetTypeAddr, 'pointer', ['pointer']);
    const typeGetObject = new NativeFunction(typeGetObjectAddr, 'pointer', ['pointer']);
    const typeObject = typeGetObject(classGetType(klass));
    if (!typeObject || typeObject.isNull()) {
        return null;
    }

    const array = findObjectsOfTypeFn(typeObject, ptr(0));
    if (!array || array.isNull()) {
        return null;
    }

    const length = array.add(0xC).readS32();
    if (length <= 0) {
        return null;
    }

    return array.add(0x10).readPointer();
}

function discoverGameLoadingManager() {
    if (isLikelyIl2CppObject(lastGameLoadingManager)) {
        return lastGameLoadingManager;
    }

    const glmKlass = findIl2CppClass('DarkOrbit', 'GameLoadingManager');
    const glm = findObjectOfType(glmKlass);
    if (isLikelyIl2CppObject(glm)) {
        lastGameLoadingManager = glm;
        return glm;
    }

    return null;
}

function discoverLaunchShow() {
    if (isLikelyIl2CppObject(lastLaunchShow)) {
        return lastLaunchShow;
    }

    const glm = discoverGameLoadingManager();
    if (isLikelyIl2CppObject(glm)) {
        const fromGlm = readFieldPointer(glm, GLM_LAUNCH_SHOW_OFFSET);
        if (isLikelyIl2CppObject(fromGlm)) {
            lastLaunchShow = fromGlm;
            return fromGlm;
        }
    }

    const launchKlass = findIl2CppClass('DarkOrbit', 'LaunchShow');
    const launchShow = findObjectOfType(launchKlass);
    if (isLikelyIl2CppObject(launchShow)) {
        lastLaunchShow = launchShow;
        if (discoverAt === 0) {
            discoverAt = Date.now();
        }
        return launchShow;
    }

    return null;
}

function readStartButton(launchShow) {
    return readFieldPointer(launchShow, LAUNCH_SHOW_BTN_START_OFFSET);
}

function readShipButton(launchShow) {
    const itemScript = readFieldPointer(launchShow, LAUNCH_SHOW_ITEM_SCRIPT_OFFSET);
    if (!isLikelyIl2CppObject(itemScript)) {
        return null;
    }
    return readFieldPointer(itemScript, SIMPLE_ITEM_SHIP_MBUTTON_OFFSET);
}

function emitState(launchShow) {
    const btnStart = isLikelyIl2CppObject(launchShow) ? readStartButton(launchShow) : null;
    const btnShip = isLikelyIl2CppObject(launchShow) ? readShipButton(launchShow) : null;
    emit('state', {
        launchShow: isLikelyIl2CppObject(launchShow) ? launchShow.toString() : null,
        btnStart: isLikelyIl2CppObject(btnStart) ? btnStart.toString() : null,
        btnShip: isLikelyIl2CppObject(btnShip) ? btnShip.toString() : null,
        glm: isLikelyIl2CppObject(lastGameLoadingManager) ? lastGameLoadingManager.toString() : null,
        hasOnBtnClickMethodInfo: !!(launchShowOnBtnClickMethodInfo && !launchShowOnBtnClickMethodInfo.isNull()),
        hasStartBtn0MethodInfo: !!(launchShowStartBtn0MethodInfo && !launchShowStartBtn0MethodInfo.isNull()),
    });
}

function invokeOnBtnClick(launchShow) {
    if (!ensureIl2CppThreadAttached()) {
        return { ok: false, mode: 'on_btn_click', reason: 'thread_attach_failed' };
    }
    if (!launchShowOnBtnClickFn || !launchShowOnBtnClickMethodInfo || launchShowOnBtnClickMethodInfo.isNull()) {
        return { ok: false, mode: 'on_btn_click', reason: 'method_info_not_ready' };
    }

    try {
        launchShowOnBtnClickFn(launchShow, launchShowOnBtnClickMethodInfo);
        return { ok: true, mode: 'on_btn_click' };
    } catch (e) {
        return { ok: false, mode: 'on_btn_click', reason: String(e) };
    }
}

function invokeStartBtn0(launchShow) {
    if (!ensureIl2CppThreadAttached()) {
        return { ok: false, mode: 'start_handler_55_0', reason: 'thread_attach_failed' };
    }
    if (!launchShowStartBtn0Fn || !launchShowStartBtn0MethodInfo || launchShowStartBtn0MethodInfo.isNull()) {
        return { ok: false, mode: 'start_handler_55_0', reason: 'method_info_not_ready' };
    }

    try {
        launchShowStartBtn0Fn(launchShow, launchShowStartBtn0MethodInfo);
        return { ok: true, mode: 'start_handler_55_0' };
    } catch (e) {
        return { ok: false, mode: 'start_handler_55_0', reason: String(e) };
    }
}

function tryEnterMapOnce() {
    if (enterGameSeen) {
        return { ok: true, reason: 'enter_game_seen' };
    }
    if (mapLoadStarted) {
        emit('waiting_enter_game', {});
        return { ok: false, reason: 'map_load_started' };
    }

    ensureLaunchShowMethodInfos();

    const launchShow = discoverLaunchShow();
    if (!isLikelyIl2CppObject(launchShow)) {
        emit('skip', { reason: 'launch_show_not_found' });
        return { ok: false, reason: 'launch_show_not_found' };
    }

    if (discoverAt === 0) {
        discoverAt = Date.now();
    }

    const elapsedSinceDiscover = Date.now() - discoverAt;
    if (elapsedSinceDiscover < MIN_DELAY_AFTER_DISCOVER_MS) {
        emit('skip', {
            reason: 'waiting_after_discover',
            waitMs: MIN_DELAY_AFTER_DISCOVER_MS - elapsedSinceDiscover,
        });
        return { ok: false, reason: 'waiting_after_discover' };
    }

    attemptCount++;
    emitState(launchShow);

    const mode = attemptCount === 1
        ? 'on_btn_click'
        : 'start_handler_55_0';
    const result = mode === 'start_handler_55_0'
        ? invokeStartBtn0(launchShow)
        : invokeOnBtnClick(launchShow);

    emit(result.ok ? 'invoke' : 'invoke_error', result);
    return result;
}

function scheduleRetries() {
    if (retryTimer !== null) {
        return;
    }

    retryTimer = setInterval(function () {
        if (enterGameSeen) {
            clearInterval(retryTimer);
            retryTimer = null;
            return;
        }
        if (attemptCount >= MAX_ATTEMPTS || mapLoadStarted) {
            emit('waiting_final', { attempts: attemptCount });
            return;
        }
        tryEnterMapOnce();
    }, RETRY_MS);
}

function installHooks(base) {
    Interceptor.attach(base.add(RVA.enterGame), {
        onEnter() {
            enterGameSeen = true;
            emit('enter_game', {});
            if (retryTimer !== null) {
                clearInterval(retryTimer);
                retryTimer = null;
            }
        },
    });

    Interceptor.attach(base.add(RVA.startGame), {
        onEnter(args) {
            mapLoadStarted = true;
            if (isLikelyIl2CppObject(args[0])) {
                lastGameLoadingManager = args[0];
            }
            emit('map_start', {
                glm: isLikelyIl2CppObject(args[0]) ? args[0].toString() : null,
            });
        },
    });

    Interceptor.attach(base.add(RVA.launchShowStart), {
        onEnter(args) {
            if (isLikelyIl2CppObject(args[0])) {
                lastLaunchShow = args[0];
                if (discoverAt === 0) {
                    discoverAt = Date.now();
                }
                emit('launch_show_start', { launchShow: args[0].toString() });
            }
        },
    });

}

function waitForGameAssembly(cb) {
    const mod = Process.findModuleByName('GameAssembly.dll');
    if (mod) {
        cb(mod.base);
        return;
    }

    const timer = setInterval(function () {
        const m = Process.findModuleByName('GameAssembly.dll');
        if (m) {
            clearInterval(timer);
            cb(m.base);
        }
    }, 200);
}

function main() {
    emit('loaded', { version: SCRIPT_VERSION, pid: Process.id });
    waitForGameAssembly(function (base) {
        findObjectsOfTypeFn = new NativeFunction(base.add(RVA.findObjectsOfType), 'pointer', ['pointer', 'pointer']);
        launchShowOnBtnClickFn = new NativeFunction(base.add(RVA.launchShowOnBtnClick), 'void', ['pointer', 'pointer']);
        launchShowStartBtn0Fn = new NativeFunction(base.add(RVA.launchShowStartBtn0), 'void', ['pointer', 'pointer']);
        ensureIl2CppThreadAttached();
        installHooks(base);
        ensureLaunchShowMethodInfos();
        emit('ready', { base: base.toString() });
        scheduleRetries();
        tryEnterMapOnce();
    });
}

rpc.exports = {
    tryEnterMap: function () {
        return JSON.stringify(tryEnterMapOnce());
    },
    getState: function () {
        const launchShow = discoverLaunchShow();
        const btnStart = isLikelyIl2CppObject(launchShow) ? readStartButton(launchShow) : null;
        const btnShip = isLikelyIl2CppObject(launchShow) ? readShipButton(launchShow) : null;
        return JSON.stringify({
            version: SCRIPT_VERSION,
            enterGameSeen: enterGameSeen,
            mapLoadStarted: mapLoadStarted,
            attemptCount: attemptCount,
            launchShow: isLikelyIl2CppObject(launchShow) ? launchShow.toString() : null,
            btnStart: isLikelyIl2CppObject(btnStart) ? btnStart.toString() : null,
            btnShip: isLikelyIl2CppObject(btnShip) ? btnShip.toString() : null,
            hasOnBtnClickMethodInfo: !!(launchShowOnBtnClickMethodInfo && !launchShowOnBtnClickMethodInfo.isNull()),
            hasStartBtn0MethodInfo: !!(launchShowStartBtn0MethodInfo && !launchShowStartBtn0MethodInfo.isNull()),
        });
    },
    stop: function () {
        if (retryTimer !== null) {
            clearInterval(retryTimer);
            retryTimer = null;
        }
        return true;
    },
};

setImmediate(main);
