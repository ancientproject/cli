#!/usr/bin/env node

const getBinary = require('./getBinary');


if(!getBinary().exist())
    getBinary().install();
getBinary().run();