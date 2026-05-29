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
exports.CsprojVersionHandler = void 0;
const path = __importStar(require("path"));
/**
 * Handles parsing and updating version elements in MSBuild project and properties files.
 */
class CsprojVersionHandler {
    canHandle(filePath) {
        if (!filePath) {
            return false;
        }
        const ext = path.extname(filePath).toLowerCase();
        const filename = path.basename(filePath).toLowerCase();
        const isProject = ext === '.csproj' || ext === '.fsproj' || ext === '.vbproj';
        const isBuildProps = filename === 'directory.build.props' || filename === 'directory.build.targets';
        return isProject || isBuildProps;
    }
    getVersion(fileContent) {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }
        const versionMatch = fileContent.match(/<Version>\s*([^<\s]+)\s*<\/Version>/i);
        if (versionMatch) {
            return versionMatch[1];
        }
        const packageVersionMatch = fileContent.match(/<PackageVersion>\s*([^<\s]+)\s*<\/PackageVersion>/i);
        if (packageVersionMatch) {
            return packageVersionMatch[1];
        }
        return null;
    }
    updateVersion(fileContent, newVersion) {
        if (!fileContent || !fileContent.trim()) {
            return `<Project>\n  <PropertyGroup>\n    <Version>${newVersion}</Version>\n  </PropertyGroup>\n</Project>`;
        }
        if (/<Version>\s*[^<\s]+\s*<\/Version>/i.test(fileContent)) {
            return fileContent.replace(/(<Version>\s*)[^<\s]+(\s*<\/Version>)/i, `$1${newVersion}$2`);
        }
        if (/<PackageVersion>\s*[^<\s]+\s*<\/PackageVersion>/i.test(fileContent)) {
            return fileContent.replace(/(<PackageVersion>\s*)[^<\s]+(\s*<\/PackageVersion>)/i, `$1${newVersion}$2`);
        }
        const propertyGroupMatch = fileContent.match(/<PropertyGroup>/i);
        if (propertyGroupMatch && propertyGroupMatch.index !== undefined) {
            const insertIndex = propertyGroupMatch.index + propertyGroupMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n    <Version>${newVersion}</Version>` + fileContent.slice(insertIndex);
        }
        const projectMatch = fileContent.match(/<Project[^>]*>/i);
        if (projectMatch && projectMatch.index !== undefined) {
            const insertIndex = projectMatch.index + projectMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n  <PropertyGroup>\n    <Version>${newVersion}</Version>\n  </PropertyGroup>` + fileContent.slice(insertIndex);
        }
        return fileContent;
    }
}
exports.CsprojVersionHandler = CsprojVersionHandler;
//# sourceMappingURL=CsprojVersionHandler.js.map