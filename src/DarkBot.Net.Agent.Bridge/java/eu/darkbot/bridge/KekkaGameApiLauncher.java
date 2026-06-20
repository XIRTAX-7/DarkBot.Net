package eu.darkbot.bridge;

import com.github.manolo8.darkbot.Main;
import com.github.manolo8.darkbot.gui.MainGui;
import com.github.manolo8.darkbot.utils.LibSetup;
import com.github.manolo8.darkbot.utils.LogUtils;
import com.github.manolo8.darkbot.utils.StartupParams;
import com.formdev.flatlaf.FlatDarkLaf;
import com.formdev.flatlaf.ui.FlatNativeWindowBorder;
import eu.darkbot.util.Popups;

import javax.swing.*;
import java.awt.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Step 3 launcher: original DarkBot path {@code Main} → {@code GameAPIImpl#createWindow()}.
 *
 * <p>Reads {@code launch.properties}, writes SID autologin, runs the same bootstrap as
 * {@link com.github.manolo8.darkbot.Bot#main}, then blocks on {@code invokeAndWait(new Main)}.
 */
public final class KekkaGameApiLauncher {

    private KekkaGameApiLauncher() {
    }

    public static void main(String[] args) throws Exception {
        log("user.dir=" + System.getProperty("user.dir"));
        log("java.library.path=" + System.getProperty("java.library.path"));

        KekkaLaunchConfig config = KekkaLaunchConfig.parse(args);
        log("config=" + config);

        Path autologin = writeAutologin(config);
        log("autologin=" + autologin);

        LogUtils.setupLogOutput();
        initSwingLookAndFeel();
        LibSetup.setupLibraries();

        String[] botArgs = new String[] {
            "-login", autologin.toString(),
            "-hide",
        };
        StartupParams params = new StartupParams(botArgs);
        log("Starting Main on EDT (GameAPIImpl.createWindow path)...");

        SwingUtilities.invokeAndWait(() -> new Main(params));

        log("Main constructor finished — keeping JVM alive (Main.run loop)");
        Thread.currentThread().join();
    }

    private static void initSwingLookAndFeel() {
        try {
            UIManager.put("MenuItem.selectionType", "underline");
            UIManager.getFont("Label.font");
            UIManager.put("TitlePane.noIconLeftGap", 0);
            UIManager.put("OptionPane.showIcon", true);
            JFrame.setDefaultLookAndFeelDecorated(true);
            JDialog.setDefaultLookAndFeelDecorated(true);
            FlatNativeWindowBorder.isSupported();
            UIManager.setLookAndFeel(new FlatDarkLaf());
            UIManager.put("Button.arc", 0);
            UIManager.put("Component.arc", 0);
            UIManager.put("Button.default.boldText", false);
            UIManager.put("Table.cellFocusColor", new Color(0, 0, 0, 160));
            Popups.setDefaultIcon(MainGui.ICON);
        } catch (Exception e) {
            log("Swing init warning: " + e);
            e.printStackTrace();
        }
    }

    private static Path writeAutologin(KekkaLaunchConfig config) throws Exception {
        Path path = Paths.get("kekka-autologin.properties").toAbsolutePath();
        String content = "server=" + config.serverFromUrl() + System.lineSeparator()
            + "sid=" + config.sidWithoutPrefix() + System.lineSeparator();
        Files.writeString(path, content, StandardCharsets.UTF_8);
        return path;
    }

    private static void log(String message) {
        System.out.println("[KekkaGameApi] " + message);
        System.out.flush();
    }
}
