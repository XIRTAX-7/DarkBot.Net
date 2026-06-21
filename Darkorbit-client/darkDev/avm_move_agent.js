/**
 * Frida game API agent: movement, select, collect, useItem, refine, callMethod via Flash AVM.
 * Based on Alph4rd/darkorbit_packet_dumper — enterFrame action queue + RPC.
 *
 * Offsets mirror Java BotInstaller (main + 504 → screenManager, +200 → eventManager).
 * Method index defaults to 10 (Kekka checkGotoMethod signature slot).
 */
'use strict';

var injectedHeapRanges = /*INJECTED_HEAP_RANGES*/;
var warnedNoRanges = false;

var patterns = {
    darkbot: 'ff ff 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00'
};

var flash_lib = Process.enumerateModules().find(function (el) {
    return el.name.indexOf('pepflash') >= 0 || el.name.indexOf('Flash.ocx') >= 0;
});

var getproperty_f = null;
var createstring_f = null;
var setproperty_f = null;
var newarray_f = null;

var avm = { core: null, constant_pool: null, abc_env: null, toplevel: null };
var offsets = {};

var gameState = {
    ready: false,
    error: null,
    lastScanNote: null,
    mainAddress: null,
    screenManager: null,
    eventManager: null,
    gotoMethodIndex: 10,
    gotoMethodName: null,
    gotoMethodInfo: null,
    gotoMethodParams: 4,
    notificationMethodIndex: null,
    notificationMethodName: null,
    useItemMethodIndex: 19,
    mainApplicationAddress: null,
    heroStatic: null,
    connectionManager: null,
    lastPacketActivityMs: 0,
    flashHookInstalled: false,
    flashHookTarget: null,
    flashModule: flash_lib ? flash_lib.name : null,
    mapAddress: null,
    mapId: 0,
    mapWidth: 0,
    mapHeight: 0,
    heroId: 0,
    heroX: 0,
    heroY: 0,
    heroHp: 0,
    heroMaxHp: 0,
    entityCount: 0
};

var hook_queue = [];
var verify_jit_hooked = false;
var packet_handler_method = null;
var flash_hook_installed = false;
var flash_hook_labels = [];
var hooked_method_infos = {};

var actionQueue = [];
var actionSeq = 0;
var lastActionResult = null;
var lastGameStateRefreshMs = 0;

var SELECT_MAP_ASSET = 'MapAssetNotificationTRY_TO_SELECT_MAPASSET';

if (!flash_lib) {
    gameState.error = 'Flash module (pepflash / Flash.ocx) not found in process';
} else if (Process.platform === 'windows') {
    offsets = {
        method_list: 0x148,
        ns_list: 0x188,
        method_names: 0x158,
        mn_list: 0xc8,
        mn_count: 0x80
    };
    if (flash_lib.name.indexOf('Flash.ocx') >= 0) {
        patterns.getproperty = '40 53 55 56 57 41 56 48 83 ec 30 48 8b 05 ?? ?? ?? ?? 48 33 c4 48 89 44 24 28 48 8b f2 49 8b f9';
        patterns.createstring = '40 53 55 57 41 55 41 57 48 83 ec 60 48 8b 05 ?? ?? ?? ?? 48 33 c4 48 89 44 24 40';
        patterns.setproperty = '48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 48 89 7c 24 20 41 56 48 83 ec 30 48 8b 5c 24 60 48 8b ea';
        patterns.newarray = '48 89 5c 24 08 57 48 83 ec 20 48 8b 41 18 8b da ba 09 00 00 00 49 8b f8 48 8b 48 08';
        offsets.ns_list = 0x180;
    } else {
        patterns.getproperty = '48 89 5c 24 08 48 89 6c 24 10 56 57 41 56 48 83 ec 20 48 8b f2 4d 8b f1 49 8b 51 28 49 8b d8';
        patterns.createstring = '40 53 55 57 41 55 41 56 48 83 ec 50 33 ed 45 8b f1 41 8b d8 48 8b fa 4c 8b e9 48 85 d2';
        patterns.setproperty = '48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 48 89 7c 24 20 41 56 48 83 ec 30 48 8b 5c 24 60 48 8b ea 49 8b f9 49 8b f0 4c 8b f1 48 8b 53 28';
        patterns.newarray = '48 89 5c 24 08 57 48 83 ec 20 48 8b 41 18 8b da ba 09 00 00 00';
        patterns.verifyjit = '48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 81 ec 00 03 00 00 48 8d 41 30';
    }
} else if (Process.platform === 'linux') {
    offsets = {
        method_list: 0x180,
        ns_list: 0x190,
        method_names: 0x190,
        mn_list: 0xe8,
        mn_count: 0x98
    };
    patterns.getproperty = '48 89 5c 24 d8 48 89 6c 24 e0 48 89 d3 4c 89 64 24 e8 4c 89 6c 24 f0 49 89 f4 4c 89 74 24 f8 48 83 ec 38 48 8b 71 28 48 89 fd 49 89 cd e8 3e 60 fd ff';
    patterns.createstring = '41 57 41 56 41 55 49 89 fd 41 54 55 89 d5 53 48 89 f3 48 83 ec 68 48 85 f6';
    patterns.setproperty = '48 89 5c 24 e0 48 89 6c 24 e8 48 89 d3 4c 89 64 24 f0 4c 89 6c 24 f8 48 83 ec 38 49 89 f5 49 8b 70 28';
    patterns.newarray = '48 89 5c 24 f0 48 89 6c 24 f8 48 83 ec 18 48 8b 47 18 48 89 d3 89 f5 be 09 00 00 00';
} else {
    gameState.error = 'Unsupported OS: ' + Process.platform;
}

function removeKind(pointer) {
    return pointer.and(uint64(0x7).not());
}

var MIN_PTR = ptr('0x10000');
var MAX_PTR = ptr('0x00007fffffffffff');

function isPlausiblePtr(p) {
    if (p === null || p === undefined) return false;
    try {
        p = ptr(p);
        if (p.isNull()) return false;
        return p.compare(MIN_PTR) >= 0 && p.compare(MAX_PTR) <= 0;
    } catch (e) {
        return false;
    }
}

function safeReadPointer(base, offset) {
    try {
        if (!isPlausiblePtr(base)) return ptr(0);
        var value = ptr(base).add(offset).readPointer();
        return isPlausiblePtr(value) ? value : ptr(0);
    } catch (e) {
        return ptr(0);
    }
}

function resetAvm() {
    avm.core = null;
    avm.constant_pool = null;
    avm.abc_env = null;
    avm.toplevel = null;
}

function readAvmString(str_pointer, c) {
    c = c || 0;
    str_pointer = removeKind(str_pointer);
    if (str_pointer.equals(0)) return '';

    var flags = str_pointer.add(0x24).readU32();
    var size = str_pointer.add(0x20).readU32();
    var width = flags & 0x1;
    size <<= width;

    if (size < 0 || c > 1) return '';

    if ((flags & (2 << 1)) !== 0) {
        return readAvmString(removeKind(str_pointer.add(0x18).readPointer()), c + 1);
    }

    var str_addr = str_pointer.add(0x10).readPointer();
    if (width) return str_addr.readUtf16String(size);
    return str_addr.readCString(size);
}

function getMultiname(index) {
    var precomp_mn = avm.constant_pool.add(offsets.mn_list).readPointer();
    var precomp_mn_size = avm.constant_pool.add(offsets.mn_count).readU32();
    if (index < precomp_mn_size) return precomp_mn.add(0x18 + index * 0x18);
    return null;
}

function getMethodName(method_info, with_class_name) {
    var name_list = avm.constant_pool.add(offsets.method_names).readPointer();
    var method_id = method_info.add(0x40).readU32();
    var name_index = name_list.add(4 + method_id * 4).readInt();
    var declarer = method_info.add(0x20).readPointer();

    if (name_index < 0) {
        name_index = -name_index;
        var multiname = getMultiname(name_index);
        if (multiname !== 0 && multiname !== null) {
            var prefix = '';
            if (with_class_name && declarer.and(1) === 0 && declarer.compare(1) > 0) {
                prefix += readAvmString(declarer.and(~1).add(0x90).readPointer()) + '::';
            }
            return prefix + readAvmString(multiname.readPointer());
        }
        return '';
    }
    return '';
}

function getMethodParams(method_info) {
    var abc_info_ptr = method_info.add(0x38).readPointer();
    if (abc_info_ptr.equals(0)) return [];

    var ptr = abc_info_ptr;
    function readU32() {
        var data = new Uint8Array(ptr.readByteArray(5));
        var result = data[0];
        if (!(result & 0x80)) { ptr = ptr.add(1); return result; }
        result = (result & 0x7f) | (data[1] << 7);
        if (!(result & 0x4000)) { ptr = ptr.add(2); return result; }
        result = (result & 0x3fff) | (data[2] << 14);
        if (!(result & 0x200000)) { ptr = ptr.add(3); return result; }
        result = (result & 0x1fffff) | (data[3] << 21);
        if (!(result & 0x10000000)) { ptr = ptr.add(4); return result; }
        result = (result & 0x0fffffff) | (data[4] << 28);
        ptr = ptr.add(5);
        return result;
    }

    var param_count = readU32();
    readU32(); // ret type
    var params = [];
    for (var i = 0; i < param_count; i++) {
        params.push(getMultiname(readU32()));
    }
    return params;
}

function methodIsCompiled(method_info_ptr) {
    return ((method_info_ptr.add(0x60).readU32() >> 21) & 1) === 1;
}

function getClassName(script_obj) {
    script_obj = removeKind(script_obj);
    var vtable = script_obj.add(0x10).readPointer();
    var traits = vtable.add(0x28).readPointer();
    return readAvmString(traits.add(0x90).readPointer());
}

function numberToAtom(n) {
    n = Math.floor(n);
    return ptr(n).shl(3).or(6);
}

function boolToAtom(value) {
    return ptr(value ? 1 : 0).shl(3).or(5);
}

function nullToAtom() {
    return ptr(0).or(1);
}

function ptrToObjectAtom(addr) {
    if (!addr || addr === 0 || addr === '0') return nullToAtom();
    return removeKind(ptr(addr)).or(1);
}

function taggedIntegerToAtom(value) {
    value = Math.floor(value);
    return ptr(value).shl(3).or(6);
}

function atomFromArg(value) {
    if (value === null || value === undefined) return nullToAtom();
    if (typeof value === 'boolean') return boolToAtom(value);
    if (typeof value === 'number') {
        if (value > 0x10000 && (value & 7) === 0) return ptrToObjectAtom(value);
        return taggedIntegerToAtom(value);
    }
    if (typeof value === 'string') {
        if (value.indexOf('0x') === 0) return ptrToObjectAtom(value);
        var num = parseInt(value, 10);
        if (!isNaN(num) && String(num) === value) return taggedIntegerToAtom(num);
        return createAvmString(value).or(2);
    }
    return ptr(value);
}

function createAvmString(str) {
    if (!createstring_f || !avm.core) throw new Error('createstring not ready');
    var string_buf = Memory.allocUtf8String(str);
    return createstring_f(avm.core, string_buf, -1, 0, 0, 0);
}

function createAvmArray(elements) {
    if (!newarray_f || !avm.core) throw new Error('newarray not ready');
    var array_args = Memory.alloc(elements.length * 8);
    for (var i = 0; i < elements.length; i++) {
        var atom = elements[i];
        if (!(atom instanceof NativePointer)) atom = ptr(atom);
        array_args.add(i * 8).writePointer(atom);
    }
    return newarray_f(avm.core, elements.length, array_args);
}

function findMethodByName(objectPtr, namePattern, minParams, maxParams) {
    minParams = minParams === undefined ? 0 : minParams;
    maxParams = maxParams === undefined ? 16 : maxParams;
    var best = null;

    for (var i = 0; i < 64; i++) {
        var info = getMethodInfoAtIndex(objectPtr, i);
        if (info.isNull()) continue;
        var name = getMethodName(info, true);
        if (!namePattern.test(name)) continue;
        var params = getMethodParams(info).length;
        if (params < minParams || params > maxParams) continue;
        var entry = {
            index: i,
            name: name,
            params: params,
            compiled: methodIsCompiled(info)
        };
        if (!best || entry.compiled) best = entry;
    }
    return best;
}

function resolveNotificationMethod() {
    if (gameState.notificationMethodIndex !== null && gameState.notificationMethodIndex !== undefined) {
        return {
            index: gameState.notificationMethodIndex,
            name: gameState.notificationMethodName || 'cached'
        };
    }
    var found = findMethodByName(ptr(gameState.screenManager), /notification/i, 1, 3);
    if (!found) {
        found = findMethodByName(ptr(gameState.screenManager), /dispatch/i, 1, 3);
    }
    if (found) {
        gameState.notificationMethodIndex = found.index;
        gameState.notificationMethodName = found.name;
    }
    return found;
}

function getMethodEnvAtIndex(object, index) {
    var obj = removeKind(object);
    var vtable = safeReadPointer(obj, 0x10);
    if (vtable.isNull()) return ptr(0);
    return safeReadPointer(vtable, 0x78 + index * 8);
}

function getMethodInfoAtIndex(object, index) {
    var env = getMethodEnvAtIndex(object, index);
    if (env.isNull()) return ptr(0);
    return safeReadPointer(env, 0x10);
}

function hookLater(method_info, callback, options) {
    if (method_info.isNull()) return;
    options = options || {};
    hook_queue.push({
        method: method_info,
        handler: callback,
        intercept: options.intercept === true
    });
}

function installVerifyJitHook() {
    if (verify_jit_hooked || !flash_lib || !patterns.verifyjit) return;

    Memory.scan(flash_lib.base, flash_lib.size, patterns.verifyjit, {
        onMatch: function (addr) {
            if (verify_jit_hooked) return;
            verify_jit_hooked = true;
            console.log('[avm_move] verifyJit @ ' + ptr(addr));
            Interceptor.attach(ptr(addr), {
                onEnter: function (args) {
                    this.method = ptr(args[1]);
                },
                onLeave: function () {
                    var idx = hook_queue.findIndex(function (h) {
                        return h.method.equals(this.method);
                    }.bind(this));
                    if (idx < 0) return;
                    var hook = hook_queue.splice(idx, 1)[0];
                    try {
                        if (hook.intercept) {
                            var code = this.method.add(0x8).readPointer();
                            Interceptor.attach(code, { onEnter: hook.handler });
                            if (packet_handler_method && this.method.equals(packet_handler_method)) {
                                flash_hook_installed = true;
                                gameState.flashHookInstalled = true;
                            }
                            var key = this.method.toString();
                            if (!hooked_method_infos[key]) {
                                hooked_method_infos[key] = 'jit';
                                if (flash_hook_labels.indexOf('jit') < 0) {
                                    flash_hook_labels.push('jit');
                                }
                                gameState.flashHookTarget = flash_hook_labels.join(' + ');
                            }
                            console.log('[avm_move] Flash thread hook installed (after JIT)');
                        } else {
                            hook.handler();
                        }
                    } catch (e) {
                        send({ type: 'warn', message: 'verifyJit callback: ' + e });
                    }
                }
            });
        },
        onError: function () {},
        onComplete: function () {}
    });
}

function waitForMethodCompiled(method_info, label, timeoutMs) {
    timeoutMs = timeoutMs || 12000;
    if (method_info.isNull()) return false;
    if (methodIsCompiled(method_info)) return true;

    var start = Date.now();
    while (!methodIsCompiled(method_info) && (Date.now() - start) < timeoutMs) {
        Thread.sleep(0.05);
    }
    return methodIsCompiled(method_info);
}

function invokeObjectMethod(obj_ptr, index, args, options) {
    options = options || {};
    var waitMs = options.waitMs || 0;
    var allowUncompiled = options.allowUncompiled !== false;

    var obj = removeKind(ptr(obj_ptr));
    var env = getMethodEnvAtIndex(obj, index);
    if (env.isNull()) throw new Error('Method env null at index ' + index);

    var method_info = safeReadPointer(env, 0x10);
    if (method_info.isNull()) throw new Error('Method info null at index ' + index);

    var methodName = getMethodName(method_info, true);

    function attempt() {
        var codePtr = safeReadPointer(env, 0x8);
        if (codePtr.isNull()) throw new Error('Method code pointer null: ' + methodName);

        // argc is uint64 in AVM invoke (see packet_dumper getPacketIdFromObj).
        var method_ptr = new NativeFunction(codePtr, 'int64', ['pointer', 'uint64', 'pointer']);
        var args_buf = Memory.alloc((args.length + 1) * 8);
        args_buf.writePointer(removeKind(ptr(obj_ptr)));

        for (var i = 0; i < args.length; i++) {
            var atom = args[i];
            if (!(atom instanceof NativePointer)) {
                atom = ptr(atom);
            }
            args_buf.add((i + 1) * 8).writePointer(atom);
        }

        method_ptr(env, args.length, args_buf);
        return { methodName: methodName, index: index, compiled: methodIsCompiled(method_info), argCount: args.length };
    }

    if (!methodIsCompiled(method_info)) {
        if (!allowUncompiled) {
            waitForMethodCompiled(method_info, methodName, waitMs || 3000);
        } else if (waitMs > 0) {
            waitForMethodCompiled(method_info, methodName, waitMs);
        }
    }

    try {
        return attempt();
    } catch (firstError) {
        if (!methodIsCompiled(method_info) && waitMs > 0) {
            waitForMethodCompiled(method_info, methodName, waitMs);
            return attempt();
        }
        throw firstError;
    }
}

function buildMoveCandidates(object) {
    var seen = {};
    var list = [];

    function add(index, reason) {
        if (seen[index]) return;
        seen[index] = true;
        var info = getMethodInfoAtIndex(object, index);
        if (info.isNull()) return;
        list.push({
            index: index,
            name: getMethodName(info, true),
            params: getMethodParams(info).length,
            compiled: methodIsCompiled(info),
            reason: reason
        });
    }

    // KekkaPlayer / Java DarkBot use method index 10 on eventManager (screenManager+200).
    add(10, 'kekka_goto_slot');

    var obj = removeKind(object);
    var vtable = safeReadPointer(obj, 0x10);
    if (!vtable.isNull()) {
        for (var i = 0; i < 48; i++) {
            try {
                var env = safeReadPointer(vtable, 0x78 + i * 8);
                if (env.isNull()) continue;
                var method_info = safeReadPointer(env, 0x10);
                if (method_info.isNull()) continue;
                var name = getMethodName(method_info, true);
                var params = getMethodParams(method_info);
                var lower = name.toLowerCase();
                if (!methodIsCompiled(method_info)) continue;
                if (params < 2 || params > 4) continue;
                if (lower.indexOf('goto') >= 0 || lower.indexOf('move') >= 0 || lower.indexOf('hero') >= 0) {
                    add(i, 'compiled_move_api');
                }
            } catch (e) {
                continue;
            }
        }
    }

    if (gameState.gotoMethodIndex !== undefined && gameState.gotoMethodIndex !== 10) {
        add(gameState.gotoMethodIndex, 'discovered');
    }

    return list;
}

function callObjectMethod(obj_ptr, index, args) {
    return invokeObjectMethod(obj_ptr, index, args, { waitMs: 0, allowUncompiled: true });
}

function iterateConstantPoolMethods(callback) {
    if (!avm.constant_pool) return false;

    var method_list = safeReadPointer(avm.constant_pool, offsets.method_list);
    if (method_list.isNull()) return false;

    var method_count = avm.constant_pool.add(offsets.method_list + 8).readU32();
    if (!method_count || method_count > 500000) return false;

    for (var i = 0; i < method_count; i++) {
        var method_info = safeReadPointer(method_list, 0x10 + i * 8);
        if (method_info.isNull()) continue;
        if (callback(method_info) === true) return true;
    }
    return false;
}

function findTickHookMethods() {
    var rules = [
        { test: function (s) { return s === 'enterFrame'; }, score: 100, label: 'enterFrame' },
        { test: function (s) { return s === 'renderFrame'; }, score: 95, label: 'renderFrame' },
        { test: function (s) { return s === 'handleEnterFrame'; }, score: 90, label: 'handleEnterFrame' },
        { test: function (s) { return s === 'onEnterFrame'; }, score: 85, label: 'onEnterFrame' },
        { test: function (s) { return s === 'updateDisplayList'; }, score: 80, label: 'updateDisplayList' }
    ];
    var candidates = [];

    iterateConstantPoolMethods(function (method_info) {
        if (!methodIsCompiled(method_info)) return false;
        var shortName = getMethodName(method_info, false);
        var fullName = getMethodName(method_info, true);
        for (var ri = 0; ri < rules.length; ri++) {
            var rule = rules[ri];
            if (rule.test(shortName) || rule.test(fullName.split('::').pop())) {
                candidates.push({
                    method_info: method_info,
                    score: rule.score,
                    label: rule.label + ' (' + fullName + ')'
                });
                break;
            }
        }
        return false;
    });

    candidates.sort(function (a, b) { return b.score - a.score; });
    return candidates;
}

function installTickHooks() {
    var candidates = findTickHookMethods();
    var seenCode = {};
    var attached = 0;
    var maxHooks = 2;

    for (var ci = 0; ci < candidates.length && attached < maxHooks; ci++) {
        var cand = candidates[ci];
        try {
            var code = cand.method_info.add(0x8).readPointer();
            var codeKey = code.toString();
            if (seenCode[codeKey]) continue;
            seenCode[codeKey] = true;
            if (attachFlashHook(cand.method_info, 'tick:' + cand.label)) attached++;
        } catch (e) {
            continue;
        }
    }
    return attached;
}

function findPacketHandlerMethods() {
    var list = [];
    iterateConstantPoolMethods(function (method_info) {
        var full_name = getMethodName(method_info, true);
        if (full_name.indexOf('::execute') === full_name.length - 9 &&
            (full_name.indexOf('$::') >= 0 || full_name.toLowerCase().indexOf('packet') >= 0)) {
            list.push({ method: method_info, name: full_name });
        }
        return false;
    });
    return list;
}

function findPacketHandlerMethod() {
    var list = findPacketHandlerMethods();
    return list.length ? list[0].method : ptr(0);
}

function findPacketSenderMethod() {
    var found = ptr(0);
    iterateConstantPoolMethods(function (method_info) {
        if (getMethodName(method_info) !== 'sendMessage') return false;
        var params = getMethodParams(method_info);
        if (params.length !== 1 || !params[0]) return false;
        var multiname = params[0];
        if (multiname.isNull()) return false;
        var paramName = readAvmString(multiname.readPointer());
        if (paramName === 'IModule') {
            found = method_info;
            return true;
        }
        return false;
    });
    return found;
}

function attachFlashHook(method_info, label) {
    if (method_info.isNull()) return false;

    var key = method_info.toString();
    if (hooked_method_infos[key]) return false;

    function onFlashActivity() {
        gameState.lastPacketActivityMs = Date.now();
        processPendingActions();
    }

    function markHooked() {
        hooked_method_infos[key] = label;
        flash_hook_installed = true;
        gameState.flashHookInstalled = true;
        if (flash_hook_labels.indexOf(label) < 0) {
            flash_hook_labels.push(label);
        }
        gameState.flashHookTarget = flash_hook_labels.join(' + ');
    }

    if (methodIsCompiled(method_info)) {
        try {
            Interceptor.attach(method_info.add(0x8).readPointer(), { onEnter: onFlashActivity });
            markHooked();
            console.log('[avm_move] Flash thread hook installed: ' + label);
            return true;
        } catch (e) {
            send({ type: 'warn', message: 'Hook attach failed (' + label + '): ' + e });
            return false;
        }
    }

    console.log('[avm_move] Waiting for JIT: ' + label);
    hookLater(method_info, onFlashActivity, { intercept: true });
    if (flash_hook_labels.indexOf(label + ' (pending JIT)') < 0) {
        flash_hook_labels.push(label + ' (pending JIT)');
    }
    gameState.flashHookTarget = flash_hook_labels.join(' + ');
    return true;
}

function installFlashThreadHook() {
    if (!avm.constant_pool) return;

    var attached = installTickHooks();

    var handlers = findPacketHandlerMethods();
    for (var hi = 0; hi < handlers.length; hi++) {
        var h = handlers[hi];
        if (hi === 0 && (packet_handler_method === null || packet_handler_method.isNull())) {
            packet_handler_method = h.method;
        }
        if (attachFlashHook(h.method, 'recv:' + h.name.split('::').pop())) attached++;
    }

    var sender = findPacketSenderMethod();
    if (!sender.isNull()) {
        if (packet_handler_method === null || packet_handler_method.isNull()) {
            packet_handler_method = sender;
        }
        if (attachFlashHook(sender, 'sendMessage')) attached++;
    }

    if (!attached && !gameState.flashHookTarget) {
        send({ type: 'warn', message: 'No flash hook target found — stay on map and retry' });
    } else if (attached) {
        console.log('[avm_move] Hooks installed: ' + gameState.flashHookTarget);
    }
}

function buildMoveArgs(x, y, paramCount, collectableAdr) {
    var xAtom = numberToAtom(x);
    var yAtom = numberToAtom(y);
    var collectAtom = collectableAdr ? ptrToObjectAtom(collectableAdr) : numberToAtom(0);
    var falseAtom = boolToAtom(false);
    var nullAtom = nullToAtom();

    if (paramCount === 2) return [xAtom, yAtom];
    if (paramCount === 3) return [xAtom, yAtom, collectAtom];
    return [xAtom, yAtom, collectAtom, nullAtom];
}

function executeMoveOnEventManager(x, y, collectableAdr) {
    var target = ptr(gameState.eventManager);
    var index = gameState.gotoMethodIndex || 10;
    var paramCount = gameState.gotoMethodParams || 4;
    var args = buildMoveArgs(x, y, paramCount, collectableAdr || 0);

    return invokeObjectMethod(target, index, args, {
        waitMs: 0,
        allowUncompiled: false
    });
}

function executeSelectEntity(taggedArgs) {
    var method = resolveNotificationMethod();
    if (!method) throw new Error('sendNotification method not found on screenManager');

    var atoms = [];
    for (var i = 0; i < taggedArgs.length; i++) {
        atoms.push(taggedIntegerToAtom(taggedArgs[i]));
    }

    var args;
    if (method.params >= 2) {
        args = [createAvmString(SELECT_MAP_ASSET).or(2), createAvmArray(atoms)];
    } else {
        args = [createAvmString(SELECT_MAP_ASSET).or(2)];
    }

    return invokeObjectMethod(ptr(gameState.screenManager), method.index, args, {
        waitMs: 0,
        allowUncompiled: false
    });
}

function executeUseItem(itemId, methodIndex, extraArgs) {
    var index = methodIndex || gameState.useItemMethodIndex || 19;
    var args = [createAvmString(String(itemId)).or(2)];
    for (var i = 0; i < extraArgs.length; i++) {
        args.push(ptrToObjectAtom(extraArgs[i]));
    }

    return invokeObjectMethod(ptr(gameState.screenManager), index, args, {
        waitMs: 0,
        allowUncompiled: false
    });
}

function executeRefine(refineUtilAddress, oreId, amount, methodIndex) {
    var target = ptr(refineUtilAddress);
    var index = methodIndex;
    if (index === undefined || index === null) {
        var found = findMethodByName(target, /refine/i, 1, 3);
        if (!found) throw new Error('refine method not found on refineUtil');
        index = found.index;
    }

    return invokeObjectMethod(target, index, [numberToAtom(oreId), numberToAtom(amount)], {
        waitMs: 0,
        allowUncompiled: false
    });
}

function executeGenericInvoke(objectPtr, methodIndex, rawArgs) {
    var args = [];
    for (var i = 0; i < rawArgs.length; i++) {
        args.push(atomFromArg(rawArgs[i]));
    }
    return invokeObjectMethod(ptr(objectPtr), methodIndex, args, {
        waitMs: 0,
        allowUncompiled: false
    });
}

function processPendingActions() {
    if (!actionQueue.length || !gameState.ready) return;

    var item = actionQueue[actionQueue.length - 1];
    actionQueue = [];
    try {
        var result;
        if (item.type === 'move') {
            result = executeMoveOnEventManager(item.x, item.y, item.collectableAdr || 0);
        } else if (item.type === 'select') {
            result = executeSelectEntity(item.args);
        } else if (item.type === 'useItem') {
            result = executeUseItem(item.itemId, item.methodIndex, item.args || []);
        } else if (item.type === 'refine') {
            result = executeRefine(item.refineUtilAddress, item.oreId, item.amount, item.methodIndex);
        } else if (item.type === 'invoke') {
            result = executeGenericInvoke(item.objectPtr, item.methodIndex, item.args || []);
        } else {
            throw new Error('Unknown action type: ' + item.type);
        }

        lastActionResult = {
            seq: item.seq,
            ok: true,
            type: item.type,
            method: result.methodName,
            methodIndex: result.index,
            compiled: result.compiled,
            args: result.argCount
        };
    } catch (e) {
        lastActionResult = {
            seq: item.seq,
            ok: false,
            type: item.type,
            error: String(e)
        };
    }
}

function waitActionResult(seq, timeoutMs) {
    timeoutMs = timeoutMs || 5000;
    var start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (lastActionResult && lastActionResult.seq === seq) {
            return lastActionResult;
        }
        Thread.sleep(0.02);
    }
    return {
        ok: false,
        pending: true,
        error: 'Timeout waiting for flash thread. Hooks: ' + (gameState.flashHookTarget || 'none'),
        seq: seq,
        queued: actionQueue.length
    };
}

function enqueueAction(action) {
    if (!gameState.ready) {
        return { ok: false, error: gameState.error || gameState.lastScanNote || 'AVM not ready — stay on the game map' };
    }

    if (!gameState.flashHookTarget) {
        installFlashThreadHook();
    }
    if (!gameState.flashHookTarget) {
        return { ok: false, error: 'No flash hook target in ABC — stay on map and retry' };
    }

    var seq = ++actionSeq;
    lastActionResult = null;
    action.seq = seq;
    actionQueue.push(action);
    return waitActionResult(seq, 5000);
}

function queueMove(x, y, collectableAdr) {
    return enqueueAction({ type: 'move', x: x, y: y, collectableAdr: collectableAdr || 0 });
}

function queueSelect(args) {
    return enqueueAction({ type: 'select', args: args });
}

function queueUseItem(itemId, methodIndex, args) {
    return enqueueAction({ type: 'useItem', itemId: itemId, methodIndex: methodIndex, args: args || [] });
}

function queueRefine(refineUtilAddress, oreId, amount, methodIndex) {
    return enqueueAction({
        type: 'refine',
        refineUtilAddress: refineUtilAddress,
        oreId: oreId,
        amount: amount,
        methodIndex: methodIndex
    });
}

function queueInvoke(objectPtr, methodIndex, args) {
    return enqueueAction({
        type: 'invoke',
        objectPtr: objectPtr,
        methodIndex: methodIndex,
        args: args || []
    });
}

function findGotoMethodIndex(object) {
    var info10 = getMethodInfoAtIndex(object, 10);
    if (!info10.isNull()) {
        return { index: 10, name: getMethodName(info10, true) };
    }
    var candidates = buildMoveCandidates(object);
    if (candidates.length > 0) {
        return { index: candidates[0].index, name: candidates[0].name };
    }
    return { index: 10, name: '(default index 10)' };
}

function readBindableIntAt(object, fieldOffset) {
    var bindable = safeReadPointer(object, fieldOffset);
    if (bindable.isNull()) {
        return 0;
    }
    return Math.floor(bindable.add(0x38).readDouble());
}

function refreshMapState() {
    gameState.mapAddress = null;
    gameState.mapId = 0;
    gameState.mapWidth = 0;
    gameState.mapHeight = 0;
    gameState.entityCount = 0;

    if (!gameState.ready || !gameState.screenManager) {
        return;
    }

    try {
        var screenManager = removeKind(ptr(gameState.screenManager));
        var mapAddr = safeReadPointer(screenManager, 256);
        if (mapAddr.isNull()) {
            return;
        }

        gameState.mapAddress = mapAddr.toString();
        gameState.mapWidth = mapAddr.add(76).readS32();
        gameState.mapHeight = mapAddr.add(80).readS32();
        gameState.mapId = mapAddr.add(84).readS32();

        var entitiesHeader = safeReadPointer(mapAddr, 40);
        if (!entitiesHeader.isNull()) {
            var count = entitiesHeader.add(0x18).readS32();
            if (count >= 0 && count < 10000) {
                gameState.entityCount = count;
            }
        }
    } catch (e) {
        gameState.mapAddress = null;
        gameState.mapId = 0;
        gameState.mapWidth = 0;
        gameState.mapHeight = 0;
        gameState.entityCount = 0;
    }
}

function refreshHeroState() {
    gameState.heroId = 0;
    gameState.heroX = 0;
    gameState.heroY = 0;
    gameState.heroHp = 0;
    gameState.heroMaxHp = 0;

    if (!gameState.ready || !gameState.screenManager) {
        return;
    }

    try {
        // Java HeroManager.tick: address = readLong(screenManager + 240) each frame.
        var screenManager = removeKind(ptr(gameState.screenManager));
        var hero = safeReadPointer(screenManager, 240);
        if (hero.isNull()) {
            gameState.heroStatic = null;
            return;
        }

        gameState.heroStatic = hero.toString();

        gameState.heroId = hero.add(56).readS32();
        if (gameState.heroId <= 0) {
            gameState.heroId = 0;
            return;
        }

        var location = safeReadPointer(hero, 64);
        if (!location.isNull()) {
            gameState.heroX = location.add(32).readDouble();
            gameState.heroY = location.add(40).readDouble();
        }

        var health = safeReadPointer(hero, 184);
        if (!health.isNull()) {
            gameState.heroHp = readBindableIntAt(health, 48);
            gameState.heroMaxHp = readBindableIntAt(health, 56);
        }
    } catch (e) {
        gameState.heroId = 0;
        gameState.heroX = 0;
        gameState.heroY = 0;
        gameState.heroHp = 0;
        gameState.heroMaxHp = 0;
    }
}

function refreshGameState() {
    refreshMapState();
    refreshHeroState();
}

function resolveGamePointers(main_address, main_application_base) {
    var mainPtr = removeKind(main_address);
    var screenManager = safeReadPointer(mainPtr, 504);
    if (screenManager.isNull()) {
        throw new Error('screenManager is null (main+504)');
    }

    var eventManager = safeReadPointer(screenManager, 200);
    if (eventManager.isNull()) {
        throw new Error('eventManager is null (screenManager+200)');
    }

    var gotoInfo = findGotoMethodIndex(eventManager);
    var gotoMethodInfo = getMethodInfoAtIndex(eventManager, gotoInfo.index);
    var gotoParams = gotoMethodInfo.isNull() ? 4 : getMethodParams(gotoMethodInfo).length;

    gameState.mainAddress = main_address.toString();
    gameState.mainApplicationAddress = main_application_base
        ? main_application_base.toString()
        : mainPtr.toString();
    gameState.screenManager = screenManager.toString();
    gameState.eventManager = eventManager.toString();
    gameState.heroStatic = safeReadPointer(screenManager, 240).toString();
    gameState.connectionManager = safeReadPointer(mainPtr, 560).toString();
    gameState.gotoMethodIndex = gotoInfo.index;
    gameState.gotoMethodName = gotoInfo.name;
    gameState.gotoMethodInfo = gotoMethodInfo.isNull() ? null : gotoMethodInfo.toString();
    gameState.gotoMethodParams = gotoParams;
    gameState.ready = true;
    gameState.error = null;
    gameState.lastScanNote = null;
    refreshGameState();

    if (!gotoMethodInfo.isNull() && !methodIsCompiled(gotoMethodInfo)) {
        hookLater(gotoMethodInfo, function () {
            send({ type: 'method_compiled', name: gameState.gotoMethodName });
            console.log('[avm_move] JIT compiled: ' + gameState.gotoMethodName);
        });
    }

    try {
        installFlashThreadHook();
    } catch (hookErr) {
        send({ type: 'warn', message: 'installFlashThreadHook: ' + hookErr });
    }

    send({ type: 'ready', state: gameState });
    console.log('[avm_move] Ready — screenManager=' + screenManager + ' goto=' + gotoInfo.name + '@' + gotoInfo.index);
}

function tryInitFromPattern(matchAddr) {
    if (gameState.ready || avm.toplevel !== null) {
        return false;
    }

    try {
        var base = ptr(matchAddr).sub(226);
        var main_address = safeReadPointer(base, 0x540);
        if (main_address.isNull()) {
            return false;
        }

        var vtable = safeReadPointer(main_address, 0x10);
        if (vtable.isNull()) {
            return false;
        }

        var traits = safeReadPointer(vtable, 0x28);
        var toplevel = safeReadPointer(vtable, 0x8);
        var vtable_init = safeReadPointer(vtable, 0x10);
        if (traits.isNull() || toplevel.isNull() || vtable_init.isNull()) {
            return false;
        }

        var vtable_scope = safeReadPointer(vtable_init, 0x18);
        if (vtable_scope.isNull()) {
            return false;
        }

        var abc_env = safeReadPointer(vtable_scope, 0x10);
        var core = safeReadPointer(traits, 0x8);
        var constant_pool = safeReadPointer(abc_env, 0x8);
        if (abc_env.isNull() || core.isNull() || constant_pool.isNull()) {
            return false;
        }

        var method_list = safeReadPointer(constant_pool, offsets.method_list);
        if (method_list.isNull()) {
            return false;
        }

        avm.toplevel = toplevel;
        avm.abc_env = abc_env;
        avm.core = core;
        avm.constant_pool = constant_pool;

        resolveGamePointers(main_address, base);
        return true;
    } catch (e) {
        resetAvm();
        gameState.ready = false;
        gameState.lastScanNote = String(e);
        return false;
    }
}

function enumerateReadableRanges() {
    if (injectedHeapRanges && injectedHeapRanges.length) {
        return injectedHeapRanges.map(function (r) {
            return { base: ptr(r.base), size: r.size };
        });
    }

    // Game objects live on the heap — avoid scanning DLL images (false positives).
    if (typeof Process.enumerateRangesSync === 'function') {
        try {
            return Process.enumerateRangesSync({ protection: 'r--', coalesce: true });
        } catch (e1) {
            try {
                return Process.enumerateRangesSync('r--');
            } catch (e2) {
                if (!warnedNoRanges) {
                    warnedNoRanges = true;
                    send({ type: 'warn', message: 'enumerateRangesSync failed: ' + e2 });
                }
            }
        }
    }

    if (typeof Process.enumerateMallocRangesSync === 'function') {
        var mallocRanges = Process.enumerateMallocRangesSync();
        if (mallocRanges && mallocRanges.length) {
            return mallocRanges;
        }
    }

    if (!warnedNoRanges) {
        warnedNoRanges = true;
        send({ type: 'warn', message: 'No readable heap ranges for pattern scan' });
    }
    return [];
}

function findPattern(pattern, match_handler) {
    var ranges = enumerateReadableRanges();
    if (!ranges || !ranges.length) {
        if (!warnedNoRanges) {
            warnedNoRanges = true;
            send({ type: 'warn', message: 'findPattern: no readable ranges' });
        }
        return;
    }

    for (var ri = 0; ri < ranges.length; ri++) {
        var range = ranges[ri];
        try {
            Memory.scan(range.base, range.size, pattern, {
                onMatch: match_handler,
                onError: function () {},
                onComplete: function () {}
            });
        } catch (e) {
            send({ type: 'warn', message: 'Memory.scan failed: ' + e });
        }
    }
}

function moveTo(x, y) {
    return queueMove(x, y, 0);
}

function collectTo(x, y, collectableAdr) {
    return queueMove(x, y, collectableAdr);
}

function listMethods(objectPtr, limit) {
    limit = limit || 20;
    var obj = removeKind(ptr(objectPtr));
    var ev_vtable = obj.add(0x10).readPointer();
    var out = [];
    for (var i = 0; i < limit; i++) {
        var env = ev_vtable.add(0x78 + i * 8).readPointer();
        if (env.isNull()) continue;
        var method_info = env.add(0x10).readPointer();
        if (method_info.isNull()) continue;
        out.push({
            index: i,
            name: getMethodName(method_info, true),
            params: getMethodParams(method_info).length,
            compiled: methodIsCompiled(method_info)
        });
    }
    return out;
}

function scanFlashFunctions() {
    if (!flash_lib || !patterns.getproperty) return;

    installVerifyJitHook();

    Memory.scan(flash_lib.base, flash_lib.size, patterns.getproperty, {
        onMatch: function (addr) {
            if (!getproperty_f) {
                getproperty_f = new NativeFunction(ptr(addr), 'pointer', ['pointer', 'pointer', 'pointer', 'pointer']);
            }
        },
        onError: function () {},
        onComplete: function () {}
    });

    Memory.scan(flash_lib.base, flash_lib.size, patterns.createstring, {
        onMatch: function (addr) {
            if (!createstring_f) {
                createstring_f = new NativeFunction(ptr(addr), 'pointer', ['pointer', 'pointer', 'int', 'int', 'bool', 'bool']);
            }
        },
        onError: function () {},
        onComplete: function () {}
    });

    if (patterns.setproperty) {
        Memory.scan(flash_lib.base, flash_lib.size, patterns.setproperty, {
            onMatch: function (addr) {
                if (!setproperty_f) {
                    setproperty_f = new NativeFunction(ptr(addr), 'void', ['pointer', 'pointer', 'pointer', 'pointer', 'pointer']);
                }
            },
            onError: function () {},
            onComplete: function () {}
        });
    }

    if (patterns.newarray) {
        Memory.scan(flash_lib.base, flash_lib.size, patterns.newarray, {
            onMatch: function (addr) {
                if (!newarray_f) {
                    newarray_f = new NativeFunction(ptr(addr), 'pointer', ['pointer', 'int', 'pointer']);
                }
            },
            onError: function () {},
            onComplete: function () {}
        });
    }
}

function scanMainApplication() {
    findPattern(patterns.darkbot, function (addr) {
        tryInitFromPattern(addr);
    });
}

rpc.exports = {
    isReady: function () { return gameState.ready; },
    getStatus: function () {
        var now = Date.now();
        if (now - lastGameStateRefreshMs >= 250) {
            lastGameStateRefreshMs = now;
            refreshGameState();
        }
        return JSON.stringify(gameState);
    },
    moveTo: function (x, y) { return JSON.stringify(moveTo(x, y)); },
    collectTo: function (x, y, collectableAdr) {
        return JSON.stringify(collectTo(x, y, collectableAdr));
    },
    selectEntity: function (argsJson) {
        var args = JSON.parse(argsJson);
        return JSON.stringify(queueSelect(args));
    },
    useItem: function (itemId, methodIndex, argsJson) {
        var args = argsJson ? JSON.parse(argsJson) : [];
        return JSON.stringify(queueUseItem(itemId, methodIndex, args));
    },
    refine: function (refineUtilAddress, oreId, amount, methodIndex) {
        return JSON.stringify(queueRefine(refineUtilAddress, oreId, amount, methodIndex));
    },
    invokeMethod: function (objectPtr, methodIndex, argsJson) {
        var args = argsJson ? JSON.parse(argsJson) : [];
        return JSON.stringify(queueInvoke(objectPtr, methodIndex, args));
    },
    listMethods: function (objectPtr, limit) {
        var target = objectPtr || gameState.eventManager;
        if (!target) return '[]';
        if (!objectPtr || objectPtr === '0') {
            return JSON.stringify({
                candidates: buildMoveCandidates(ptr(gameState.eventManager)),
                methods: listMethods(gameState.eventManager, limit || 32)
            });
        }
        return JSON.stringify({
            methods: listMethods(target, limit || 32)
        });
    }
};

function startAgent() {
    if (gameState.error) {
        send({ type: 'error', error: gameState.error });
        return;
    }

    try {
        scanFlashFunctions();
        scanMainApplication();
    } catch (e) {
        gameState.error = String(e);
        send({ type: 'error', error: gameState.error });
    }

    var rescan = setInterval(function () {
        if (gameState.ready) {
            clearInterval(rescan);
            return;
        }
        try {
            scanMainApplication();
        } catch (e) {
            send({ type: 'warn', message: 'rescan: ' + e });
        }
    }, 2000);
}

// Register RPC before init so Python can poll even if scan is slow.
setImmediate(startAgent);
