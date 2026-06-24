'use strict';

/**
 * Трассировка полного ручного пути Unity-клиента (v1.1.102).
 *
 * Запуск:
 *   python unity_login_trace.py
 *   python unity_login_trace.py --pid <DarkOrbit.exe pid>
 *
 * Сценарий для записи:
 *   1. Запусти DarkOrbit.exe вручную (или через бота — без Frida bridge agent).
 *   2. Запусти этот скрипт.
 *   3. Пройди путь сам: логин → START → корабль → карта.
 *   4. Останови Ctrl+C — получишь login_trace_*.jsonl с цепочкой вызовов.
 */
const TRACE_VERSION = 'login-trace-2026-06-24-2';

const LAUNCH_SHOW_BTN_START_OFFSET = 0x44;
const GLM_LAUNCH_SHOW_OFFSET = 0x1C;

const IL2CPP_STRING_LENGTH_OFFSET = Process.pointerSize * 2;
const IL2CPP_STRING_CHARS_OFFSET = IL2CPP_STRING_LENGTH_OFFSET + 4;

const RVA = {
    checkUpdateStart: 0xEE1500,
    checkUpdateEnterGame: 0xEE1430,
    getPost: 0xF11300,
    enterGame: 0xF11170,
    startGame: 0xF12B80,
    openWebLogin: 0xF12140,
    updateWebData: 0xF13510,
    loadingUIShow: 0xF11A90,
    startPreload: 0xF13160,
    launchShowOnBtnClick: 0xEE5460,
    launchShowInit: 0xEE52C0,
    launchShowPreInit: 0xEE6010,
    launchShowStart: 0xEE6300,
    launchShowUpdateButton: 0xEE6B10,
    launchShowGetHangerPost: 0xEE5070,
    launchShowHangarReady: 0xEF2E60,
    launchShowStartBtn0: 0xEE6830,
    launchShowStartBtn1: 0xEE68A0,
    launchShowStartBtn3: 0xEE6900,
    launchShowStartBtn4: 0xEE69A0,
    launchShowStartBtn5: 0xEE6A40,
    launchShowStartBtn6: 0xEE6A70,
    launchShowStartBtn7: 0xEE6AA0,
    launchShowStartBtn2Static: 0xEF3010,
    uiButtonPress: 0x286AC40,
    uiButtonOnPointerClick: 0x286AB50,
};

const hooks = [];
let gameAssemblyBase = null;
let seq = 0;
let traceStartedAt = Date.now();
let lastPhaseAt = traceStartedAt;
let lastLaunchShow = null;
let lastGameLoadingManager = null;
let launchShowStartMs = 0;

function emit(phase, data) {
    const now = Date.now();
    const payload = {
        type: 'login_trace',
        seq: ++seq,
        phase: phase,
        ts: now,
        sinceStartMs: now - traceStartedAt,
        sincePrevMs: now - lastPhaseAt,
        pid: Process.id,
        pointerSize: Process.pointerSize,
        ...data,
    };
    lastPhaseAt = now;
    send(payload);
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
        return '<read_error:' + e + '>';
    }
}

function preview(text, maxLen) {
    if (text === null || text === undefined) {
        return null;
    }
    const s = String(text);
    if (s.length <= maxLen) {
        return s;
    }
    return s.slice(0, maxLen) + '...[' + s.length + ' bytes total]';
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

function emitStartClickSnapshot(pressedButton, source) {
    const btnStart = lastLaunchShow
        ? readFieldPointer(lastLaunchShow, LAUNCH_SHOW_BTN_START_OFFSET)
        : null;
    const glmLaunchShow = lastGameLoadingManager
        ? readFieldPointer(lastGameLoadingManager, GLM_LAUNCH_SHOW_OFFSET)
        : null;

    emit('start_click_snapshot', {
        source: source || 'ui_button_press',
        pressedButton: pressedButton ? pressedButton.toString() : null,
        launchShow: lastLaunchShow ? lastLaunchShow.toString() : null,
        btnStartField: btnStart ? btnStart.toString() : null,
        btnStartOffset: LAUNCH_SHOW_BTN_START_OFFSET,
        gameLoadingManager: lastGameLoadingManager ? lastGameLoadingManager.toString() : null,
        glmLaunchShowField: glmLaunchShow ? glmLaunchShow.toString() : null,
        glmLaunchShowOffset: GLM_LAUNCH_SHOW_OFFSET,
        buttonsMatch: !!(btnStart && pressedButton && btnStart.equals(pressedButton)),
        glmLaunchShowMatch: !!(glmLaunchShow && lastLaunchShow && glmLaunchShow.equals(lastLaunchShow)),
        msSinceLaunchShowStart: launchShowStartMs > 0 ? Date.now() - launchShowStartMs : null,
    });
}

function track(name, target, callbacks) {
    const interceptor = Interceptor.attach(target, callbacks);
    hooks.push(interceptor);
    console.log('[login_trace] ' + name + ' @ ' + target);
}

function trackLaunchShowHandler(name, rva, phase) {
    track(name, gameAssemblyBase.add(rva), {
        onEnter(args) {
            emit(phase, {
                handler: name,
                self: args[0] ? args[0].toString() : null,
            });
            if (phase === 'user_click_start_candidate') {
                emitStartClickSnapshot(null, name);
            }
        },
    });
}

function installTraceHooks(base) {
    gameAssemblyBase = base;

    track('CheckUpdateAndDownload.StartCheckAddressable', base.add(RVA.checkUpdateStart), {
        onEnter() {
            emit('client_update_started', {});
        },
    });

    track('CheckUpdateAndDownload.EnterGame', base.add(RVA.checkUpdateEnterGame), {
        onEnter() {
            emit('client_update_complete', {});
        },
    });

    track('GameLoadingManager.StartPreload', base.add(RVA.startPreload), {
        onEnter(args) {
            emit('start_preload', { self: args[0].toString() });
        },
    });

    track('GameLoadingManager.LoadingUIShow', base.add(RVA.loadingUIShow), {
        onEnter(args) {
            emit('loading_ui_show', { self: args[0].toString() });
        },
    });

    track('GameLoadingManager.GetPost', base.add(RVA.getPost), {
        onEnter(args) {
            const url = readIl2CppString(args[1]);
            emit('get_post', {
                self: args[0].toString(),
                url: url,
                isLoginApi: !!(url && url.indexOf('login.php') >= 0),
            });
        },
    });

    track('GameLoadingManager.OpenWebLogin', base.add(RVA.openWebLogin), {
        onEnter(args) {
            const url = readIl2CppString(args[1]);
            emit('open_web_login', {
                self: args[0].toString(),
                url: url || '',
            });
        },
    });

    track('GameLoadingManager.UpdateWebData', base.add(RVA.updateWebData), {
        onEnter(args) {
            lastGameLoadingManager = args[0];
            const webData = readIl2CppString(args[1]);
            emit('update_web_data', {
                self: args[0].toString(),
                webDataPreview: preview(webData, 2000),
                webDataLength: webData ? webData.length : 0,
                webDataFull: webData && webData.length <= 200_000 ? webData : undefined,
            });
        },
    });

    track('GameLoadingManager.EnterGame', base.add(RVA.enterGame), {
        onEnter(args) {
            emit('enter_game', { self: args[0].toString() });
        },
    });

    track('GameLoadingManager.StartGame', base.add(RVA.startGame), {
        onEnter(args) {
            emit('map_load_start_game', { self: args[0].toString() });
        },
    });

    // --- LaunchShow / главное меню ---
    track('LaunchShow.PreInit', base.add(RVA.launchShowPreInit), {
        onEnter(args) {
            emit('launch_show_pre_init', { self: args[0].toString() });
        },
    });

    track('LaunchShow.Init', base.add(RVA.launchShowInit), {
        onEnter(args) {
            emit('launch_show_init', { self: args[0].toString() });
        },
    });

    track('LaunchShow.Start', base.add(RVA.launchShowStart), {
        onEnter(args) {
            lastLaunchShow = args[0];
            launchShowStartMs = Date.now();
            const btnStart = readFieldPointer(args[0], LAUNCH_SHOW_BTN_START_OFFSET);
            emit('launch_show_start', {
                self: args[0].toString(),
                btnStartAtStart: btnStart ? btnStart.toString() : null,
            });
        },
    });

    track('LaunchShow.UpdateButton', base.add(RVA.launchShowUpdateButton), {
        onEnter(args) {
            emit('launch_show_update_button', { self: args[0].toString() });
        },
    });

    track('LaunchShow.GetHangerPost', base.add(RVA.launchShowGetHangerPost), {
        onEnter(args) {
            const url = readIl2CppString(args[1]);
            emit('launch_show_get_hanger_post', {
                self: args[0].toString(),
                url: url,
            });
        },
    });

    track('LaunchShow.GetHangerPost.complete', base.add(RVA.launchShowHangarReady), {
        onEnter() {
            emit('launch_show_hangar_ready', {});
        },
    });

    track('LaunchShow.OnBtnClick', base.add(RVA.launchShowOnBtnClick), {
        onEnter(args) {
            emit('user_click_ship', { self: args[0].toString() });
        },
    });

    // Обработчики кнопки START (lambda из LaunchShow.Start)
    trackLaunchShowHandler('LaunchShow.<Start>b__55_0', RVA.launchShowStartBtn0, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_1', RVA.launchShowStartBtn1, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_3', RVA.launchShowStartBtn3, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_4', RVA.launchShowStartBtn4, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_5', RVA.launchShowStartBtn5, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_6', RVA.launchShowStartBtn6, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<Start>b__55_7', RVA.launchShowStartBtn7, 'user_click_start_candidate');
    trackLaunchShowHandler('LaunchShow.<>c.<Start>b__55_2', RVA.launchShowStartBtn2Static, 'user_click_start_candidate');

    // Unity UI Button — все нажатия (может быть шумно; фильтруй по времени рядом с start_candidate)
    track('UnityEngine.UI.Button.Press', base.add(RVA.uiButtonPress), {
        onEnter(args) {
            emitStartClickSnapshot(args[0], 'ui_button_press');
            emit('ui_button_press', { button: args[0].toString() });
        },
    });

    track('UnityEngine.UI.Button.OnPointerClick', base.add(RVA.uiButtonOnPointerClick), {
        onEnter(args) {
            emit('ui_button_pointer_click', { button: args[0].toString() });
        },
    });

    emit('trace_ready', {
        traceVersion: TRACE_VERSION,
        gameAssemblyBase: base.toString(),
        hookCount: hooks.length,
    });
}

rpc.exports = {
    getTraceInfo: function () {
        return JSON.stringify({
            traceVersion: TRACE_VERSION,
            pid: Process.id,
            gameAssemblyBase: gameAssemblyBase ? gameAssemblyBase.toString() : null,
            hookCount: hooks.length,
            eventCount: seq,
        });
    },
    stop: function () {
        while (hooks.length > 0) {
            try {
                hooks.pop().detach();
            } catch (e) {
                // ignore
            }
        }
        emit('trace_stopped', {});
        return true;
    },
};

emit('trace_loaded', { traceVersion: TRACE_VERSION, pid: Process.id });

function waitForGameAssembly(callback) {
    const existing = Process.findModuleByName('GameAssembly.dll');
    if (existing) {
        callback(existing);
        return;
    }
    const timer = setInterval(function () {
        const mod = Process.findModuleByName('GameAssembly.dll');
        if (!mod) {
            return;
        }
        clearInterval(timer);
        callback(mod);
    }, 250);
}

waitForGameAssembly(function (mod) {
    try {
        installTraceHooks(mod.base);
    } catch (e) {
        emit('trace_error', { message: String(e) });
        throw e;
    }
});
