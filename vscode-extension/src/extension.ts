import * as vscode from 'vscode';
import * as path from 'path';
import { VersionSegment, VersionIncrementer } from './utils/versionIncrementer';
import { VersionLogger } from './utils/logger';
import { getHandlerForFile } from './handlers/handlerRegistry';
import { getWorkspaceDiagnostics } from './utils/projectScanner';
import { VersionStatusBar } from './ui/statusBar';
import { showProjectVersionsQuickPick } from './ui/quickPickList';
import { promptSetVersion, promptAlignment } from './ui/dialogs';

let logger: VersionLogger;
let statusBar: VersionStatusBar;

export function activate(context: vscode.ExtensionContext) {
    logger = new VersionLogger();
    logger.log('VersionUp extension has been activated.');

    statusBar = new VersionStatusBar();
    statusBar.show();

    // Register active editor listeners to refresh status bar
    context.subscriptions.push(
        vscode.window.onDidChangeActiveTextEditor(() => statusBar.update()),
        vscode.workspace.onDidSaveTextDocument(() => statusBar.update()),
        statusBar
    );

    // Initial status bar update
    statusBar.update();

    // Helper to execute segment increments
    const runIncrement = async (uri: vscode.Uri | undefined, segment: VersionSegment) => {
        await executeVersionCommand(uri, async (currentVersion) => {
            const incrementer = new VersionIncrementer(logger);
            return incrementer.increment(currentVersion, segment);
        });
    };

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('versionup.incrementMajor', (uri) => runIncrement(uri, VersionSegment.Major)),
        vscode.commands.registerCommand('versionup.incrementMinor', (uri) => runIncrement(uri, VersionSegment.Minor)),
        vscode.commands.registerCommand('versionup.incrementBuild', (uri) => runIncrement(uri, VersionSegment.Build)),
        vscode.commands.registerCommand('versionup.incrementRevision', (uri) => runIncrement(uri, VersionSegment.Revision)),
        
        vscode.commands.registerCommand('versionup.setVersion', async (uri) => {
            await executeVersionCommand(uri, async (currentVersion) => {
                const newVersion = await promptSetVersion(currentVersion);
                return newVersion; // undefined if cancelled
            });
        }),

        vscode.commands.registerCommand('versionup.showProjectVersions', async () => {
            try {
                const projects = await getWorkspaceDiagnostics();
                await showProjectVersionsQuickPick(projects, async (projectPath, targetVersion) => {
                    await alignProjectVersions(projectPath, targetVersion);
                });
            } catch (ex: any) {
                logger.log(`Error showing project list: ${ex.message}`);
                vscode.window.showErrorMessage(`Failed to display project list: ${ex.message}`);
            }
        })
    );
}

export function deactivate() {
    if (statusBar) {
        statusBar.dispose();
    }
}

/**
 * Executes a version command (increment or set) on the selected or active file.
 */
async function executeVersionCommand(
    uri: vscode.Uri | undefined,
    getVersionUpdate: (current: string) => Promise<string | undefined>
): Promise<void> {
    const selectedPath = getSelectedPath(uri);
    if (!selectedPath) {
        vscode.window.showWarningMessage('No active project or file selected for version update.');
        return;
    }

    const handler = getHandlerForFile(selectedPath);
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
        const projects = await getWorkspaceDiagnostics();
        const matchedProj = projects.find(p => {
            const isFile = p.projectPath.endsWith('.json') || 
                           p.projectPath.endsWith('.csproj') || 
                           p.projectPath.endsWith('.fsproj') || 
                           p.projectPath.endsWith('.vbproj');
            const projDir = isFile ? path.dirname(p.projectPath) : p.projectPath;
            return selectedPath.startsWith(projDir + path.sep) || selectedPath === p.projectPath;
        });

        let alignFiles: { filePath: string; handler: any }[] = [];
        if (matchedProj && matchedProj.diagnostics.versions.length > 1) {
            const decisions = vscode.workspace.getConfiguration('versionup').get<Record<string, string>>('alignmentDecisions') || {};
            const savedDecision = decisions[matchedProj.projectPath];

            let shouldAlign = false;
            if (savedDecision === 'Always') {
                shouldAlign = true;
            } else if (savedDecision !== 'Never') {
                const choice = await promptAlignment(matchedProj.name, newVersion);
                if (choice === 'Always') {
                    shouldAlign = true;
                    decisions[matchedProj.projectPath] = 'Always';
                    await vscode.workspace.getConfiguration('versionup').update('alignmentDecisions', decisions, vscode.ConfigurationTarget.Workspace);
                } else if (choice === 'Never') {
                    shouldAlign = false;
                    decisions[matchedProj.projectPath] = 'Never';
                    await vscode.workspace.getConfiguration('versionup').update('alignmentDecisions', decisions, vscode.ConfigurationTarget.Workspace);
                } else if (choice === 'Yes') {
                    shouldAlign = true;
                }
            }

            if (shouldAlign) {
                for (const verDetail of matchedProj.diagnostics.versions) {
                    if (verDetail.filePath !== selectedPath) {
                        const fileHandler = getHandlerForFile(verDetail.filePath);
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
        } else {
            throw new Error('WorkspaceEdit transaction rejected by the editor.');
        }

    } catch (ex: any) {
        logger.log(`Error updating version: ${ex.message}`);
        vscode.window.showErrorMessage(`Failed to update version: ${ex.message}`);
    }
}

/**
 * Aligns all versions of project files to a specified version.
 */
async function alignProjectVersions(projectPath: string, targetVersion: string): Promise<void> {
    try {
        const projects = await getWorkspaceDiagnostics();
        const proj = projects.find(p => p.projectPath === projectPath);
        if (!proj) {
            return;
        }

        const edit = new vscode.WorkspaceEdit();
        for (const verDetail of proj.diagnostics.versions) {
            const handler = getHandlerForFile(verDetail.filePath);
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
    } catch (ex: any) {
        logger.log(`Failed to align project: ${ex.message}`);
        vscode.window.showErrorMessage(`Failed to align project versions: ${ex.message}`);
    }
}

function getSelectedPath(uri: vscode.Uri | undefined): string | undefined {
    if (uri && uri.fsPath) {
        return uri.fsPath;
    }
    if (vscode.window.activeTextEditor) {
        return vscode.window.activeTextEditor.document.fileName;
    }
    return undefined;
}
