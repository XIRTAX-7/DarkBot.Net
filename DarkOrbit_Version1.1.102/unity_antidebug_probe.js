'use strict';

/**
 * Lightweight anti-debug probe — avoid hot-path backtraces (they freeze the game).
 */
const MAX_DETAIL_LOGS = 5;

const counts = Object.create(null);

function bump(key) {
    counts[key] = (counts[key] || 0) + 1;
    return counts[key];
}

function shortBacktrace(context) {
    try {
        return Thread.backtrace(context, Backtracer.FUZZY)
            .slice(0, 6)
            .map(function (addr) { return DebugSymbol.fromAddress(addr).toString(); })
            .join(' <- ');
    } catch (e) {
        return '(backtrace failed: ' + e + ')';
    }
}

function logCall(key, context, detail) {
    const n = bump(key);
    if (n > MAX_DETAIL_LOGS) {
        if (n === MAX_DETAIL_LOGS + 1) {
            console.log('[antidebug] ' + key + ': total so far ' + n + ', suppressing details');
        }
        return;
    }
    console.log('[antidebug] ' + key + ' #' + n + (detail ? ' ' + detail : ''));
    console.log('[antidebug]   caller: ' + shortBacktrace(context));
}

function resolveExport(moduleName, exportName) {
    try {
        const mod = Process.getModuleByName(moduleName);
        const addr = mod.getExportByName(exportName);
        if (addr) {
            return addr;
        }
    } catch (e) {
        // module not loaded yet
    }

    if (typeof Module.getGlobalExportByName === 'function') {
        const globalAddr = Module.getGlobalExportByName(exportName);
        if (globalAddr) {
            return globalAddr;
        }
    }

    throw new Error('export not found: ' + moduleName + '!' + exportName);
}

function scanFridaArtifacts() {
    const moduleHits = Process.enumerateModules().filter(function (m) {
        return /frida|gum-js|linjector/i.test(m.name);
    });
    console.log('[antidebug] frida/gum modules visible: ' + moduleHits.length);
    moduleHits.forEach(function (m) {
        console.log('[antidebug]   MODULE ' + m.name);
    });
}

function installAntiDebugHooks() {
    const isDbg = resolveExport('kernel32.dll', 'IsDebuggerPresent');
    Interceptor.attach(isDbg, {
        onEnter: function () {
            logCall('IsDebuggerPresent', this.context);
        },
        onLeave: function (retval) {
            if ((counts.IsDebuggerPresent || 0) <= MAX_DETAIL_LOGS) {
                console.log('[antidebug] IsDebuggerPresent => ' + retval.toInt32());
            }
        }
    });
    console.log('[antidebug] hooked IsDebuggerPresent @ ' + isDbg);

    const checkRemote = resolveExport('kernel32.dll', 'CheckRemoteDebuggerPresent');
    Interceptor.attach(checkRemote, {
        onEnter: function (args) {
            this.outPtr = args[1];
            logCall('CheckRemoteDebuggerPresent', this.context);
        },
        onLeave: function (retval) {
            if ((counts.CheckRemoteDebuggerPresent || 0) <= MAX_DETAIL_LOGS && !this.outPtr.isNull()) {
                try {
                    console.log('[antidebug] CheckRemoteDebuggerPresent => api=' + retval.toInt32() +
                        ' flag=' + this.outPtr.readU32());
                } catch (e) {
                    console.log('[antidebug] CheckRemoteDebuggerPresent read failed: ' + e);
                }
            }
        }
    });
    console.log('[antidebug] hooked CheckRemoteDebuggerPresent @ ' + checkRemote);

    // NtQueryInformationProcess is extremely hot — count only, no backtrace.
    const debugClasses = { 7: 'ProcessDebugPort', 30: 'ProcessDebugObjectHandle', 31: 'ProcessDebugFlags' };
    const ntQuery = resolveExport('ntdll.dll', 'NtQueryInformationProcess');
    Interceptor.attach(ntQuery, {
        onEnter: function (args) {
            this.infoClass = args[1].toInt32();
        },
        onLeave: function (retval) {
            const label = debugClasses[this.infoClass];
            if (!label) {
                return;
            }
            const n = bump('NtQuery:' + label);
            if (n <= MAX_DETAIL_LOGS) {
                console.log('[antidebug] NtQueryInformationProcess(' + label + ') #' + n +
                    ' status=' + retval.toInt32());
            } else if (n === MAX_DETAIL_LOGS + 1) {
                console.log('[antidebug] NtQueryInformationProcess(' + label + '): further calls counted only');
            }
        }
    });
    console.log('[antidebug] hooked NtQueryInformationProcess (count-only) @ ' + ntQuery);
}

function main() {
    console.log('[antidebug] probe loaded pid=' + Process.id);
    scanFridaArtifacts();
    installAntiDebugHooks();
    console.log('[antidebug] ready — lightweight mode, safe for 60s');
}

setImmediate(main);
