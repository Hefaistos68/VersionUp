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
exports.RcVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating versions in native C++ resource script (.rc) files.
 */
class RcVersionHandler {
    static fileVersionKeywordRegex = /FILEVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+/gi;
    static productVersionKeywordRegex = /PRODUCTVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+/gi;
    static fileVersionValueRegex = /(VALUE\s+"FileVersion"\s*,\s*")([^"]+)(")/gi;
    static productVersionValueRegex = /(VALUE\s+"ProductVersion"\s*,\s*")([^"]+)(")/gi;
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const ext = path.extname(filePath).toLowerCase();
        return ext === '.rc';
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        RcVersionHandler.fileVersionValueRegex.lastIndex = 0;
        const valueMatch = RcVersionHandler.fileVersionValueRegex.exec(fileContent);
        if (valueMatch) {
            return valueMatch[2];
        }
        RcVersionHandler.fileVersionKeywordRegex.lastIndex = 0;
        const keywordMatch = RcVersionHandler.fileVersionKeywordRegex.exec(fileContent);
        if (keywordMatch) {
            const raw = keywordMatch[0];
            const rawNumbers = raw.replace(/[^\d,]/g, '');
            return rawNumbers.replace(/,/g, '.');
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }
        const parts = newVersion.split('.');
        const segments = [];
        for (let i = 0; i < 4; i++) {
            if (i < parts.length) {
                segments.push(parts[i]);
            }
            else {
                segments.push("0");
            }
        }
        const commaVersion = segments.join(',');
        let result = fileContent;
        result = result.replace(RcVersionHandler.fileVersionKeywordRegex, `FILEVERSION ${commaVersion}`);
        result = result.replace(RcVersionHandler.productVersionKeywordRegex, `PRODUCTVERSION ${commaVersion}`);
        result = result.replace(RcVersionHandler.fileVersionValueRegex, `$1${newVersion}$3`);
        result = result.replace(RcVersionHandler.productVersionValueRegex, `$1${newVersion}$3`);
        return result;
    }
}
exports.RcVersionHandler = RcVersionHandler;
//# sourceMappingURL=RcVersionHandler.js.map