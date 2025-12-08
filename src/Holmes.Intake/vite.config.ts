import child_process from "child_process";
import fs from "fs";
import {fileURLToPath, URL} from "node:url";
import path from "path";
import {env} from "process";

import plugin from "@vitejs/plugin-react";
import {defineConfig} from "vite";

const isDevelopment = process.env.NODE_ENV === "development";

let httpsConfig;
let proxyConfig;

if (isDevelopment) {
    const baseFolder =
        env.APPDATA !== undefined && env.APPDATA !== ""
            ? `${env.APPDATA}/ASP.NET/https`
            : `${env.HOME}/.aspnet/https`;

    const certificateName = "holmes-intake";
    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (
            child_process.spawnSync(
                "dotnet",
                [
                    "dev-certs",
                    "https",
                    "--export-path",
                    certFilePath,
                    "--format",
                    "Pem",
                    "--no-password"
                ],
                {stdio: "inherit"}
            ).status !== 0
        ) {
            throw new Error("Could not create certificate.");
        }
    }

    httpsConfig = {
        key: fs.readFileSync(keyFilePath),
        cert: fs.readFileSync(certFilePath)
    };

    let target = "https://localhost:5002";
    if (env.ASPNETCORE_HTTPS_PORT) {
        target = `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`;
    } else if (env.ASPNETCORE_URLS) {
        target = env.ASPNETCORE_URLS.split(";")[0];
    }

    proxyConfig = {
        "^/api": {target, secure: false}
    };
}

export default defineConfig({
    plugins: [plugin()],
    base: "/",
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
            "@holmes/ui-core": fileURLToPath(new URL("../Holmes.Core/src", import.meta.url))
        },
        dedupe: ["@emotion/react", "@emotion/styled", "@mui/material", "react", "react-dom"]
    },
    server: isDevelopment
        ? {
            port: 3002,
            proxy: proxyConfig,
            https: httpsConfig
        }
        : {
            port: 3002
        }
});
