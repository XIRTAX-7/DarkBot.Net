package eu.darkbot.bridge;

import com.github.manolo8.darkbot.extensions.util.VerifierChecker;
import com.github.manolo8.darkbot.utils.AuthAPI;

import javax.swing.*;

/**
 * Loads {@code AuthAPI.INSTANCE} before KekkaPlayer native code reads it via JNI.
 *
 * <p>Mirrors Java {@link com.github.manolo8.darkbot.Bot} / {@link com.github.manolo8.darkbot.Main}
 * startup sequence:
 * <ol>
 *   <li>Swing toolkit is initialised on the AWT Event Dispatch Thread (EDT).</li>
 *   <li>{@code VerifierChecker.getAuthApi().setupAuth()} reads the Discord token from disk
 *       (same as {@code Main} constructor on EDT — no {@code isDonor()} before
 *       {@code createWindow()}).</li>
 * </ol>
 *
 * <p>Both methods are safe to call from the EDT or from any other thread.
 */
public final class KekkaAuthBootstrap {
    private static volatile JFrame swingFrame;

    private KekkaAuthBootstrap() {
    }

    /**
     * Initialises Swing toolkit and calls {@code setupAuth()} — same as Java DarkBot
     * {@code Main} constructor running on the EDT.
     *
     * <p>The original DarkBot does <strong>not</strong> call {@code isDonor()} before
     * {@code createWindow()}; only {@code setupAuth()} is used.  A valid Discord token must
     * already exist on disk (written by a prior run of {@code DarkBot.jar}).
     *
     * <p>Safe to call from the EDT and from any other thread.
     */
    public static void ensureAuthApi() {
        ensureSwingToolkit();
        AuthAPI api = VerifierChecker.getAuthApi();

        log("setupAuth (VerifierChecker)...");
        api.setupAuth();

        String authId = api.getAuthId();
        if (authId != null) {
            log("Auth OK, authId=" + mask(authId));
        } else {
            log("setupAuth completed (no token on disk — Discord OAuth will be required on first run)");
        }
    }

    /**
     * Creates the AWT/Swing toolkit. Uses a 1×1 off-screen utility frame — not a user-facing window.
     * Game UI is Darkorbit-client (Electron); bot UI is DarkBot.Net (Avalonia).
     */
    private static void ensureSwingToolkit() {
        if (swingFrame != null) {
            return;
        }

        Runnable init = () -> {
            if (swingFrame != null) return; // double-checked inside EDT
            java.awt.Toolkit.getDefaultToolkit();
            swingFrame = new JFrame();
            swingFrame.setUndecorated(true);
            swingFrame.setDefaultCloseOperation(JFrame.DO_NOTHING_ON_CLOSE);
            swingFrame.setSize(1, 1);
            swingFrame.setLocation(-32000, -32000);
            try {
                swingFrame.setType(java.awt.Window.Type.UTILITY);
            } catch (Exception ignored) {
                // Java 8 — UTILITY type unavailable
            }
            swingFrame.setVisible(true);
        };

        if (SwingUtilities.isEventDispatchThread()) {
            init.run();
        } else {
            try {
                SwingUtilities.invokeAndWait(init);
            } catch (Exception e) {
                throw new AuthBootstrapException("Failed to initialize Swing toolkit for KekkaPlayer", e);
            }
        }
    }

    private static String mask(String value) {
        if (value == null || value.length() < 8) {
            return "***";
        }
        return value.substring(0, Math.min(16, value.length())) + "...";
    }

    private static void log(String message) {
        System.out.println("[KekkaAuth] " + message);
        System.out.flush();
    }

    /** Debug: {@code java -cp DarkBot.jar;classes eu.darkbot.bridge.KekkaAuthBootstrap} */
    public static void main(String[] args) {
        ensureAuthApi();
        System.out.println("AuthAPI.INSTANCE loaded and setupAuth completed");
    }

    public static final class AuthBootstrapException extends RuntimeException {
        public AuthBootstrapException(String message) {
            super(message);
        }

        public AuthBootstrapException(String message, Throwable cause) {
            super(message, cause);
        }
    }
}

