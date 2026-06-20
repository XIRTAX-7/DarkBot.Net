package eu.darkbot.bridge;

import com.github.manolo8.darkbot.gui.MainGui;
import com.github.manolo8.darkbot.utils.LibSetup;
import com.github.manolo8.darkbot.utils.LogUtils;
import com.github.manolo8.darkbot.utils.StartupParams;
import com.github.manolo8.darkbot.utils.login.LoginData;
import com.github.manolo8.darkbot.utils.login.LoginUtils;
import com.formdev.flatlaf.FlatDarkLaf;
import com.formdev.flatlaf.ui.FlatNativeWindowBorder;
import eu.darkbot.api.KekkaPlayer;
import eu.darkbot.util.Popups;

import javax.swing.*;
import java.awt.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/** Shared DarkBot bootstrap used before {@code KekkaPlayer.createWindow()} (no {@code MainGui}). */
final class KekkaBootstrap {

    private KekkaBootstrap() {
    }

    static void initEnvironment() {
        LogUtils.setupLogOutput();
        initSwingLookAndFeel();
        LibSetup.setupLibraries();
    }

    static Path writeAutologin(KekkaLaunchConfig config) throws Exception {
        Path path = Paths.get("kekka-autologin.properties").toAbsolutePath();
        String content = "server=" + config.serverFromUrl() + System.lineSeparator()
            + "sid=" + config.sidWithoutPrefix() + System.lineSeparator();
        Files.writeString(path, content, StandardCharsets.UTF_8);
        return path;
    }

    /**
     * Same SID login + preloader fetch as {@code GameAPIImpl} constructor ({@code LoginUtils.performUserLogin}).
     */
    static LoginData performSidLogin(KekkaLaunchConfig config) {
        try {
            Path autologin = writeAutologin(config);
            StartupParams params = new StartupParams(new String[] {
                "-login", autologin.toString(),
            });
            if (params.getAutoLoginProps() == null) {
                throw new IllegalStateException("Failed to load autologin properties: " + autologin);
            }
            return LoginUtils.performAutoLogin(params.getAutoLoginProps());
        } catch (Exception e) {
            throw new RuntimeException("SID autologin failed", e);
        }
    }

    static void setDataFromLogin(KekkaPlayer player, LoginData loginData, KekkaLaunchConfig config) {
        String url = loginData.getUrl() == null ? config.url : "https://" + loginData.getUrl() + "/";
        String sid = loginData.getSid() == null ? config.sid : "dosid=" + loginData.getSid();
        String preloader = loginData.getPreloaderUrl() != null ? loginData.getPreloaderUrl() : config.preloader;
        player.setData(url, sid, preloader, config.vars);
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
            System.err.println("[KekkaBootstrap] Swing init warning: " + e);
            e.printStackTrace();
        }
    }
}
