import { runAllTests } from './unit-tests';

try {
    runAllTests();
    process.exit(0);
} catch (err: any) {
    console.error('\nTest execution failed!');
    console.error(err.stack || err.message || err);
    process.exit(1);
}
