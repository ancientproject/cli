const { existsSync, createWriteStream } = require("fs");
const { join } = require("path");
const { spawnSync } = require("child_process");
const { URL } = require("universal-url");
const envPaths = require("env-paths");
const mkdirp = require("mkdirp");
const extract = require('extract-zip');
const axios = require("axios");
const rimraf = require("rimraf");
const ProgressBar = require('progress')
class Binary {
    constructor(url, data) {
        if (typeof url !== "string") {
            errors.push("url must be a string");
        } else {
            try {
                new URL(url);
            } catch (e) {
                errors.push(e);
            }
        }
        let errors = [];
        if (data.name && typeof data.name !== "string") {
            errors.push("name must be a string");
        }
        if (data.installDirectory && typeof data.installDirectory !== "string") {
            errors.push("installDirectory must be a string");
        }
        if (!data.installDirectory && !data.name) {
            errors.push("You must specify either name or installDirectory");
        }
        if (errors.length > 0) {
            console.error("Your Binary constructor is invalid:");
            errors.forEach(error => {
                console.error(error);
            });
        }
        this.url = url;
        this.name = data.name || -1;
        this.installDirectory = data.installDirectory || envPaths(this.name).config;
        this.binaryDirectory = -1;
        this.binaryPath = -1;
    }

    _getInstallDirectory() {
        if (!existsSync(this.installDirectory)) {
            mkdirp.sync(this.installDirectory);
        }
        return this.installDirectory;
    }

    _getBinaryDirectory() {
        const installDirectory = this._getInstallDirectory();
        const binaryDirectory = join(installDirectory, "bin");
        if (existsSync(binaryDirectory)) {
            this.binaryDirectory = binaryDirectory;
        } else {
            throw `You have not installed ${this.name ? this.name : "this package"}`;
        }
        return this.binaryDirectory;
    }

    _getBinaryPath() {
        const dir = this._getInstallDirectory();
        const binaryDirectory = join(dir, "bin");
        if (this._getPlatform() == "win64")
            return join(binaryDirectory, `${this.name}.exe`);
        return join(binaryDirectory, `${this.name}`)
    }

    _getPlatform() {
        const os = require("os");
        const type = os.type();
        const arch = os.arch();

        if (type === 'Windows_NT' && arch === 'x64') return 'win64';
        if (type === 'Linux' && arch === 'x64') return 'linux';
        if (type === 'Darwin' && arch === 'x64') return 'macos';

        throw new Error(`Unsupported platform: ${type} ${arch}`);
    }

    install() {
        const dir = this._getInstallDirectory();
        if (!existsSync(dir)) {
            mkdirp.sync(dir);
        }

        this.binaryDirectory = join(dir, "bin");

        if (existsSync(this.binaryDirectory)) {
            rimraf.sync(this.binaryDirectory);
        }

        mkdirp.sync(this.binaryDirectory);

        console.log("Downloading release", this.url);
        async function downloadFile(url, file) {
            const { data, headers } = await axios({
                url,
                method: 'GET',
                responseType: 'stream'
            });
            const totalLength = headers['content-length'];
            const progressBar = new ProgressBar('[:bar] :rate/bps :percent :etas', {
                width: 40,
                complete: '=',
                incomplete: ' ',
                renderThrottle: 1,
                total: parseInt(totalLength)
            });

            const writer = createWriteStream(file);

            data.on('data', (chunk) => progressBar.tick(chunk.length));
            data.pipe(writer);
            return new Promise((resolve, reject) => {
                writer.on('finish', resolve)
                writer.on('error', reject)
            });
        }

        const file = join(this._getInstallDirectory(), "target.zip");
        downloadFile(this.url, file).then(() => {
            extract(file, { dir: this.binaryDirectory })
                .then(() => {
                    console.log(
                        `${this.name ? this.name : "Your package"} has been installed!`
                    );
                }).catch(e => {
                    console.error("Error extract release", e.message);
                    throw e;
                });
        });
    }

    exist() {
        const binaryPath = this._getBinaryPath();
        return existsSync(binaryPath);
    }

    uninstall() {
        if (existsSync(this._getInstallDirectory())) {
            rimraf.sync(this.installDirectory);
            console.log(
                `${this.name ? this.name : "Your package"} has been uninstalled`
            );
        }
    }

    run() {
        const binaryPath = this._getBinaryPath();
        const [, , ...args] = process.argv;

        const options = {
            cwd: process.cwd(),
            stdio: "inherit"
        };

        const result = spawnSync(binaryPath, args, options);

        if (result.error) {
            console.error(result.error);
            process.exit(1);
        }

        process.exit(result.status);
    }
}

module.exports = Binary;
