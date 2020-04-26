const Binary = require('./binary');
function getBinary() {
    const name = 'rune';
    const version = require('../package.json').version;
    const url = `https://github.com/ancientproject/cli/releases/download/v${version}/${name}-cli-win-64.zip`;
    return new Binary(url, { name });
}

module.exports = getBinary;
