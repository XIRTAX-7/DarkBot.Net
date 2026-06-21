const { ipcRenderer } = require("electron");
const WebSocket = require("ws");

function startControlServer(port) {
    const wss = new WebSocket.WebSocketServer({ port });
    console.log(`DarkBot control WS listening on :${port}`);

    wss.on("connection", ws => {
        ws.binaryType = "arraybuffer";
        ws.on("message", message => {
            handleControlMessage(ws, message).catch(err => console.error("control WS error:", err));
        });
    });

    wss.on("error", err => console.error(`control WS :${port} error:`, err));

    return wss;
}

async function handleControlMessage(ws, message) {
    const data = new DataView(message);
    const opcode = data.getInt16(0);
    let buffer;
    let dv;

    switch (opcode) {
        case 1: {
            const version = String(await ipcRenderer.invoke("getAppVersion")).split(".");
            buffer = new ArrayBuffer(8);
            dv = new DataView(buffer);
            dv.setInt16(0, 1);
            dv.setInt16(2, parseInt(version[0], 10) || 0);
            dv.setInt16(4, parseInt(version[1], 10) || 0);
            dv.setInt16(6, parseInt(version[2], 10) || 0);
            ws.send(buffer);
            break;
        }
        case 2: {
            const metrics = await ipcRenderer.invoke("getAppMetrics");
            buffer = new ArrayBuffer(6);
            dv = new DataView(buffer);
            dv.setInt16(0, 2);
            let pid = 0;
            for (const elem of metrics) {
                if (elem.type === "Pepper Plugin") {
                    pid = elem.pid;
                    break;
                }
            }
            dv.setInt32(2, pid);
            ws.send(buffer);
            break;
        }
        case 3:
            await ipcRenderer.invoke("controlSetSize", data.getInt32(2), data.getInt32(6));
            break;
        case 4:
            await ipcRenderer.invoke("controlSetVisible", data.getInt16(2) === 1);
            break;
        case 5:
            await ipcRenderer.invoke("controlSetMinimized", data.getInt16(2) === 1);
            break;
        case 6:
            location.reload();
            break;
        case 7: {
            buffer = new ArrayBuffer(4);
            dv = new DataView(buffer);
            dv.setInt16(0, 7);
            dv.setInt16(2, document.readyState === "complete" ? 1 : 0);
            ws.send(buffer);
            break;
        }
        default:
            console.warn(`control WS unknown opcode ${opcode}`);
            break;
    }
}

module.exports = { startControlServer };
