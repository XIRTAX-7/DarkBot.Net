package eu.darkbot.bridge;

import java.net.URI;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;

final class KekkaLaunchConfig {

    final String flashOcx;
    final String url;
    final String sid;
    final String preloader;
    final String vars;
    final int width;
    final int height;
    final int proxyPort;

    KekkaLaunchConfig(
        String flashOcx,
        String url,
        String sid,
        String preloader,
        String vars,
        int width,
        int height,
        int proxyPort) {
        this.flashOcx = flashOcx;
        this.url = url;
        this.sid = sid;
        this.preloader = preloader;
        this.vars = vars;
        this.width = width;
        this.height = height;
        this.proxyPort = proxyPort;
    }

    static KekkaLaunchConfig parse(String[] args) throws Exception {
        if (args.length == 1 && args[0].startsWith("@")) {
            return fromPropertiesFile(Paths.get(args[0].substring(1)));
        }

        if (args.length < 5) {
            throw new IllegalArgumentException(
                "Usage: <launcher> <flashOcx> <url> <sid> <preloader> <vars> [width] [height] [proxyPort]"
                    + " OR <launcher> @launch.properties");
        }

        return new KekkaLaunchConfig(
            normalizePath(args[0]),
            args[1],
            args[2],
            args[3],
            args[4],
            args.length > 5 ? Integer.parseInt(args[5]) : 1280,
            args.length > 6 ? Integer.parseInt(args[6]) : 720,
            args.length > 7 ? Integer.parseInt(args[7]) : 0);
    }

    static KekkaLaunchConfig fromPropertiesFile(Path path) throws Exception {
        Map<String, String> properties = new HashMap<>();
        for (String line : Files.readAllLines(path)) {
            String trimmed = line.trim();
            if (trimmed.isEmpty() || trimmed.startsWith("#")) {
                continue;
            }
            int eq = trimmed.indexOf('=');
            if (eq <= 0) {
                continue;
            }
            properties.put(trimmed.substring(0, eq).trim(), trimmed.substring(eq + 1).trim());
        }

        return new KekkaLaunchConfig(
            normalizePath(required(properties, "flashOcx")),
            required(properties, "url"),
            required(properties, "sid"),
            required(properties, "preloader"),
            required(properties, "vars"),
            Integer.parseInt(properties.getOrDefault("width", "1280")),
            Integer.parseInt(properties.getOrDefault("height", "720")),
            Integer.parseInt(properties.getOrDefault("proxyPort", "0")));
    }

    String serverFromUrl() {
        String normalized = url.endsWith("/") ? url : url + "/";
        URI uri = URI.create(normalized);
        String host = uri.getHost();
        if (host == null || host.isEmpty()) {
            throw new IllegalArgumentException("Invalid url: " + url);
        }
        int dot = host.indexOf('.');
        return dot > 0 ? host.substring(0, dot) : host;
    }

    String sidWithoutPrefix() {
        if (sid == null) {
            return "";
        }
        return sid.startsWith("dosid=") ? sid.substring("dosid=".length()) : sid;
    }

    private static String required(Map<String, String> properties, String key) {
        String value = properties.get(key);
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException("Missing property: " + key);
        }
        return value.trim();
    }

    private static String normalizePath(String path) {
        if (path == null || path.isEmpty()) {
            return path;
        }
        return Paths.get(path.replace('/', '\\')).toAbsolutePath().toString();
    }

    @Override
    public String toString() {
        return "LaunchConfig{"
            + "flashOcx='" + flashOcx + '\''
            + ", url='" + url + '\''
            + ", sid='" + maskSid(sid) + '\''
            + ", preloader='" + preloader + '\''
            + ", varsLength=" + (vars != null ? vars.length() : 0)
            + ", width=" + width
            + ", height=" + height
            + ", proxyPort=" + proxyPort
            + '}';
    }

    private static String maskSid(String sid) {
        if (sid == null || sid.length() < 8) {
            return "***";
        }
        return sid.substring(0, Math.min(12, sid.length())) + "...";
    }
}
