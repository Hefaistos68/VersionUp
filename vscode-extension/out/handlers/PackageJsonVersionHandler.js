"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.PackageJsonVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating the version property in package.json files.
 */
class PackageJsonVersionHandler {
    static versionRegex = /"version"\s*:\s*"([^"]+)"/gi;
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const fileName = path.basename(filePath).toLowerCase();
        return fileName === 'package.json';
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        PackageJsonVersionHandler.versionRegex.lastIndex = 0;
        const match = PackageJsonVersionHandler.versionRegex.exec(fileContent);
        if (match) {
            return match[1];
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return `{\n  "version": "${newVersion}"\n}`;
        }
        PackageJsonVersionHandler.versionRegex.lastIndex = 0;
        return fileContent.replace(PackageJsonVersionHandler.versionRegex, (match, rawValue) => {
            const index = match.indexOf(rawValue);
            if (index >= 0) {
                return match.slice(0, index) + newVersion + match.slice(index + rawValue.length);
            }
            return match;
        });
    }
}
exports.PackageJsonVersionHandler = PackageJsonVersionHandler;
//# sourceMappingURL=PackageJsonVersionHandler.js.map