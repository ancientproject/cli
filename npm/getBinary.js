const Binary = require('./binary');

const os = require('os');

function getPlatform() {
    const type = os.type();
    const arch = os.arch();

    if (type === 'Windows_NT' && arch === 'x64') return 'win';
    if (type === 'Linux' && arch === 'x64') return 'linux';
    if (type === 'Darwin' && arch === 'x64') return 'macos';

    throw new Error(`Unsupported platform: ${type} ${arch}`);
}

function getBinary() {
    const name = 'rune';
    const version = require('../package.json').version;
    const url = `https://github.com/ancientproject/cli/releases/download/v${version}/${name}-cli-${getPlatform()}-64.zip`;
    return new Binary(url, { name });
}

module.exports = getBinary;
