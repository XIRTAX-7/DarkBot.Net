'use strict';

/**
 * Unity IL2CPP game bridge agent (Dark Orbit v1.1.102).
 * Frida 17+ — RVAs from Il2CppDumper script.json.
 */
const SCHEMA_VERSION = 1;
const AGENT_VERSION = 'unity-bridge-2026-06-24-28';
const MIN_WEB_DATA_LENGTH = 128;
const WEB_AUTOLOGIN_DELAYS_MS = [2000, 5000, 9000, 15000, 22000];
const WEB_AUTOLOGIN_POST_UPDATE_DELAYS_MS = [1000, 3000, 6000, 10000, 15000];
const AUTO_ENTER_MAP_POLL_MS = 400;
const LAUNCH_SHOW_BTN_START_OFFSET = 0x44;
const LAUNCH_SHOW_ITEM_SCRIPT_OFFSET = 0xA4;
const SIMPLE_ITEM_SHIP_MBUTTON_OFFSET = 0x28;

const RVA_VUPLEX = {
    getWebView: 0x3A7790,
    executeJavaScriptCallback: 0x28EFC0,
    executeJavaScriptTask: 0x28EF10,
};

const RVA_GLM = {
    getCanvasWebViewPrefab: 0xDD8F20,
    setCanvasWebViewPrefab: 0xDD9010,
    findObjectsOfType: 0x27BDFB0,
};

const GLM_CANVAS_WEBVIEW_OFFSET = 0x74;
const GLM_LAUNCH_SHOW_OFFSET = 0x1C;
const PREFAB_CACHED_WEBVIEW_OFFSET = 0x4C;
const PREFAB_WEBVIEW_INIT_OFFSET = 0x8C;
const MIN_IL2CPP_OBJECT_ADDRESS = 0x10000;

const IL2CPP_ASSEMBLY_CANDIDATES = [
    'Assembly-CSharp',
    'Assembly-CSharp.dll',
    'UnityEngine.UI',
    'UnityEngine.UI.dll',
    'UnityEngine.CoreModule',
    'UnityEngine.CoreModule.dll',
];

const IL2CPP_STRING_LENGTH_OFFSET = Process.pointerSize * 2;
const IL2CPP_STRING_CHARS_OFFSET = IL2CPP_STRING_LENGTH_OFFSET + 4;

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
    checkUpdateEnterGame: 0xEE1430,
    checkUpdateStart: 0xEE1500,
    openWebLogin: 0xF12140,
    updateWebData: 0xF13510,
    loadingUIShow: 0xF11A90,
    startPreload: 0xF13160,
    getPost: 0xF11300,
    startGame: 0xF12B80,
    launchShowOnBtnClick: 0xEE5460,
    launchShowStart: 0xEE6300,
    launchShowStartBtn0: 0xEE6830,
    launchShowHangarReady: 0xEF2E60,
    launchShowInit: 0xEE52C0,
    launchShowPreInit: 0xEE6010,
    launchShowUpdateButton: 0xEE6B10,
    uiButtonPress: 0x286AC40,
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

const bootstrap = {
    username: null,
    password: null,
    clientUpdateStarted: false,
    clientUpdateComplete: false,
    clientUpdateCompleteAt: 0,
    sessionInjected: false,
    bootstrapHooksReady: false,
    movementHooksReady: false,
    pendingGameLoadingManager: null,
    pendingLaunchShow: null,
    openWebLoginMethodInfo: null,
    updateWebDataMethodInfo: null,
    webLoginOpened: false,
    getPostSeen: false,
    openWebLoginAt: 0,
    getPostAt: 0,
    webAutoLoginScheduled: false,
    mapStartRequested: false,
    mapStartComplete: false,
    mainMenuUiShownAt: 0,
    hangarDataReadyAt: 0,
    launchShowInitAt: 0,
    launchShowStarted: false,
    launchShowStartCompleted: false,
    launchShowStartAt: 0,
    launchShowStartCompletedAt: 0,
    autoStartEnabled: false,
    naturalStartButtonPressAt: 0,
    syntheticStartPressAttempted: false,
    startButtonBoundAt: 0,
    shipButtonBoundAt: 0,
    pendingShipButton: null,
    unityLoginPostAt: 0,
    startButtonPressed: false,
    startButtonPressedAt: 0,
};

let il2cppStringNewFn = null;
let updateWebDataFn = null;
let launchShowStartBtn0Fn = null;
let launchShowOnBtnClickFn = null;
let buttonPressFn = null;
let startGameMethodInfo = null;
let launchShowStartBtn0MethodInfo = null;
let launchShowOnBtnClickMethodInfo = null;
let buttonPressMethodInfo = null;
let getWebViewFn = null;
let executeJavaScriptCallbackFn = null;
let executeJavaScriptTaskFn = null;
let findObjectsOfTypeFn = null;
let gameAssemblyPollTimer = null;
let movementHooksTimer = null;
let autoEnterMapPollTimer = null;
let syntheticStartPressInFlight = false;
let lastAutoEnterSkipReason = '';
let lastAutoEnterSkipLogAt = 0;

function emitAutoEnterMapSkip(reason, extra) {
    const now = Date.now();
    const minIntervalMs = reason === 'btn_start_not_bound'
        || reason === 'ship_button_not_bound'
        || reason === 'auto_start_wait_game'
        || reason === 'launch_show_not_completed'
        ? 5000
        : 8000;
    if (reason === lastAutoEnterSkipReason && now - lastAutoEnterSkipLogAt < minIntervalMs) {
        return;
    }

    lastAutoEnterSkipReason = reason;
    lastAutoEnterSkipLogAt = now;
    emit(Object.assign({
        type: 'auto_enter_map_skip',
        reason: reason,
        ts: now,
    }, extra || {}));
}

function readIl2CppString(ptr) {
    if (!ptr || ptr.isNull()) {
        return null;
    }
    try {
        const len = ptr.add(IL2CPP_STRING_LENGTH_OFFSET).readS32();
        if (len <= 0 || len > 1_000_000) {
            return null;
        }
        return ptr.add(IL2CPP_STRING_CHARS_OFFSET).readUtf16String(len);
    } catch (e) {
        return null;
    }
}

function resolveMethodInfo() {
    if (bootstrap.openWebLoginMethodInfo && !bootstrap.openWebLoginMethodInfo.isNull()) {
        return bootstrap.openWebLoginMethodInfo;
    }
    if (bootstrap.updateWebDataMethodInfo && !bootstrap.updateWebDataMethodInfo.isNull()) {
        return bootstrap.updateWebDataMethodInfo;
    }
    return null;
}

function resolveGameAssembly() {
    const mod = Process.findModuleByName('GameAssembly.dll');
    if (!mod) {
        throw new Error('GameAssembly.dll not loaded');
    }
    return mod;
}

function tryResolveGameAssembly() {
    return Process.findModuleByName('GameAssembly.dll');
}

function findModuleExport(moduleName, name) {
    try {
        if (typeof Module.findExportByName === 'function') {
            return Module.findExportByName(moduleName, name);
        }
    } catch (e) {
        // API может отсутствовать в разных версиях Frida.
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
        // API может отсутствовать в разных версиях Frida.
    }

    try {
        if (typeof Module.getExportByName === 'function') {
            return Module.getExportByName(moduleName, name);
        }
    } catch (e) {
        // API может отсутствовать в разных версиях Frida.
    }

    try {
        if (typeof Module.findGlobalExportByName === 'function') {
            return Module.findGlobalExportByName(name);
        }
    } catch (e) {
        // API может отсутствовать в разных версиях Frida.
    }

    try {
        if (typeof Module.getGlobalExportByName === 'function') {
            return Module.getGlobalExportByName(name);
        }
    } catch (e) {
        // API может отсутствовать в разных версиях Frida.
    }

    return null;
}

function resolveIl2CppStringNew() {
    const addr = findModuleExport('GameAssembly.dll', 'il2cpp_string_new');
    if (!addr) {
        throw new Error('il2cpp_string_new export not found in GameAssembly.dll');
    }
    return new NativeFunction(addr, 'pointer', ['pointer']);
}

function createIl2CppStringUtf8(value) {
    if (!il2cppStringNewFn) {
        il2cppStringNewFn = resolveIl2CppStringNew();
    }
    return il2cppStringNewFn(Memory.allocUtf8String(value));
}

function initLoginNativeFunctions(base) {
    updateWebDataFn = new NativeFunction(base.add(RVA.updateWebData), 'void', ['pointer', 'pointer', 'pointer']);
    launchShowStartBtn0Fn = new NativeFunction(base.add(RVA.launchShowStartBtn0), 'void', ['pointer', 'pointer']);
    launchShowOnBtnClickFn = new NativeFunction(base.add(RVA.launchShowOnBtnClick), 'void', ['pointer', 'pointer']);
    buttonPressFn = new NativeFunction(base.add(RVA.uiButtonPress), 'void', ['pointer', 'pointer']);
    getWebViewFn = new NativeFunction(base.add(RVA_VUPLEX.getWebView), 'pointer', ['pointer', 'pointer']);
    executeJavaScriptCallbackFn = new NativeFunction(
        base.add(RVA_VUPLEX.executeJavaScriptCallback),
        'void',
        ['pointer', 'pointer', 'pointer', 'pointer']);
    executeJavaScriptTaskFn = new NativeFunction(
        base.add(RVA_VUPLEX.executeJavaScriptTask),
        'pointer',
        ['pointer', 'pointer', 'pointer']);
    findObjectsOfTypeFn = new NativeFunction(
        base.add(RVA_GLM.findObjectsOfType),
        'pointer',
        ['pointer', 'pointer']);
}

function resolveIl2CppExport(name) {
    return findModuleExport('GameAssembly.dll', name);
}

function captureGameLoadingManager(candidate, source) {
    if (!isLikelyIl2CppObject(candidate)) {
        return false;
    }

    const alreadyKnown = bootstrap.pendingGameLoadingManager
        && !bootstrap.pendingGameLoadingManager.isNull()
        && bootstrap.pendingGameLoadingManager.equals(candidate);

    bootstrap.pendingGameLoadingManager = candidate;
    if (!alreadyKnown) {
        emit({
            type: 'game_loading_manager_found',
            source: source,
            ts: Date.now(),
        });
        scheduleWebAutoLogin(true);
    }
    return true;
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
    const assemblyOpen = new NativeFunction(
        assemblyOpenAddr,
        'pointer',
        ['pointer', 'pointer']);
    const getImage = new NativeFunction(getImageAddr, 'pointer', ['pointer']);
    const classFromName = new NativeFunction(
        classFromNameAddr,
        'pointer',
        ['pointer', 'pointer', 'pointer']);

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

function ensureEnterMapMethodInfo() {
    const launchKlass = findIl2CppClass('DarkOrbit', 'LaunchShow');
    if (!launchShowStartBtn0MethodInfo || launchShowStartBtn0MethodInfo.isNull()) {
        launchShowStartBtn0MethodInfo = resolveIl2CppMethodInfo(launchKlass, '<Start>b__55_0', 0);
    }
    if (!launchShowOnBtnClickMethodInfo || launchShowOnBtnClickMethodInfo.isNull()) {
        launchShowOnBtnClickMethodInfo = resolveIl2CppMethodInfo(launchKlass, 'OnBtnClick', 0);
    }
    ensureButtonPressMethodInfo();
}

function ensureButtonPressMethodInfo() {
    if (buttonPressMethodInfo && !buttonPressMethodInfo.isNull()) {
        return true;
    }

    const buttonKlass = findIl2CppClass('UnityEngine.UI', 'Button');
    if (buttonKlass && !buttonKlass.isNull()) {
        buttonPressMethodInfo = resolveIl2CppMethodInfo(buttonKlass, 'Press', 0);
    }

    return !!(buttonPressMethodInfo && !buttonPressMethodInfo.isNull());
}

function resolveLaunchShow(gameLoadingManager) {
    if (!gameLoadingManager || gameLoadingManager.isNull()) {
        return null;
    }
    return readFieldPointer(gameLoadingManager, GLM_LAUNCH_SHOW_OFFSET);
}

function markMainMenuVisible(source) {
    if (bootstrap.mainMenuUiShownAt === 0) {
        bootstrap.mainMenuUiShownAt = Date.now();
        emit({
            type: 'main_menu_loading_ui',
            schemaVersion: SCHEMA_VERSION,
            source: source,
            ts: Date.now(),
        });
    }
    scheduleAutoEnterMap(source);
}

function looksLikeAnyLoginPayload(webData) {
    if (!webData || webData.length < 32) {
        return false;
    }
    const lower = webData.toLowerCase();
    return lower.indexOf('sessionid') >= 0
        || lower.indexOf('userid') >= 0
        || lower.indexOf('mapid') >= 0;
}

function findGameLoadingManagerClass() {
    return findIl2CppClass('DarkOrbit', 'GameLoadingManager');
}

function findGameLoadingManagerViaUnity(base) {
    if (!findObjectsOfTypeFn) {
        return null;
    }

    const klass = findGameLoadingManagerClass();
    if (!klass || klass.isNull()) {
        return null;
    }

    const classGetTypeAddr = resolveIl2CppExport('il2cpp_class_get_type');
    const typeGetObjectAddr = resolveIl2CppExport('il2cpp_type_get_object');
    if (!classGetTypeAddr || !typeGetObjectAddr) {
        return null;
    }

    const classGetType = new NativeFunction(classGetTypeAddr, 'pointer', ['pointer']);
    const typeGetObject = new NativeFunction(typeGetObjectAddr, 'pointer', ['pointer']);
    const type = classGetType(klass);
    const typeObject = typeGetObject(type);
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

function findLaunchShowViaUnity() {
    if (!findObjectsOfTypeFn) {
        return null;
    }

    const klass = findIl2CppClass('DarkOrbit', 'LaunchShow');
    if (!klass || klass.isNull()) {
        return null;
    }

    const classGetTypeAddr = resolveIl2CppExport('il2cpp_class_get_type');
    const typeGetObjectAddr = resolveIl2CppExport('il2cpp_type_get_object');
    if (!classGetTypeAddr || !typeGetObjectAddr) {
        return null;
    }

    const classGetType = new NativeFunction(classGetTypeAddr, 'pointer', ['pointer']);
    const typeGetObject = new NativeFunction(typeGetObjectAddr, 'pointer', ['pointer']);
    const type = classGetType(klass);
    const typeObject = typeGetObject(type);
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

function discoverLaunchShow(reason) {
    if (isLikelyIl2CppObject(bootstrap.pendingLaunchShow)) {
        return bootstrap.pendingLaunchShow;
    }

    try {
        const launchShow = findLaunchShowViaUnity();
        if (isLikelyIl2CppObject(launchShow)) {
            captureLaunchShow(launchShow, reason || 'find_launch_show');
            return launchShow;
        }
    } catch (e) {
        emit({ type: 'warn', message: 'discover_launch_show: ' + e, ts: Date.now() });
    }

    return null;
}

function discoverGameLoadingManager(reason) {
    if (bootstrap.pendingGameLoadingManager && !bootstrap.pendingGameLoadingManager.isNull()) {
        return true;
    }
    if (!gameAssemblyBase) {
        return false;
    }

    try {
        const glm = findGameLoadingManagerViaUnity(gameAssemblyBase);
        if (glm && !glm.isNull()) {
            return captureGameLoadingManager(glm, reason || 'find_objects_of_type');
        }
    } catch (e) {
        emit({ type: 'warn', message: 'discover_glm: ' + e, ts: Date.now() });
    }

    return false;
}

function scheduleGameLoadingManagerDiscovery() {
    let attempts = 0;
    const timer = setInterval(function () {
        attempts++;
        if (bootstrap.sessionInjected || attempts > 30) {
            clearInterval(timer);
            return;
        }
        discoverGameLoadingManager('poll_' + attempts);
    }, 2000);
}

function invokeExecuteJavaScript(webView, jsStr) {
    if (executeJavaScriptCallbackFn) {
        try {
            executeJavaScriptCallbackFn(webView, jsStr, ptr(0), ptr(0));
            return 'callback';
        } catch (e) {
            if (!executeJavaScriptTaskFn) {
                throw e;
            }
        }
    }

    if (executeJavaScriptTaskFn) {
        executeJavaScriptTaskFn(webView, jsStr, ptr(0));
        return 'task';
    }

    throw new Error('ExecuteJavaScript native functions are not initialized');
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
        return addr >= MIN_IL2CPP_OBJECT_ADDRESS && (addr % 4) === 0;
    } catch (e) {
        return false;
    }
}

function resolveWebView(gameLoadingManager) {
    if (!isLikelyIl2CppObject(gameLoadingManager)) {
        return null;
    }

    const prefab = readFieldPointer(gameLoadingManager, GLM_CANVAS_WEBVIEW_OFFSET);
    if (!isLikelyIl2CppObject(prefab)) {
        return null;
    }

    const cached = readFieldPointer(prefab, PREFAB_CACHED_WEBVIEW_OFFSET);
    if (isLikelyIl2CppObject(cached)) {
        return cached;
    }

    const initWebView = readFieldPointer(prefab, PREFAB_WEBVIEW_INIT_OFFSET);
    if (isLikelyIl2CppObject(initWebView)) {
        return initWebView;
    }

    if (getWebViewFn) {
        try {
            const webView = getWebViewFn(prefab, ptr(0));
            if (isLikelyIl2CppObject(webView)) {
                return webView;
            }
        } catch (e) {
            emit({ type: 'web_autologin_error', message: 'get_WebView: ' + e, ts: Date.now() });
        }
    }

    return null;
}

function buildAutoLoginScript() {
    const user = bootstrap.username || '';
    const pass = bootstrap.password || '';
    const hasCredentials = !!(user && pass);
    // Повторяем ручной путь из legacy Electron inject/login.js: click → value → submit.
    return '(function(){try{'
        + 'var u=document.getElementById("bgcdw_login_form_username");'
        + 'var p=document.getElementById("bgcdw_login_form_password");'
        + 'if(!u||!p)return "no_form";'
        + 'if(!' + JSON.stringify(hasCredentials) + ')return "no_credentials";'
        + 'u.click();u.value=' + JSON.stringify(user) + ';'
        + 'p.click();p.value=' + JSON.stringify(pass) + ';'
        + 'if(document.querySelectorAll(\'iframe[title="reCAPTCHA"]\').length)return "captcha";'
        + 'var btn=document.querySelector("input.bgcdw_login_form_login[type=submit]");'
        + 'if(btn){btn.click();return "submit";}'
        + 'return "no_button";'
        + '}catch(e){return "err:"+e;}})()';
}

function hasAutoLoginMaterial() {
    return !!(bootstrap.username && bootstrap.password);
}

function tryWebViewAutoLogin() {
    if (bootstrap.sessionInjected) {
        return;
    }
    if (!hasAutoLoginMaterial()) {
        emit({ type: 'web_autologin_skip', reason: 'no_auth_material', ts: Date.now() });
        return;
    }
    if ((!executeJavaScriptCallbackFn && !executeJavaScriptTaskFn) || !gameAssemblyBase) {
        emit({ type: 'web_autologin_skip', reason: 'vuplex_not_ready', ts: Date.now() });
        return;
    }

    if (!bootstrap.pendingGameLoadingManager || bootstrap.pendingGameLoadingManager.isNull()) {
        discoverGameLoadingManager('autologin_retry');
    }

    const glm = bootstrap.pendingGameLoadingManager;
    if (!glm || glm.isNull()) {
        emit({ type: 'web_autologin_skip', reason: 'no_game_loading_manager', ts: Date.now() });
        return;
    }

    const webView = resolveWebView(glm);
    if (!webView) {
        emit({ type: 'web_autologin_skip', reason: 'no_webview', ts: Date.now() });
        return;
    }

    try {
        const js = buildAutoLoginScript();
        const jsStr = createIl2CppStringUtf8(js);
        const mode = invokeExecuteJavaScript(webView, jsStr);
        emit({ type: 'web_autologin_sent', mode: mode, ts: Date.now() });
    } catch (e) {
        emit({ type: 'web_autologin_error', message: String(e), ts: Date.now() });
    }
}

function scheduleWebAutoLoginDelays(delaysMs, phase) {
    if (bootstrap.sessionInjected) {
        return;
    }
    if (!hasAutoLoginMaterial()) {
        return;
    }

    delaysMs.forEach(function (delayMs) {
        setTimeout(function () {
            if (!bootstrap.sessionInjected) {
                tryWebViewAutoLogin();
            }
        }, delayMs);
    });
    emit({
        type: 'web_autologin_scheduled',
        phase: phase || 'default',
        delaysMs: delaysMs,
        ts: Date.now(),
    });
}

function scheduleWebAutoLogin(force) {
    if (!force && (bootstrap.webAutoLoginScheduled || bootstrap.sessionInjected)) {
        return;
    }
    if (!hasAutoLoginMaterial()) {
        return;
    }

    bootstrap.webAutoLoginScheduled = true;
    scheduleWebAutoLoginDelays(WEB_AUTOLOGIN_DELAYS_MS, 'initial');
}

function schedulePostUpdateWebAutoLogin() {
    if (bootstrap.sessionInjected) {
        return;
    }
    scheduleWebAutoLoginDelays(WEB_AUTOLOGIN_POST_UPDATE_DELAYS_MS, 'post_update');
}

function markClientUpdateComplete(source) {
    if (bootstrap.clientUpdateComplete) {
        return;
    }

    bootstrap.clientUpdateComplete = true;
    bootstrap.clientUpdateCompleteAt = Date.now();
    emit({
        type: 'client_update_complete',
        schemaVersion: SCHEMA_VERSION,
        source: source || 'hook',
        ts: Date.now(),
    });
    schedulePostUpdateWebAutoLogin();
}

function readStartButton(launchShow) {
    if (!isLikelyIl2CppObject(launchShow)) {
        return null;
    }
    return readFieldPointer(launchShow, LAUNCH_SHOW_BTN_START_OFFSET);
}

function readShipButton(launchShow) {
    if (!isLikelyIl2CppObject(launchShow)) {
        return null;
    }

    const itemScript = readFieldPointer(launchShow, LAUNCH_SHOW_ITEM_SCRIPT_OFFSET);
    if (!isLikelyIl2CppObject(itemScript)) {
        return null;
    }

    return readFieldPointer(itemScript, SIMPLE_ITEM_SHIP_MBUTTON_OFFSET);
}

function getStartButtonState(launchShow) {
    const btnStart = readStartButton(launchShow);
    if (!isLikelyIl2CppObject(btnStart)) {
        return { state: 'missing', button: null };
    }

    return { state: 'ready', button: btnStart };
}

function getShipButtonState(launchShow) {
    const btnShip = readShipButton(launchShow);
    if (!isLikelyIl2CppObject(btnShip)) {
        return { state: 'missing', button: null };
    }

    const btnStart = readStartButton(launchShow);
    if (isLikelyIl2CppObject(btnStart) && btnShip.equals(btnStart)) {
        return { state: 'same_as_start', button: btnShip };
    }

    return { state: 'ready', button: btnShip };
}

function noteStartButtonBound(btnStart) {
    if (bootstrap.startButtonBoundAt !== 0 || !isLikelyIl2CppObject(btnStart)) {
        return;
    }

    bootstrap.startButtonBoundAt = Date.now();
    emit({
        type: 'start_button_bound',
        schemaVersion: SCHEMA_VERSION,
        button: btnStart.toString(),
        ts: Date.now(),
    });
}

function noteShipButtonBound(btnShip) {
    if (bootstrap.shipButtonBoundAt !== 0 || !isLikelyIl2CppObject(btnShip)) {
        return;
    }

    bootstrap.shipButtonBoundAt = Date.now();
    bootstrap.pendingShipButton = btnShip;
    emit({
        type: 'ship_button_bound',
        schemaVersion: SCHEMA_VERSION,
        button: btnShip.toString(),
        ts: Date.now(),
    });
}

function probeStartButtonBinding(launchShow) {
    const buttonState = getStartButtonState(launchShow);
    if (buttonState.state === 'ready') {
        noteStartButtonBound(buttonState.button);
    }
    return buttonState;
}

function probeShipButtonBinding(launchShow) {
    const buttonState = getShipButtonState(launchShow);
    if (buttonState.state === 'ready') {
        noteShipButtonBound(buttonState.button);
    }
    return buttonState;
}

function captureLaunchShow(launchShow, source) {
    if (!isLikelyIl2CppObject(launchShow)) {
        return;
    }

    bootstrap.pendingLaunchShow = launchShow;
    probeStartButtonBinding(launchShow);
    probeShipButtonBinding(launchShow);
}

function onLaunchShowStartCompleted(launchShow) {
    if (!isLikelyIl2CppObject(launchShow)) {
        return;
    }

    bootstrap.launchShowStarted = true;
    bootstrap.launchShowStartCompleted = true;
    bootstrap.launchShowStartCompletedAt = Date.now();
    captureLaunchShow(launchShow, 'launch_show_start_completed');

    if (!bootstrap.sessionInjected || bootstrap.mapStartComplete) {
        return;
    }

    scheduleAutoEnterMap('launch_show_start_completed');
    tryAutoEnterMap();
}

function isStartButtonObject(btn) {
    if (!isLikelyIl2CppObject(bootstrap.pendingLaunchShow) || !isLikelyIl2CppObject(btn)) {
        return false;
    }

    const btnStart = readStartButton(bootstrap.pendingLaunchShow);
    return isLikelyIl2CppObject(btnStart) && btn.equals(btnStart);
}

function isShipButtonObject(btn) {
    if (!isLikelyIl2CppObject(bootstrap.pendingLaunchShow) || !isLikelyIl2CppObject(btn)) {
        return false;
    }

    const btnShip = readShipButton(bootstrap.pendingLaunchShow);
    return isLikelyIl2CppObject(btnShip) && btn.equals(btnShip);
}

function noteNaturalShipButtonPress(btn, source) {
    if (bootstrap.naturalStartButtonPressAt > 0) {
        return;
    }

    bootstrap.naturalStartButtonPressAt = Date.now();
    bootstrap.syntheticStartPressAttempted = true;
    bootstrap.mapStartRequested = true;
    stopAutoEnterMapPoller();
    emit({
        type: 'natural_ship_button_press',
        schemaVersion: SCHEMA_VERSION,
        source: source || 'ui_button_press',
        button: btn.toString(),
        msSinceLaunchShowStart: bootstrap.launchShowStartAt > 0
            ? Date.now() - bootstrap.launchShowStartAt
            : 0,
        autoStartEnabled: bootstrap.autoStartEnabled,
        ts: Date.now(),
    });
}

function noteNaturalStartButtonPress(btn, source) {
    if (bootstrap.naturalStartButtonPressAt > 0) {
        return;
    }

    bootstrap.naturalStartButtonPressAt = Date.now();
    bootstrap.syntheticStartPressAttempted = true;
    bootstrap.mapStartRequested = true;
    stopAutoEnterMapPoller();
    emit({
        type: 'natural_start_button_press',
        schemaVersion: SCHEMA_VERSION,
        source: source || 'ui_button_press',
        button: btn.toString(),
        msSinceLaunchShowStart: bootstrap.launchShowStartAt > 0
            ? Date.now() - bootstrap.launchShowStartAt
            : 0,
        autoStartEnabled: bootstrap.autoStartEnabled,
        ts: Date.now(),
    });
}

function canPressShipButtonNow(launchShow) {
    if (!bootstrap.sessionInjected) {
        return { ok: false, reason: 'session_not_ready' };
    }
    if (!bootstrap.launchShowStartCompleted) {
        return { ok: false, reason: 'launch_show_not_completed' };
    }
    if (bootstrap.mapStartComplete) {
        return { ok: false, reason: 'map_already_started' };
    }
    if (bootstrap.syntheticStartPressAttempted || bootstrap.startButtonPressed) {
        return { ok: false, reason: 'enter_already_pressed' };
    }

    const buttonState = getShipButtonState(launchShow);
    if (buttonState.state === 'missing') {
        return { ok: false, reason: 'ship_button_not_bound' };
    }
    if (buttonState.state === 'same_as_start') {
        return { ok: false, reason: 'ship_button_same_as_start' };
    }

    noteShipButtonBound(buttonState.button);
    return {
        ok: true,
        reason: 'ship_button_ready',
        button: buttonState.button ? buttonState.button.toString() : null,
    };
}

function canPressStartButtonNow(launchShow) {
    if (!bootstrap.sessionInjected) {
        return { ok: false, reason: 'session_not_ready' };
    }
    if (!bootstrap.launchShowStartCompleted) {
        return { ok: false, reason: 'launch_show_not_completed' };
    }
    if (bootstrap.autoStartEnabled) {
        return { ok: false, reason: 'auto_start_wait_game' };
    }
    if (bootstrap.mapStartComplete) {
        return { ok: false, reason: 'map_already_started' };
    }
    if (bootstrap.syntheticStartPressAttempted || bootstrap.startButtonPressed) {
        return { ok: false, reason: 'start_already_pressed' };
    }

    const buttonState = getStartButtonState(launchShow);
    if (buttonState.state === 'missing') {
        return { ok: false, reason: 'btn_start_not_bound' };
    }

    noteStartButtonBound(buttonState.button);
    return {
        ok: true,
        reason: 'btn_start_ready',
        button: buttonState.button ? buttonState.button.toString() : null,
    };
}

function resolveLaunchShowInstance(glm) {
    if (isLikelyIl2CppObject(bootstrap.pendingLaunchShow)) {
        return bootstrap.pendingLaunchShow;
    }
    return resolveLaunchShow(glm);
}

function noteHangarFromSessionWebData(webData) {
    const payload = parseLoginNodePayloadFromWebData(webData);
    if (!payload) {
        return;
    }

    const hasHangars = Object.prototype.hasOwnProperty.call(payload, 'hangars');
    const autoStart = payload.autoStartEnabled === 1
        || payload.autoStartEnabled === '1'
        || payload.autoStartEnabled === true;

    bootstrap.autoStartEnabled = autoStart;

    if (!hasHangars && !autoStart) {
        return;
    }

    if (bootstrap.hangarDataReadyAt === 0) {
        bootstrap.hangarDataReadyAt = Date.now();
        bootstrap.autoStartEnabled = autoStart;
        emit({
            type: 'main_menu_hangar_ready',
            schemaVersion: SCHEMA_VERSION,
            reason: 'session_web_data',
            autoStartEnabled: autoStart,
            ts: Date.now(),
        });
    } else if (autoStart) {
        bootstrap.autoStartEnabled = true;
    }
}

function invokeUiButtonPress(btn) {
    ensureEnterMapMethodInfo();
    syntheticStartPressInFlight = true;

    if (ensureButtonPressMethodInfo() && buttonPressFn && isLikelyIl2CppObject(btn)) {
        buttonPressFn(btn, buttonPressMethodInfo);
        syntheticStartPressInFlight = false;
        return 'ui_button_press';
    }

    syntheticStartPressInFlight = false;
    return null;
}

function invokeStartButtonPress(launchShow, btnStart, gate) {
    return invokeUiButtonPress(btnStart);
}

function tryPressShipButton(launchShow, gate, elapsedMs, btnShipOverride) {
    const btnShip = isLikelyIl2CppObject(btnShipOverride)
        ? btnShipOverride
        : readShipButton(launchShow);

    if (!isLikelyIl2CppObject(btnShip)) {
        emit({
            type: 'auto_enter_map_skip',
            reason: 'ship_button_not_bound',
            gate: gate,
            hasBtnShip: false,
            hasMethodInfo: ensureButtonPressMethodInfo(),
            ts: Date.now(),
        });
        return false;
    }

    const btnStart = readStartButton(launchShow);
    if (isLikelyIl2CppObject(btnStart) && btnShip.equals(btnStart)) {
        emit({
            type: 'auto_enter_map_skip',
            reason: 'ship_button_same_as_start',
            gate: gate,
            button: btnShip.toString(),
            ts: Date.now(),
        });
        return false;
    }

    try {
        const mode = invokeUiButtonPress(btnShip);
        if (!mode) {
            emit({
                type: 'auto_enter_map_skip',
                reason: 'method_info_not_ready',
                gate: gate,
                hasBtnShip: true,
                hasMethodInfo: ensureButtonPressMethodInfo(),
                button: btnShip.toString(),
                ts: Date.now(),
            });
            return false;
        }

        bootstrap.syntheticStartPressAttempted = true;
        bootstrap.mapStartRequested = true;
        emit({
            type: 'auto_enter_map',
            mode: mode,
            gate: gate,
            elapsedMs: elapsedMs || 0,
            button: btnShip.toString(),
            enterVia: 'ship',
            autoStartEnabled: bootstrap.autoStartEnabled,
            ts: Date.now(),
        });
        return true;
    } catch (e) {
        bootstrap.mapStartRequested = false;
        emit({
            type: 'auto_enter_map_error',
            mode: 'ship_button_press',
            gate: gate,
            message: String(e),
            ts: Date.now(),
        });
        return false;
    }
}

function tryPressStartButton(launchShow, gate, elapsedMs, btnStartOverride) {
    const btnStart = isLikelyIl2CppObject(btnStartOverride)
        ? btnStartOverride
        : readStartButton(launchShow);

    if (!isLikelyIl2CppObject(btnStart)) {
        emit({
            type: 'auto_enter_map_skip',
            reason: 'btn_start_not_bound',
            gate: gate,
            hasBtnStart: false,
            hasMethodInfo: ensureButtonPressMethodInfo(),
            hasStartBtn0: !!(launchShowStartBtn0MethodInfo && !launchShowStartBtn0MethodInfo.isNull()),
            ts: Date.now(),
        });
        return false;
    }

    try {
        const mode = invokeStartButtonPress(launchShow, btnStart, gate);
        if (!mode) {
            emit({
                type: 'auto_enter_map_skip',
                reason: 'method_info_not_ready',
                gate: gate,
                hasBtnStart: true,
                hasMethodInfo: ensureButtonPressMethodInfo(),
                hasStartBtn0: !!(launchShowStartBtn0MethodInfo && !launchShowStartBtn0MethodInfo.isNull()),
                button: btnStart.toString(),
                ts: Date.now(),
            });
            return false;
        }

        bootstrap.syntheticStartPressAttempted = true;
        bootstrap.mapStartRequested = true;
        emit({
            type: 'auto_enter_map',
            mode: mode,
            gate: gate,
            elapsedMs: elapsedMs || 0,
            button: btnStart.toString(),
            autoStartEnabled: bootstrap.autoStartEnabled,
            ts: Date.now(),
        });
        return true;
    } catch (e) {
        bootstrap.mapStartRequested = false;
        emit({
            type: 'auto_enter_map_error',
            mode: 'btn_start_press',
            gate: gate,
            message: String(e),
            ts: Date.now(),
        });
        return false;
    }
}

function ensureAutoEnterMapPoller() {
    if (autoEnterMapPollTimer !== null || bootstrap.mapStartComplete) {
        return;
    }

    autoEnterMapPollTimer = setInterval(function () {
        if (bootstrap.mapStartComplete) {
            clearInterval(autoEnterMapPollTimer);
            autoEnterMapPollTimer = null;
            return;
        }
        tryAutoEnterMap();
    }, AUTO_ENTER_MAP_POLL_MS);

    emit({
        type: 'auto_enter_map_poller_started',
        pollMs: AUTO_ENTER_MAP_POLL_MS,
        ts: Date.now(),
    });
}

function stopAutoEnterMapPoller() {
    if (autoEnterMapPollTimer === null) {
        return;
    }
    clearInterval(autoEnterMapPollTimer);
    autoEnterMapPollTimer = null;
}

function notifyMainMenuProgress(phase) {
    ensureAutoEnterMapPoller();
    emit({
        type: 'main_menu_progress',
        phase: phase,
        mainMenuUiShownAt: bootstrap.mainMenuUiShownAt,
        hangarDataReadyAt: bootstrap.hangarDataReadyAt,
        launchShowInitAt: bootstrap.launchShowInitAt,
        ts: Date.now(),
    });
}

function tryAutoEnterMap() {
    emitAutoEnterMapSkip('delegated_to_enter_map_agent');
    return;

    if (bootstrap.mapStartComplete || bootstrap.mapStartRequested) {
        return;
    }
    if (bootstrap.naturalStartButtonPressAt > 0) {
        return;
    }
    if (!bootstrap.sessionInjected) {
        emitAutoEnterMapSkip('session_not_ready');
        return;
    }
    if (!gameAssemblyBase) {
        emitAutoEnterMapSkip('game_assembly_not_ready');
        return;
    }

    ensureEnterMapMethodInfo();

    if (!bootstrap.pendingGameLoadingManager || bootstrap.pendingGameLoadingManager.isNull()) {
        discoverGameLoadingManager('auto_enter_map');
    }

    const glm = bootstrap.pendingGameLoadingManager;
    if (!glm || glm.isNull()) {
        emitAutoEnterMapSkip('no_game_loading_manager');
        return;
    }

    let launchShow = resolveLaunchShowInstance(glm);
    if (!isLikelyIl2CppObject(launchShow)) {
        launchShow = discoverLaunchShow('auto_enter_map');
    }
    if (!isLikelyIl2CppObject(launchShow)) {
        emitAutoEnterMapSkip('launch_show_not_ready');
        return;
    }

    const readiness = canPressShipButtonNow(launchShow);
    if (!readiness.ok) {
        emitAutoEnterMapSkip(readiness.reason, {
            elapsedMs: readiness.elapsedMs || 0,
            button: readiness.button || null,
        });
        return;
    }

    const elapsedMs = bootstrap.launchShowStartCompletedAt > 0
        ? Date.now() - bootstrap.launchShowStartCompletedAt
        : 0;
    tryPressShipButton(launchShow, readiness.reason, elapsedMs, null);
}

function scheduleAutoEnterMap(phase) {
    emitAutoEnterMapSkip('delegated_to_enter_map_agent', {
        phase: phase || 'scheduled',
    });
    return;

    if (!bootstrap.sessionInjected || bootstrap.mapStartComplete) {
        return;
    }
    notifyMainMenuProgress(phase || 'scheduled');
    ensureAutoEnterMapPoller();
}

function markSessionReady(reason) {
    if (bootstrap.sessionInjected) {
        return;
    }

    bootstrap.sessionInjected = true;
    emit({
        type: 'session_injected',
        schemaVersion: SCHEMA_VERSION,
        reason: reason,
        ts: Date.now(),
    });
    scheduleAutoEnterMap('session_ready');
}

function parseLoginNodePayloadFromWebData(webData) {
    if (!webData || webData.length < MIN_WEB_DATA_LENGTH) {
        return null;
    }

    const trimmed = webData.trim();
    if (trimmed.charAt(0) !== '{') {
        return null;
    }

    try {
        const outer = JSON.parse(trimmed);
        if (!outer || typeof outer !== 'object' || Array.isArray(outer)) {
            return null;
        }

        if (typeof outer.data === 'string') {
            const innerTrimmed = outer.data.trim();
            if (innerTrimmed.charAt(0) === '{') {
                return JSON.parse(innerTrimmed);
            }
        }

        return outer;
    } catch (e) {
        return null;
    }
}

function loginNodePayloadLooksValid(payload) {
    if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
        return false;
    }

    const hasUser = Object.prototype.hasOwnProperty.call(payload, 'userID')
        || Object.prototype.hasOwnProperty.call(payload, 'userId');
    const hasMap = Object.prototype.hasOwnProperty.call(payload, 'mapID')
        || Object.prototype.hasOwnProperty.call(payload, 'mapId');
    const hasSession = Object.prototype.hasOwnProperty.call(payload, 'sessionID')
        || Object.prototype.hasOwnProperty.call(payload, 'sessionId');
    const hasClientData = Object.prototype.hasOwnProperty.call(payload, 'itemXmlHash')
        || Object.prototype.hasOwnProperty.call(payload, 'resourcesXmlHash')
        || Object.prototype.hasOwnProperty.call(payload, 'gameclientPath')
        || Object.prototype.hasOwnProperty.call(payload, 'basePath');

    return hasUser && hasMap && hasSession && hasClientData;
}

function looksLikeLoginNodeData(webData) {
    const payload = parseLoginNodePayloadFromWebData(webData);
    if (loginNodePayloadLooksValid(payload)) {
        return true;
    }

    return webData.length >= 2000 && looksLikeAnyLoginPayload(webData);
}

function installBootstrapHooks(base) {
    trackInterceptor(Interceptor.attach(base.add(RVA.checkUpdateStart), {
        onEnter() {
            if (bootstrap.clientUpdateStarted) {
                return;
            }
            bootstrap.clientUpdateStarted = true;
            emit({
                type: 'client_update_started',
                schemaVersion: SCHEMA_VERSION,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] CheckUpdateAndDownload.StartCheckAddressable @ ' + base.add(RVA.checkUpdateStart));

    trackInterceptor(Interceptor.attach(base.add(RVA.checkUpdateEnterGame), {
        onLeave() {
            markClientUpdateComplete('addressables_enter_game');
        },
    }));
    console.log('[unity_bridge] CheckUpdateAndDownload.EnterGame @ ' + base.add(RVA.checkUpdateEnterGame));

    trackInterceptor(Interceptor.attach(base.add(RVA.loadingUIShow), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'loading_ui_show');
            if (bootstrap.sessionInjected && !bootstrap.mapStartComplete) {
                if (bootstrap.mainMenuUiShownAt === 0) {
                    bootstrap.mainMenuUiShownAt = Date.now();
                    emit({
                        type: 'main_menu_loading_ui',
                        schemaVersion: SCHEMA_VERSION,
                        ts: Date.now(),
                    });
                }
                scheduleAutoEnterMap('loading_ui_show');
            }
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.LoadingUIShow @ ' + base.add(RVA.loadingUIShow));

    trackInterceptor(Interceptor.attach(base.add(RVA.startPreload), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'start_preload');
            if (args[1] && !args[1].isNull()) {
                bootstrap.openWebLoginMethodInfo = args[1];
            }
            scheduleWebAutoLogin();
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.StartPreload @ ' + base.add(RVA.startPreload));

    trackInterceptor(Interceptor.attach(base.add(RVA_GLM.getCanvasWebViewPrefab), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'get_canvas_webview_prefab');
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.get_canvasWebViewPrefab @ ' + base.add(RVA_GLM.getCanvasWebViewPrefab));

    trackInterceptor(Interceptor.attach(base.add(RVA_GLM.setCanvasWebViewPrefab), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'set_canvas_webview_prefab');
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.set_canvasWebViewPrefab @ ' + base.add(RVA_GLM.setCanvasWebViewPrefab));

    trackInterceptor(Interceptor.attach(base.add(RVA.getPost), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'get_post');
            if (args[2] && !args[2].isNull()) {
                bootstrap.updateWebDataMethodInfo = args[2];
            }
            bootstrap.getPostSeen = true;
            bootstrap.getPostAt = Date.now();
            const url = readIl2CppString(args[1]);
            emit({
                type: 'get_post',
                schemaVersion: SCHEMA_VERSION,
                url: url,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.GetPost @ ' + base.add(RVA.getPost));

    trackInterceptor(Interceptor.attach(base.add(RVA.updateWebData), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'update_web_data');
            if (args[2] && !args[2].isNull()) {
                bootstrap.updateWebDataMethodInfo = args[2];
            }
            const webData = readIl2CppString(args[1]);
            if (looksLikeLoginNodeData(webData)) {
                noteHangarFromSessionWebData(webData);
                emit({
                    type: 'update_web_data_seen',
                    schemaVersion: SCHEMA_VERSION,
                    length: webData.length,
                    ts: Date.now(),
                });
                markSessionReady('natural_update_web_data');
            }
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.UpdateWebData probe @ ' + base.add(RVA.updateWebData));

    trackInterceptor(Interceptor.attach(base.add(RVA.startGame), {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'start_game');
            if (args[1] && !args[1].isNull()) {
                startGameMethodInfo = args[1];
            }
            bootstrap.mapStartComplete = true;
            bootstrap.startButtonPressed = true;
            bootstrap.startButtonPressedAt = Date.now();
            bootstrap.mapStartRequested = true;
            bootstrap.syntheticStartPressAttempted = true;
            stopAutoEnterMapPoller();
            scheduleMovementHooks();
            emit({
                type: 'map_start',
                mode: 'game_loading_manager_start_game',
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.StartGame @ ' + base.add(RVA.startGame));

    trackInterceptor(Interceptor.attach(base.add(RVA.uiButtonPress), {
        onEnter(args) {
            if (args[1] && !args[1].isNull()) {
                buttonPressMethodInfo = args[1];
            }
            if (!syntheticStartPressInFlight
                && isLikelyIl2CppObject(args[0])) {
                if (isShipButtonObject(args[0])) {
                    noteNaturalShipButtonPress(args[0], 'ui_button_press');
                } else if (isStartButtonObject(args[0])) {
                    noteNaturalStartButtonPress(args[0], 'ui_button_press');
                }
            }
        },
    }));
    console.log('[unity_bridge] UnityEngine.UI.Button.Press @ ' + base.add(RVA.uiButtonPress));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowOnBtnClick), {
        onEnter(args) {
            if (args[1] && !args[1].isNull()) {
                launchShowOnBtnClickMethodInfo = args[1];
            }
        },
    }));
    console.log('[unity_bridge] LaunchShow.OnBtnClick probe @ ' + base.add(RVA.launchShowOnBtnClick));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowStartBtn0), {
        onEnter(args) {
            if (args[1] && !args[1].isNull()) {
                launchShowStartBtn0MethodInfo = args[1];
            }
            emit({
                type: 'natural_start_handler',
                schemaVersion: SCHEMA_VERSION,
                handler: 'LaunchShow.<Start>b__55_0',
                self: isLikelyIl2CppObject(args[0]) ? args[0].toString() : null,
                ts: Date.now(),
            });
            bootstrap.syntheticStartPressAttempted = true;
            bootstrap.mapStartRequested = true;
            stopAutoEnterMapPoller();
        },
    }));
    console.log('[unity_bridge] LaunchShow.<Start>b__55_0 probe @ ' + base.add(RVA.launchShowStartBtn0));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowStart), {
        onEnter(args) {
            if (isLikelyIl2CppObject(args[0])) {
                bootstrap.pendingLaunchShow = args[0];
            }
            if (bootstrap.launchShowStartAt === 0) {
                bootstrap.launchShowStartAt = Date.now();
                const btnStart = isLikelyIl2CppObject(args[0])
                    ? readStartButton(args[0])
                    : null;
                const btnShip = isLikelyIl2CppObject(args[0])
                    ? readShipButton(args[0])
                    : null;
                emit({
                    type: 'main_menu_launch_show_start',
                    schemaVersion: SCHEMA_VERSION,
                    self: isLikelyIl2CppObject(args[0]) ? args[0].toString() : null,
                    btnStartAtStart: isLikelyIl2CppObject(btnStart) ? btnStart.toString() : null,
                    btnShipAtStart: isLikelyIl2CppObject(btnShip) ? btnShip.toString() : null,
                    ts: Date.now(),
                });
            }
        },
        onLeave() {
            if (isLikelyIl2CppObject(bootstrap.pendingLaunchShow)) {
                onLaunchShowStartCompleted(bootstrap.pendingLaunchShow);
            }
        },
    }));
    console.log('[unity_bridge] LaunchShow.Start @ ' + base.add(RVA.launchShowStart));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowHangarReady), {
        onEnter() {
            if (bootstrap.hangarDataReadyAt === 0) {
                bootstrap.hangarDataReadyAt = Date.now();
                emit({
                    type: 'main_menu_hangar_ready',
                    schemaVersion: SCHEMA_VERSION,
                    ts: Date.now(),
                });
            }
        },
    }));
    console.log('[unity_bridge] LaunchShow.GetHangerPost complete @ ' + base.add(RVA.launchShowHangarReady));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowPreInit), {
        onEnter(args) {
            emit({
                type: 'main_menu_launch_show_pre_init',
                schemaVersion: SCHEMA_VERSION,
                self: isLikelyIl2CppObject(args[0]) ? args[0].toString() : null,
                ts: Date.now(),
            });
            captureLaunchShow(args[0], 'launch_show_pre_init');
        },
    }));
    console.log('[unity_bridge] LaunchShow.PreInit @ ' + base.add(RVA.launchShowPreInit));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowInit), {
        onEnter(args) {
            if (bootstrap.launchShowInitAt === 0) {
                bootstrap.launchShowInitAt = Date.now();
                emit({
                    type: 'main_menu_launch_show_init',
                    schemaVersion: SCHEMA_VERSION,
                    ts: Date.now(),
                });
            }
            captureLaunchShow(args[0], 'launch_show_init');
        },
    }));
    console.log('[unity_bridge] LaunchShow.Init @ ' + base.add(RVA.launchShowInit));

    trackInterceptor(Interceptor.attach(base.add(RVA.launchShowUpdateButton), {
        onEnter(args) {
            captureLaunchShow(args[0], 'launch_show_update_button');
            emit({
                type: 'main_menu_update_button',
                schemaVersion: SCHEMA_VERSION,
                ts: Date.now(),
            });
        },
    }));
    console.log('[unity_bridge] LaunchShow.UpdateButton @ ' + base.add(RVA.launchShowUpdateButton));

    const openWebLoginAddr = base.add(RVA.openWebLogin);
    trackInterceptor(Interceptor.attach(openWebLoginAddr, {
        onEnter(args) {
            captureGameLoadingManager(args[0], 'open_web_login');
            if (args[2] && !args[2].isNull()) {
                bootstrap.openWebLoginMethodInfo = args[2];
            }
            bootstrap.webLoginOpened = true;
            bootstrap.openWebLoginAt = Date.now();

            const url = readIl2CppString(args[1]);
            emit({
                type: 'open_web_login',
                schemaVersion: SCHEMA_VERSION,
                url: url,
                ts: Date.now(),
            });
            scheduleWebAutoLogin();
        },
        onLeave() {
            scheduleWebAutoLogin();
        },
    }));
    console.log('[unity_bridge] GameLoadingManager.OpenWebLogin attach (WebView loads naturally) @ ' + openWebLoginAddr);

    bootstrap.bootstrapHooksReady = true;
    emit({
        type: 'bootstrap_hooks_ready',
        schemaVersion: SCHEMA_VERSION,
        agentVersion: AGENT_VERSION,
        pointerSize: Process.pointerSize,
        ts: Date.now(),
    });

    scheduleGameLoadingManagerDiscovery();
    discoverGameLoadingManager('hooks_installed');

    setTimeout(function () {
        if (bootstrap.clientUpdateComplete) {
            return;
        }
        if (!bootstrap.clientUpdateStarted) {
            markClientUpdateComplete('forced_no_update_screen');
            discoverGameLoadingManager('forced_update_complete');
        }
    }, 30000);
}

function waitForGameAssembly(callback) {
    const existing = tryResolveGameAssembly();
    if (existing) {
        callback(existing);
        return;
    }

    gameAssemblyPollTimer = setInterval(function () {
        const mod = tryResolveGameAssembly();
        if (!mod) {
            return;
        }
        clearInterval(gameAssemblyPollTimer);
        gameAssemblyPollTimer = null;
        callback(mod);
    }, 250);
}

function scheduleMovementHooks() {
    if (movementHooksTimer !== null || bootstrap.movementHooksReady) {
        return;
    }

    movementHooksTimer = setTimeout(function () {
        movementHooksTimer = null;
        try {
            installMovementPhase();
        } catch (e) {
            emit({ type: 'warn', message: 'movement_hooks: ' + e, ts: Date.now() });
            movementHooksTimer = setTimeout(function () {
                movementHooksTimer = null;
                scheduleMovementHooks();
            }, 3000);
        }
    }, 2000);
}

function installMovementPhase() {
    if (bootstrap.movementHooksReady) {
        return;
    }

    const ga = resolveGameAssembly();
    gameAssemblyBase = ga.base;
    initMoveNativeFunctions(ga.base);
    installMoveProbeHooks(ga.base);
    installNetworkProbeHooks(ga.base);
    installHeroMoveHook(ga.base);
    installMoveCommandHook(ga.base);
    installClickCompareHooks(ga.base);
    if (ENABLE_MAP_CLICK_COORD_PROBE) {
        installOperaClickHook(ga.base);
    } else {
        console.log('[unity_bridge] map_click coord probe disabled (Vector3 ABI)');
    }
    startPing();

    bootstrap.movementHooksReady = true;
    emit({
        type: 'movement_hooks_ready',
        schemaVersion: SCHEMA_VERSION,
        agentVersion: AGENT_VERSION,
        pid: Process.id,
        gameAssemblyBase: ga.base.toString(),
        ts: Date.now(),
    });
    emit({
        type: 'ready',
        schemaVersion: SCHEMA_VERSION,
        agentVersion: AGENT_VERSION,
        pid: Process.id,
        gameAssemblyBase: ga.base.toString(),
        ts: Date.now(),
    });
    console.log('[unity_bridge] movement hooks ready pid=' + Process.id);
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
    if (gameAssemblyPollTimer !== null) {
        clearInterval(gameAssemblyPollTimer);
        gameAssemblyPollTimer = null;
    }
    if (movementHooksTimer !== null) {
        clearTimeout(movementHooksTimer);
        movementHooksTimer = null;
    }
    if (proactiveInjectTimer !== null) {
        clearInterval(proactiveInjectTimer);
        proactiveInjectTimer = null;
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
    emit({
        type: 'agent_loaded',
        schemaVersion: SCHEMA_VERSION,
        agentVersion: AGENT_VERSION,
        pid: Process.id,
        ts: Date.now(),
    });

    waitForGameAssembly(function (ga) {
        gameAssemblyBase = ga.base;
        try {
            initLoginNativeFunctions(ga.base);
            installBootstrapHooks(ga.base);
            console.log('[unity_bridge] bootstrap hooks installed, base=' + ga.base);
        } catch (e) {
            emit({ type: 'bootstrap_error', message: String(e), ts: Date.now() });
            throw e;
        }
    });
}

rpc.exports = {
    getStatus: function () {
        return JSON.stringify({
            schemaVersion: SCHEMA_VERSION,
            agentVersion: AGENT_VERSION,
            ready: bootstrap.movementHooksReady,
            bootstrapHooksReady: bootstrap.bootstrapHooksReady,
            clientUpdateStarted: bootstrap.clientUpdateStarted,
            clientUpdateComplete: bootstrap.clientUpdateComplete,
            clientUpdateCompleteAt: bootstrap.clientUpdateCompleteAt,
            sessionInjected: bootstrap.sessionInjected,
            webLoginOpened: bootstrap.webLoginOpened,
            getPostSeen: bootstrap.getPostSeen,
            mainMenuUiShownAt: bootstrap.mainMenuUiShownAt,
            hangarDataReadyAt: bootstrap.hangarDataReadyAt,
            launchShowInitAt: bootstrap.launchShowInitAt,
            launchShowStarted: bootstrap.launchShowStarted,
            launchShowStartAt: bootstrap.launchShowStartAt,
            startButtonBoundAt: bootstrap.startButtonBoundAt,
            mapStartRequested: bootstrap.mapStartRequested,
            mapStartComplete: bootstrap.mapStartComplete,
            startButtonPressed: bootstrap.startButtonPressed,
            movementHooksReady: bootstrap.movementHooksReady,
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
            heroPos: bootstrap.movementHooksReady ? getHeroMapPosition() : null,
            netStats: bootstrap.movementHooksReady ? snapshotNetStats() : null,
            clickCompareStats: clickCompareStats,
            mapCenter: { x: MAP_CENTER_X, y: MAP_CENTER_Y },
        });
    },
    bootstrapSession: function (_dosid, _webGlJson, username, password) {
        bootstrap.username = username || null;
        bootstrap.password = password || null;
        bootstrap.webAutoLoginScheduled = false;
        discoverGameLoadingManager('bootstrap_session');
        scheduleWebAutoLogin(true);
        return JSON.stringify({
            accepted: true,
            clientUpdateComplete: bootstrap.clientUpdateComplete,
            sessionInjected: bootstrap.sessionInjected,
            webLoginOpened: bootstrap.webLoginOpened,
            getPostSeen: bootstrap.getPostSeen,
            hasCredentials: !!(bootstrap.username && bootstrap.password),
            hasGameLoadingManager: !!(bootstrap.pendingGameLoadingManager && !bootstrap.pendingGameLoadingManager.isNull()),
            pointerSize: Process.pointerSize,
        });
    },
    refreshSession: function (_dosid, _webGlJson, username, password) {
        bootstrap.username = username || null;
        bootstrap.password = password || null;
        bootstrap.sessionInjected = false;
        bootstrap.webAutoLoginScheduled = false;
        emit({
            type: 'session_refresh_requested',
            schemaVersion: SCHEMA_VERSION,
            hasCredentials: !!(bootstrap.username && bootstrap.password),
            ts: Date.now(),
        });
        scheduleWebAutoLogin(true);
        return JSON.stringify({
            accepted: true,
            sessionInjected: bootstrap.sessionInjected,
            webLoginOpened: bootstrap.webLoginOpened,
            hasCredentials: !!(bootstrap.username && bootstrap.password),
            hasGameLoadingManager: !!(bootstrap.pendingGameLoadingManager && !bootstrap.pendingGameLoadingManager.isNull()),
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
