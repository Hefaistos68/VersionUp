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
exports.AssemblyInfoVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating version attributes in C#, VB, and F# AssemblyInfo files.
 */
class AssemblyInfoVersionHandler {
    static versionRegex = /(?:\[<assembly:\s*|\[assembly:\s*|<Assembly:\s*)(AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\("([^"]+)"\)(?:\]>|\]|>)/gi;
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const fileName = path.basename(filePath).toLowerCase();
        const ext = path.extname(filePath).toLowerCase();
        const isAssemblyInfo = fileName.startsWith('assemblyinfo');
        const isSupportedExtension = ext === '.cs' || ext === '.vb' || ext === '.fs';
        return isAssemblyInfo && isSupportedExtension;
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        AssemblyInfoVersionHandler.versionRegex.lastIndex = 0;
        const matches = [...fileContent.matchAll(AssemblyInfoVersionHandler.versionRegex)];
        for (const match of matches) {
            if (match[1].toLowerCase() === 'assemblyversion') {
                return match[2];
            }
        }
        for (const match of matches) {
            if (match[1].toLowerCase() === 'assemblyfileversion') {
                return match[2];
            }
        }
        if (matches.length > 0) {
            return matches[0][2];
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }
        AssemblyInfoVersionHandler.versionRegex.lastIndex = 0;
        return fileContent.replace(AssemblyInfoVersionHandler.versionRegex, (match, _attributeName, rawValue) => {
            const index = match.indexOf(rawValue);
            if (index >= 0) {
                return match.slice(0, index) + newVersion + match.slice(index + rawValue.length);
            }
            return match;
        });
    }
}
exports.AssemblyInfoVersionHandler = AssemblyInfoVersionHandler;
//# sourceMappingURL=AssemblyInfoVersionHandler.js.map