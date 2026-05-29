import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { getHandlerForFile } from '../handlers/handlerRegistry';

export interface VersionDetails {
    sourceName: string;
    filePath: string;
    version: string;
}

export interface ProjectVersionDiagnostics {
    primaryVersion: string;
    versions: VersionDetails[];
    isOutOfSync: boolean;
}

export interface ProjectInfo {
    name: string;
    projectPath: string; // Path to project file or virtual directory
    diagnostics: ProjectVersionDiagnostics;
}

/**
 * Reads file contents from an open editor buffer if active/modified, or falls back to disk read.
 */
async function getFileContent(filePath: string): Promise<string> {
    const openDoc = vscode.workspace.textDocuments.find(doc => doc.fileName === filePath);
    if (openDoc) {
        return openDoc.getText();
    }
    return fs.promises.readFile(filePath, 'utf8');
}

/**
 * Scans the workspace recursively for all supported version files and projects.
 */
export async function getWorkspaceDiagnostics(): Promise<ProjectInfo[]> {
    // 1. Find all version-sensitive files across workspace folders
    const allFiles = await vscode.workspace.findFiles(
        '**/*.{csproj,fsproj,vbproj,nuspec,rc,wxs,json,cs,fs,vb,props,targets,appxmanifest,vsixmanifest}',
        '**/node_modules/**'
    );

    const handledFiles: string[] = [];
    const projectFiles: string[] = [];

    for (const uri of allFiles) {
        const filePath = uri.fsPath;
        const ext = path.extname(filePath).toLowerCase();
        const filename = path.basename(filePath).toLowerCase();

        // Identify project files
        if (ext === '.csproj' || ext === '.fsproj' || ext === '.vbproj') {
            projectFiles.push(filePath);
        } else if (filename === 'package.json') {
            // Include package.json as project files if at root level of workspace or parent folders
            projectFiles.push(filePath);
        }

        if (getHandlerForFile(filePath)) {
            handledFiles.push(filePath);
        }
    }

    // Sort projects by depth (longest directory paths first) to match closest parent first
    projectFiles.sort((a, b) => path.dirname(b).length - path.dirname(a).length);

    // Group files by project
    const projectGroups = new Map<string, string[]>(); // Project path -> file paths
    const virtualFiles: string[] = []; // Files without project files

    for (const filePath of handledFiles) {
        let matchedProj: string | null = null;
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
        } else {
            virtualFiles.push(filePath);
        }
    }

    const projects: ProjectInfo[] = [];

    // Parse version files for each project group
    for (const [projPath, filePaths] of projectGroups.entries()) {
        const projName = path.basename(projPath);
        const versions: VersionDetails[] = [];
        let primaryVersion = '';

        for (const filePath of filePaths) {
            const handler = getHandlerForFile(filePath);
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
                } catch {
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
        const versions: VersionDetails[] = [];
        for (const filePath of virtualFiles) {
            const handler = getHandlerForFile(filePath);
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
                } catch {
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

function checkOutOfSync(versions: VersionDetails[]): boolean {
    if (versions.length <= 1) {
        return false;
    }

    let baselineVersion: string | null = null;
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
