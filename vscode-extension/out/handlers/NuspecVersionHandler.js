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
exports.NuspecVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating version elements in NuGet specification (.nuspec) files.
 */
class NuspecVersionHandler {
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const extension = path.extname(filePath).toLowerCase();
        return extension === '.nuspec';
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        const versionMatch = fileContent.match(/<version>\s*([^<\s]+)\s*<\/version>/i);
        if (versionMatch) {
            return versionMatch[1];
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return `<package>\n  <metadata>\n    <version>${newVersion}</version>\n  </metadata>\n</package>`;
        }
        if (/<version>\s*[^<\s]+\s*<\/version>/i.test(fileContent)) {
            return fileContent.replace(/(<version>\s*)[^<\s]+(\s*<\/version>)/i, `$1${newVersion}$2`);
        }
        const metadataMatch = fileContent.match(/<metadata>/i);
        if (metadataMatch && metadataMatch.index !== undefined) {
            const insertIndex = metadataMatch.index + metadataMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n    <version>${newVersion}</version>` + fileContent.slice(insertIndex);
        }
        const packageMatch = fileContent.match(/<package[^>]*>/i);
        if (packageMatch && packageMatch.index !== undefined) {
            const insertIndex = packageMatch.index + packageMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n  <metadata>\n    <version>${newVersion}</version>\n  </metadata>` + fileContent.slice(insertIndex);
        }
        return fileContent;
    }
}
exports.NuspecVersionHandler = NuspecVersionHandler;
//# sourceMappingURL=NuspecVersionHandler.js.map