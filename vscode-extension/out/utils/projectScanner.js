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
exports.getWorkspaceDiagnostics = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const fs = __importStar(require("fs"));
const handlerRegistry_1 = require("../handlers/handlerRegistry");
/**
 * Reads file contents from an open editor buffer if active/modified, or falls back to disk read.
 */
async function getFileContent(filePath) {
    const openDoc = vscode.workspace.textDocuments.find(doc => doc.fileName === filePath);
    if (openDoc) {
        return openDoc.getText();
    }
    return fs.promises.readFile(filePath, 'utf8');
}
/**
 * Scans the workspace recursively for all supported version files and projects.
 */
async function getWorkspaceDiagnostics() {
    // 1. Find all version-sensitive files across workspace folders
    const allFiles = await vscode.workspace.findFiles('**/*.{csproj,fsproj,vbproj,nuspec,rc,wxs,json,cs,fs,vb,props,targets,appxmanifest,vsixmanifest}', '**/node_modules/**');
    const handledFiles = [];
    const projectFiles = [];
    for (const uri of allFiles) {
        const filePath = uri.fsPath;
        const ext = path.extname(filePath).toLowerCase();
        const filename = path.basename(filePath).toLowerCase();
        // Identify project files
        if (ext === '.csproj' || ext === '.fsproj' || ext === '.vbproj') {
            projectFiles.push(filePath);
        }
        else if (filename === 'package.json') {
            // Include package.json as project files if at root level of workspace or parent folders
            projectFiles.push(filePath);
        }
        if ((0, handlerRegistry_1.getHandlerForFile)(filePath)) {
            handledFiles.push(filePath);
        }
    }
    // Sort projects by depth (longest directory paths first) to match closest parent first
    projectFiles.sort((a, b) => path.dirname(b).length - path.dirname(a).length);
    // Group files by project
    const projectGroups = new Map(); // Project path -> file paths
    const virtualFiles = []; // Files without project files
    for (const filePath of handledFiles) {
        let matchedProj = null;
        for (const projPath of projectFiles) {
            const projDir = path.dirname(projPath);
            if (filePath.startsWith(projDir + path.sep) || filePath === projPath) {
                matchedProj = projPath;
                break;
            }
        }
        if (matchedProj) {
            let list = projectGroups.get(matchedProj);
            if (!list) {
                list = [];
                projectGroups.set(matchedProj, list);
            }
            if (!list.includes(filePath)) {
                list.push(filePath);
            }
        }
        else {
            virtualFiles.push(filePath);
        }
    }
    const projects = [];
    // Parse version files for each project group
    for (const [projPath, filePaths] of projectGroups.entries()) {
        const projName = path.basename(projPath);
        const versions = [];
        let primaryVersion = '';
        for (const filePath of filePaths) {
            const handler = (0, handlerRegistry_1.getHandlerForFile)(filePath);
            if (handler) {
                try {
                    const content = await getFileContent(filePath);
                    const ver = handler.getVersion(content);
                    if (ver) {
                        versions.push({
                            sourceName: path.basename(filePath),
                            filePath,
                            version: ver
                        });
                        // Set project file as primary version
                        if (filePath === projPath) {
                            primaryVersion = ver;
                        }
                    }
                }
                catch {
                    // Fail safe: absorb disk read or parser errors
                }
            }
        }
        if (versions.length > 0) {
            if (!primaryVersion) {
                primaryVersion = versions[0].version;
            }
            const isOutOfSync = checkOutOfSync(versions);
            projects.push({
                name: projName,
                projectPath: projPath,
                diagnostics: {
                    primaryVersion,
                    versions,
                    isOutOfSync
                }
            });
        }
    }
    // Handle virtual project for orphan files
    if (virtualFiles.length > 0) {
        const versions = [];
        for (const filePath of virtualFiles) {
            const handler = (0, handlerRegistry_1.getHandlerForFile)(filePath);
            if (handler) {
                try {
                    const content = await getFileContent(filePath);
                    const ver = handler.getVersion(content);
                    if (ver) {
                        versions.push({
                            sourceName: path.basename(filePath),
                            filePath,
                            version: ver
                        });
                    }
                }
                catch {
                    // Ignore
                }
            }
        }
        if (versions.length > 0) {
            const isOutOfSync = checkOutOfSync(versions);
            projects.push({
                name: 'Workspace (Unlinked)',
                projectPath: vscode.workspace.workspaceFolders?.[0]?.uri.fsPath || '',
                diagnostics: {
                    primaryVersion: versions[0].version,
                    versions,
                    isOutOfSync
                }
            });
        }
    }
    return projects;
}
exports.getWorkspaceDiagnostics = getWorkspaceDiagnostics;
function checkOutOfSync(versions) {
    if (versions.length <= 1) {
        return false;
    }
    let baselineVersion = null;
    for (const v of versions) {
        if (v.version.toLowerCase() !== '$version$') {
            baselineVersion = v.version;
            break;
        }
    }
    if (!baselineVersion) {
        return false;
    }
    for (const v of versions) {
        if (v.version !== baselineVersion && v.version.toLowerCase() !== '$version$') {
            return true;
        }
    }
    return false;
}
//# sourceMappingURL=projectScanner.js.map