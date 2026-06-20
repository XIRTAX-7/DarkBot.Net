package eu.darkbot.bridge;

import com.github.manolo8.darkbot.Main;
import com.github.manolo8.darkbot.utils.StartupParams;

import javax.swing.SwingUtilities;
import java.nio.file.Path;

/**
 * .NET game launcher: Flash client via official {@code Main} init path, DarkBot GUI hidden after startup.
 *
 * <p>{@code KekkaPlayer.createWindow()} requires the full {@code Main} / {@code GameAPIImpl} /
 * {@code KekkaPlayerAdapter} chain from the signed {@code DarkBot.jar}. A stripped direct
 * {@code KekkaPlayer} path crashes in native code (unbound JNI for {@code createWindow}).
 */
public final class KekkaMinimalLauncher {

    private KekkaMinimalLauncher() {
    }

    public static void main(String[] args) throws Exception {
        log("user.dir=" + System.getProperty("user.dir"));
        log("java.library.path=" + System.getProperty("java.library.path"));

        KekkaLaunchConfig config = KekkaLaunchConfig.parse(args);
        log("config=" + config);

        log("Bootstrap: LogUtils + FlatLaf + LibSetup...");
        KekkaBootstrap.initEnvironment();

        Path autologin = KekkaBootstrap.writeAutologin(config);
        log("autologin=" + autologin);

        String[] botArgs = new String[] {
            "-login", autologin.toString(),
            "-hide",
        };
        StartupParams params = new StartupParams(botArgs);

        log("Starting Main on EDT (GameAPIImpl.createWindow), then hiding MainGui...");
        final Main[] mainHolder = new Main[1];
        SwingUtilities.invokeAndWait(() -> {
            mainHolder[0] = new Main(params);
            mainHolder[0].getGui().setVisible(false);
            log("[EDT] Main started, MainGui hidden");
        });

        log("Flash window running — keeping JVM alive (Main tick loop)");
        Thread.currentThread().join();
    }

    private static void log(String message) {
        System.out.println("[KekkaMinimal] " + message);
        System.out.flush();
    }
}
