import child_process from "child_process";
import fs from "fs";
import {fileURLToPath, URL} from "node:url";
import path from "path";
import {env} from "process";

import viteReact from "@vitejs/plugin-react";
import {defineConfig} from "vite";

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ""
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

console.log(baseFolder);

const certificateName = "holmes";
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

// eslint-disable-next-line no-nested-ternary
const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(";")[0]
        : "https://localhost:5001";

// https://vitejs.dev/config/

export default defineConfig({
    plugins: [viteReact()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url))
        }
    },
    server: {
        proxy: {
            "^/api": {target, secure: false},
            "^/auth": {target, secure: false}
        },
        port: 3000,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath)
        }
    }
});
