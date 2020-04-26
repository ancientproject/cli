#!/usr/bin/env node

const getBinary = require('./getBinary');

if(!getBinary().exist())
{
    console.log(">> installing binaries...");
    getBinary().install();
}
else
	getBinary().run();