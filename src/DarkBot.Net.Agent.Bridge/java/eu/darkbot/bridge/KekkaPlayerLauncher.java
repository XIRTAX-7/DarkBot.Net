package eu.darkbot.bridge;

import eu.darkbot.api.KekkaPlayer;

/**
 * Standalone launcher that mirrors the Java DarkBot startup sequence (direct KekkaPlayer calls).
 *
 * <p>Prefer {@link KekkaGameApiLauncher} — it uses {@code Bot.main → Main → GameAPIImpl.createWindow()},
 * matching the working original DarkBot path.
 *
 * @see KekkaGameApiLauncher
 */
public final class KekkaPlayerLauncher {

    private static final long SWING_SETTLE_MS = 2500;

    private KekkaPlayerLauncher() {
    }

    public static void main(String[] args) throws Exception {
        log("user.dir=" + System.getProperty("user.dir"));
        log("java.library.path=" + System.getProperty("java.library.path"));

        KekkaLaunchConfig config = KekkaLaunchConfig.parse(args);
        log("config=" + config);

        log("EDT-1: Swing toolkit + setupAuth...");
        invokeOnEdt(() -> {
            log("[EDT] ensureAuthApi...");
            KekkaAuthBootstrap.ensureAuthApi();
            log("[EDT] ensureAuthApi OK");
        });
        log("EDT-1 complete");

        log("Pausing " + SWING_SETTLE_MS + " ms (EDT free, AWT/COM STA settle)...");
        Thread.sleep(SWING_SETTLE_MS);
        log("Settle complete");

        final Thread[] apiThreadHolder = new Thread[1];
        log("EDT-2: KekkaPlayer setup + start API thread from EDT...");
        invokeOnEdt(() -> {
            KekkaPlayer player = new KekkaPlayer();
            log("[EDT] KekkaPlayer version=" + player.getVersion());

            player.setFlashOcxPath(config.flashOcx);
            log("[EDT] setFlashOcxPath OK path=" + config.flashOcx);

            player.setMinClientSize(800, 600);
            log("[EDT] setMinClientSize OK");

            player.setBlockingPatterns();
            log("[EDT] setBlockingPatterns OK");

            if (config.proxyPort > 0) {
                player.setLocalProxy(config.proxyPort);
                log("[EDT] setLocalProxy OK port=" + config.proxyPort);
            }

            player.setSize(config.width, config.height);
            log("[EDT] setSize OK");

            player.setData(config.url, config.sid, config.preloader, config.vars);
            log("[EDT] setData OK");

            Thread apiThread = new Thread(player::createWindow, "API thread");
            apiThread.setDaemon(true);
            apiThread.setUncaughtExceptionHandler((thread, error) -> {
                log("API thread: createWindow threw " + error);
                error.printStackTrace();
            });
            log("[EDT] starting API thread (player::createWindow)...");
            apiThread.start();
            log("[EDT] API thread started");

            apiThreadHolder[0] = apiThread;
        });
        log("EDT-2 complete");

        Thread apiThread = apiThreadHolder[0];
        if (apiThread == null) {
            log("ERROR: EDT-2 did not produce an API thread — launcher exiting");
            return;
        }

        log("Main thread keep-alive (like DarkBot Main.run loop)");
        while (apiThread.isAlive()) {
            Thread.sleep(1000);
        }
        log("API thread exited — launcher ending");
    }

    private static void invokeOnEdt(Runnable task) throws Exception {
        try {
            javax.swing.SwingUtilities.invokeAndWait(task);
        } catch (java.lang.reflect.InvocationTargetException e) {
            Throwable cause = e.getCause();
            if (cause instanceof RuntimeException) throw (RuntimeException) cause;
            if (cause instanceof Exception)        throw (Exception) cause;
            throw new RuntimeException("EDT task failed", cause);
        }
    }

    private static void log(String message) {
        System.out.println("[KekkaLauncher] " + message);
        System.out.flush();
    }
}
