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
exports.AppxManifestVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating the package identity version in AppX packaging manifests.
 */
class AppxManifestVersionHandler {
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const fileName = path.basename(filePath).toLowerCase();
        return fileName === 'package.appxmanifest';
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        const match = fileContent.match(/<Identity[^>]*\s+Version="([^"]+)"/i);
        if (match) {
            return this.normalizeToFourSegments(match[1]);
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }
        const normalized = this.normalizeToFourSegments(newVersion);
        if (/<Identity[^>]*\s+Version="([^"]+)"/i.test(fileContent)) {
            return fileContent.replace(/(<Identity[^>]*\s+Version=")[^"]+(")/i, `$1${normalized}$2`);
        }
        return fileContent;
    }
    normalizeToFourSegments(version) {
        if (!version) {
            return "1.0.0.0";
        }
        const parts = version.split('.');
        const segments = [];
        for (let i = 0; i < 4; i++) {
            if (i < parts.length) {
                segments.push(parts[i]);
            }
            else {
                segments.push("0");
            }
        }
        return segments.join(".");
    }
}
exports.AppxManifestVersionHandler = AppxManifestVersionHandler;
//# sourceMappingURL=AppxManifestVersionHandler.js.map