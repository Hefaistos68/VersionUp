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
exports.WxsVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating the version attribute in WiX installer setup (.wxs) files.
 */
class WxsVersionHandler {
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const ext = path.extname(filePath).toLowerCase();
        return ext === '.wxs';
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        const match = fileContent.match(/<(Product|Package|Module)[^>]*\s+Version="([^"]+)"/i);
        if (match) {
            return match[2];
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }
        if (/<(Product|Package|Module)[^>]*\s+Version="([^"]+)"/i.test(fileContent)) {
            return fileContent.replace(/(<(Product|Package|Module)[^>]*\s+Version=")[^"]+(")/i, `$1${newVersion}$3`);
        }
        return fileContent;
    }
}
exports.WxsVersionHandler = WxsVersionHandler;
//# sourceMappingURL=WxsVersionHandler.js.map