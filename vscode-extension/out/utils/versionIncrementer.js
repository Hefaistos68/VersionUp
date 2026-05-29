"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.VersionIncrementer = exports.VersionSegment = void 0;
var VersionSegment;
(function (VersionSegment) {
    VersionSegment["Major"] = "Major";
    VersionSegment["Minor"] = "Minor";
    VersionSegment["Build"] = "Build";
    VersionSegment["Revision"] = "Revision";
})(VersionSegment = exports.VersionSegment || (exports.VersionSegment = {}));
class VersionIncrementer {
    logger;
    constructor(logger) {
        this.logger = logger;
    }
    /**
     * Increments a specific segment of a version string.
     * @param currentVersion The current version string.
     * @param segment The version segment to increment.
     */
    increment(currentVersion, segment) {
        if (!currentVersion || !currentVersion.trim()) {
            this.logger.log("Current version is empty, returning default 1.0.0");
            return "1.0.0";
        }
        const match = currentVersion.trim().match(/^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$/);
        if (!match) {
            this.logger.log(`Failed to parse version '${currentVersion}', returning default 1.0.0`);
            return "1.0.0";
        }
        let major = parseInt(match[1], 10);
        let minor = parseInt(match[2], 10);
        let build = match[3] !== undefined ? parseInt(match[3], 10) : -1;
        let revision = match[4] !== undefined ? parseInt(match[4], 10) : -1;
        let b = build < 0 ? 0 : build;
        let r = revision < 0 ? 0 : revision;
        switch (segment) {
            case VersionSegment.Major:
                major++;
                minor = 0;
                b = 0;
                r = 0;
                break;
            case VersionSegment.Minor:
                minor++;
                b = 0;
                r = 0;
                break;
            case VersionSegment.Build:
                b++;
                r = 0;
                break;
            case VersionSegment.Revision:
                r++;
                break;
            default:
                throw new Error(`Unsupported segment: ${segment}`);
        }
        const result = r > 0
            ? `${major}.${minor}.${b}.${r}`
            : `${major}.${minor}.${b}`;
        this.logger.log(`Incremented version from '${currentVersion}' to '${result}' (Segment: ${segment})`);
        return result;
    }
}
exports.VersionIncrementer = VersionIncrementer;
//# sourceMappingURL=versionIncrementer.js.map