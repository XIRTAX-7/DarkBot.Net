const { ipcRenderer, contextBridge } = require("electron");
const path = require("path");
const fs = require('fs');
const api = require("./api");
const nprogress = require("../libs/nprogress/nprogress");
const nprogressCss = require("../libs/nprogress/nprogressCss.js");

document.onreadystatechange = () => {
    if (document.readyState === 'interactive') {
        contextBridge.exposeInMainWorld("api", api);
        api.injectCss(nprogressCss);
        api.injectCss("#nprogress .bar {background: #7ECE3B !important; height: 3px !important;}");
        nprogress.configure({ showSpinner: false });
        nprogress.start();
        run();
    } else {
        setTimeout(() => {
            nprogress.done();
        }, 1000);
    }
}

window.addEventListener("beforeunload", () => {
    nprogress.configure({ showSpinner: false, minimum: 0.01 });
    nprogress.start();
});

async function run() {
    let url = this.location.href;

    switch (true) {
        case /https:\/\/www\.darkorbit\.[^./]*\//.test(url):
        case /https:\/\/.*\.darkorbit\.com\/\?/.test(url):
        case /https:\/\/.*\.darkorbit\.com\/index\.[^.\/]*\?action=externalHome&loginError=.{0,3}/.test(url):
            require("./login");
            break;
        case /.*\?action=internalMapRevolution.*/.test(url):
            api.getConfig().then(async (data) => {
                async function resolveDarkDevScript(name) {
                    const appPath = await ipcRenderer.invoke("getAppPath");
                    const candidates = [
                        path.join(appPath, '../darkDev', name),
                        path.join(appPath, './darkDev', name),
                    ];
                    if (process.platform == 'linux') {
                        candidates.unshift(path.join(
                            process.resourcesPath.split("/")[1] === "tmp" ? process.resourcesPath : appPath,
                            './darkDev', name));
                    }
                    for (const candidate of candidates) {
                        if (fs.existsSync(candidate)) return candidate;
                    }
                    return candidates[candidates.length - 1];
                }

                function spawnPython(scriptPath, args = []) {
                    const python = require('child_process').spawn('python', [scriptPath, ...args]);
                    python.stdout.on('data', data => console.log(data.toString()));
                    python.stderr.on('data', data => console.error(`stderr: ${data}`));
                    python.on('close', code => console.log(`${path.basename(scriptPath)} exited ${code}`));
                    console.log(`Started ${path.basename(scriptPath)} pid=${python.pid}`);
                }

                if (data.Settings.Packet) {
                    const WebSocket = require('ws');

                    const wss = new WebSocket.WebSocketServer({ port: 44569 });

                    wss.on('connection', ws => {
                        ws.on('message', message => {
                            let event = new CustomEvent("Packet", {
                                detail: {
                                    packet: JSON.parse(message.toString())
                                }
                            });
                            window.dispatchEvent(event);
                        });
                    });

                    for (;;) {
                        if (document.readyState == "complete") {
                            await new Promise(r => setTimeout(r, data.Settings.PacketTimeout));
                            console.log('Start packet_dumper');
                            spawnPython(await resolveDarkDevScript('packet_dumper.py'));
                            break;
                        } else {
                            await new Promise(r => setTimeout(r, 50));
                        }
                    }
                }

                if (data.Settings.Movement) {
                    for (;;) {
                        if (document.readyState == "complete") {
                            await new Promise(r => setTimeout(r, data.Settings.MovementTimeout || 15000));
                            const port = data.Settings.MovementPort || 44570;
                            console.log(`Start avm_move HTTP :${port}`);
                            spawnPython(await resolveDarkDevScript('avm_move.py'), ['--serve', '--port', String(port)]);
                            break;
                        } else {
                            await new Promise(r => setTimeout(r, 50));
                        }
                    }
                }

                if (data.Settings.PreventCloseGame) {
                    api.injectJs("bpCloseWindow = function() {}");
                    window.removeEventListener("beforeunload");
                }
            })
            break;
        default:
            break;
    }
}

function customUrlRegex(match, url) {
    let pattern = match.replaceAll("/", "\\/");
    pattern = pattern.replaceAll(".", "\\.");
    pattern = pattern.replaceAll("*", ".*");
    pattern = pattern.replace(/[+?^${}()|]/g, '\\$&');

    return new RegExp(pattern).test(url);
}

ipcRenderer.once("customJs", (event, data) => {
    if (data.enable) {
        for (let id in data.list) {
            if (data.list[id].enable) {
                if (customUrlRegex(data.list[id].match, document.location.href)) {
                    api.get(data.list[id].actionUrl)
                        .then(res => api.injectJs(res));
                }
            }
        }
    }
});

ipcRenderer.once("customCss", (event, data) => {
    if (data.enable) {
        for (let id in data.list) {
            if (data.list[id].enable) {
                if (customUrlRegex(data.list[id].match, document.location.href)) {
                    api.get(data.list[id].actionUrl)
                        .then(res => api.injectCss(res));
                }
            }
        }
    }
});