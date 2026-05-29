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
exports.deactivate = exports.activate = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const versionIncrementer_1 = require("./utils/versionIncrementer");
const logger_1 = require("./utils/logger");
const handlerRegistry_1 = require("./handlers/handlerRegistry");
const projectScanner_1 = require("./utils/projectScanner");
const statusBar_1 = require("./ui/statusBar");
const quickPickList_1 = require("./ui/quickPickList");
const dialogs_1 = require("./ui/dialogs");
let logger;
let statusBar;
function activate(context) {
    logger = new logger_1.VersionLogger();
    logger.log('VersionUp extension has been activated.');
    statusBar = new statusBar_1.VersionStatusBar();
    statusBar.show();
    // Register active editor listeners to refresh status bar
    context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(() => statusBar.update()), vscode.workspace.onDidSaveTextDocument(() => statusBar.update()), statusBar);
    // Initial status bar update
    statusBar.update();
    // Helper to execute segment increments
    const runIncrement = async (uri, segment) => {
        await executeVersionCommand(uri, async (currentVersion) => {
            const incrementer = new versionIncrementer_1.VersionIncrementer(logger);
            return incrementer.increment(currentVersion, segment);
        });
    };
    // Register commands
    context.subscriptions.push(vscode.commands.registerCommand('versionup.incrementMajor', (uri) => runIncrement(uri, versionIncrementer_1.VersionSegment.Major)), vscode.commands.registerCommand('versionup.incrementMinor', (uri) => runIncrement(uri, versionIncrementer_1.VersionSegment.Minor)), vscode.commands.registerCommand('versionup.incrementBuild', (uri) => runIncrement(uri, versionIncrementer_1.VersionSegment.Build)), vscode.commands.registerCommand('versionup.incrementRevision', (uri) => runIncrement(uri, versionIncrementer_1.VersionSegment.Revision)), vscode.commands.registerCommand('versionup.setVersion', async (uri) => {
        await executeVersionCommand(uri, async (currentVersion) => {
            const newVersion = await (0, dialogs_1.promptSetVersion)(currentVersion);
            return newVersion; // undefined if cancelled
        });
    }), vscode.commands.registerCommand('versionup.showProjectVersions', async () => {
        try {
            const projects = await (0, projectScanner_1.getWorkspaceDiagnostics)();
            await (0, quickPickList_1.showProjectVersionsQuickPick)(projects, async (projectPath, targetVersion) => {
                await alignProjectVersions(projectPath, targetVersion);
            });
        }
        catch (ex) {
            logger.log(`Error showing project list: ${ex.message}`);
            vscode.window.showErrorMessage(`Failed to display project list: ${ex.message}`);
        }
    }));
}
exports.activate = activate;
function deactivate() {
    if (statusBar) {
        statusBar.dispose();
    }
}
exports.deactivate = deactivate;
/**
 * Executes a version command (increment or set) on the selected or active file.
 */
async function executeVersionCommand(uri, getVersionUpdate) {
    const selectedPath = getSelectedPath(uri);
    if (!selectedPath) {
        vscode.window.showWarningMessage('No active project or file selected for version update.');
        return;
    }
    const handler = (0, handlerRegistry_1.getHandlerForFile)(selectedPath);
    if (!handler) {
        vscode.window.showWarningMessage('Unsupported file type for version update.');
        return;
    }
    try {
        const doc = await vscode.workspace.openTextDocument(selectedPath);
        const fileContent = doc.getText();
        const currentVersion = handler.getVersion(fileContent) || '1.0.0';
        const newVersion = await getVersionUpdate(currentVersion);
        if (!newVersion) {
            // Cancelled
            return;
        }
        const updatedContent = handler.updateVersion(fileContent, newVersion);
        // Apply edit atomically
        const edit = new vscode.WorkspaceEdit();
        const range = new vscode.Range(doc.positionAt(0), doc.positionAt(fileContent.length));
        edit.replace(doc.uri, range, updatedContent);
        // Check if there are other files to align inside this project
        const projects = await (0, projectScanner_1.getWorkspaceDiagnostics)();
        const matchedProj = projects.find(p => {
            const isFile = p.projectPath.endsWith('.json') ||
                p.projectPath.endsWith('.csproj') ||
                p.projectPath.endsWith('.fsproj') ||
                p.projectPath.endsWith('.vbproj');
            const projDir = isFile ? path.dirname(p.projectPath) : p.projectPath;
            return selectedPath.startsWith(projDir + path.sep) || selectedPath === p.projectPath;
        });
        let alignFiles = [];
        if (matchedProj && matchedProj.diagnostics.versions.length > 1) {
            const decisions = vscode.workspace.getConfiguration('versionup').get('alignmentDecisions') || {};
            const savedDecision = decisions[matchedProj.projectPath];
            let shouldAlign = false;
            if (savedDecision === 'Always') {
                shouldAlign = true;
            }
            else if (savedDecision !== 'Never') {
                const choice = await (0, dialogs_1.promptAlignment)(matchedProj.name, newVersion);
                if (choice === 'Always') {
                    shouldAlign = true;
                    decisions[matchedProj.projectPath] = 'Always';
                    await vscode.workspace.getConfiguration('versionup').update('alignmentDecisions', decisions, vscode.ConfigurationTarget.Workspace);
                }
                else if (choice === 'Never') {
                    shouldAlign = false;
                    decisions[matchedProj.projectPath] = 'Never';
                    await vscode.workspace.getConfiguration('versionup').update('alignmentDecisions', decisions, vscode.ConfigurationTarget.Workspace);
                }
                else if (choice === 'Yes') {
                    shouldAlign = true;
                }
            }
            if (shouldAlign) {
                for (const verDetail of matchedProj.diagnostics.versions) {
                    if (verDetail.filePath !== selectedPath) {
                        const fileHandler = (0, handlerRegistry_1.getHandlerForFile)(verDetail.filePath);
                        if (fileHandler) {
                            alignFiles.push({
                                filePath: verDetail.filePath,
                                handler: fileHandler
                            });
                        }
                    }
                }
            }
        }
        // Add alignment changes to the transaction
        for (const file of alignFiles) {
            const fileDoc = await vscode.workspace.openTextDocument(file.filePath);
            const otherContent = fileDoc.getText();
            const otherUpdated = file.handler.updateVersion(otherContent, newVersion);
            const otherRange = new vscode.Range(fileDoc.positionAt(0), fileDoc.positionAt(otherContent.length));
            edit.replace(fileDoc.uri, otherRange, otherUpdated);
        }
        const success = await vscode.workspace.applyEdit(edit);
        if (success) {
            const successMsg = `Successfully updated version to ${newVersion} inside ${path.basename(selectedPath)}!`;
            logger.log(successMsg);
            vscode.window.setStatusBarMessage(successMsg, 5000);
            statusBar.update();
        }
        else {
            throw new Error('WorkspaceEdit transaction rejected by the editor.');
        }
    }
    catch (ex) {
        logger.log(`Error updating version: ${ex.message}`);
        vscode.window.showErrorMessage(`Failed to update version: ${ex.message}`);
    }
}
/**
 * Aligns all versions of project files to a specified version.
 */
async function alignProjectVersions(projectPath, targetVersion) {
    try {
        const projects = await (0, projectScanner_1.getWorkspaceDiagnostics)();
        const proj = projects.find(p => p.projectPath === projectPath);
        if (!proj) {
            return;
        }
        const edit = new vscode.WorkspaceEdit();
        for (const verDetail of proj.diagnostics.versions) {
            const handler = (0, handlerRegistry_1.getHandlerForFile)(verDetail.filePath);
            if (handler) {
                const doc = await vscode.workspace.openTextDocument(verDetail.filePath);
                const fileContent = doc.getText();
                const updatedContent = handler.updateVersion(fileContent, targetVersion);
                const range = new vscode.Range(doc.positionAt(0), doc.positionAt(fileContent.length));
                edit.replace(doc.uri, range, updatedContent);
            }
        }
        const success = await vscode.workspace.applyEdit(edit);
        if (success) {
            const successMsg = `Aligned all project files in ${proj.name} to version ${targetVersion}!`;
            logger.log(successMsg);
            vscode.window.setStatusBarMessage(successMsg, 5000);
            statusBar.update();
        }
    }
    catch (ex) {
        logger.log(`Failed to align project: ${ex.message}`);
        vscode.window.showErrorMessage(`Failed to align project versions: ${ex.message}`);
    }
}
function getSelectedPath(uri) {
    if (uri && uri.fsPath) {
        return uri.fsPath;
    }
    if (vscode.window.activeTextEditor) {
        return vscode.window.activeTextEditor.document.fileName;
    }
    return undefined;
}
//# sourceMappingURL=extension.js.map