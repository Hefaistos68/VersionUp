"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const unit_tests_1 = require("./unit-tests");
try {
    (0, unit_tests_1.runAllTests)();
    process.exit(0);
}
catch (err) {
    console.error('\nTest execution failed!');
    console.error(err.stack || err.message || err);
    process.exit(1);
}
//# sourceMappingURL=run-tests.js.map