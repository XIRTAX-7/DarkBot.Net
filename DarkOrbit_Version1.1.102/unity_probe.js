'use strict';

/**
 * Unity IL2CPP probe — movement hooks (Dark Orbit v1.1.102, IL2CPP v31).
 * RVAs from Il2CppDumper script.json.
 */
const RVA = {
    heroMoveHandlerHandle: 0xF5FAE0,
    moveCommandHandlerHandle: 0xF633D0,
    opera2DHeroMove: 0x35CB80,
};

const HERO_MOVE_CMD_X_OFFSET = 0x8;
const HERO_MOVE_CMD_Y_OFFSET = 0xC;
const MOVE_CMD_USER_OFFSET = 0x8;
const MOVE_CMD_X_OFFSET = 0xC;
const MOVE_CMD_Y_OFFSET = 0x10;

function resolveGameAssembly() {
    const mod = Process.findModuleByName('GameAssembly.dll');
    if (!mod) {
        throw new Error('GameAssembly.dll not loaded yet — open the game map first');
    }
    return mod;
}

function readVector3(ptr) {
    return {
        x: ptr.readFloat(),
        y: ptr.add(4).readFloat(),
        z: ptr.add(8).readFloat(),
    };
}

function installHeroMoveHook(base) {
    const target = base.add(RVA.heroMoveHandlerHandle);
    Interceptor.attach(target, {
        onEnter(args) {
            try {
                const message = args[1];
                if (message.isNull()) {
                    console.log('[unity_probe] HeroMoveCommand: message=null');
                    return;
                }
                const x = message.add(HERO_MOVE_CMD_X_OFFSET).readS32();
                const y = message.add(HERO_MOVE_CMD_Y_OFFSET).readS32();
                console.log('[unity_probe] HeroMoveCommand: x=' + x + ' y=' + y);
            } catch (e) {
                console.log('[unity_probe] HeroMoveCommand error: ' + e);
            }
        }
    });
    console.log('[unity_probe] HeroMoveCommandHandler.Handle @ ' + target);
}

function installMoveCommandHook(base) {
    const target = base.add(RVA.moveCommandHandlerHandle);
    Interceptor.attach(target, {
        onEnter(args) {
            try {
                const message = args[1];
                if (message.isNull()) {
                    console.log('[unity_probe] MoveCommand: message=null');
                    return;
                }
                const userId = message.add(MOVE_CMD_USER_OFFSET).readS32();
                const x = message.add(MOVE_CMD_X_OFFSET).readS32();
                const y = message.add(MOVE_CMD_Y_OFFSET).readS32();
                console.log('[unity_probe] MoveCommand: userId=' + userId + ' x=' + x + ' y=' + y);
            } catch (e) {
                console.log('[unity_probe] MoveCommand error: ' + e);
            }
        }
    });
    console.log('[unity_probe] MoveCommandHandler.Handle @ ' + target);
}

function installOperaClickHook(base) {
    const target = base.add(RVA.opera2DHeroMove);
    Interceptor.attach(target, {
        onEnter(args) {
            try {
                const clickPos = readVector3(args[1]);
                console.log('[unity_probe] Opera2D.HeroMove: x=' + clickPos.x.toFixed(1) +
                    ' y=' + clickPos.y.toFixed(1) + ' z=' + clickPos.z.toFixed(1));
            } catch (e) {
                console.log('[unity_probe] Opera2D.HeroMove error: ' + e);
            }
        }
    });
    console.log('[unity_probe] Opera2DComponentSystem.HeroMove @ ' + target);
}

function main() {
    console.log('[unity_probe] agent loaded, pid=' + Process.id);
    const ga = resolveGameAssembly();
    console.log('[unity_probe] GameAssembly base=' + ga.base);
    installHeroMoveHook(ga.base);
    installMoveCommandHook(ga.base);
    installOperaClickHook(ga.base);
    console.log('[unity_probe] ready — click/move on the map');
}

setImmediate(main);
