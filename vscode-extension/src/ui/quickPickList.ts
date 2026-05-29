import * as vscode from 'vscode';
import { ProjectInfo } from '../utils/projectScanner';

interface VersionQuickPickItem extends vscode.QuickPickItem {
    action?: 'align' | 'initialize';
    projectPath?: string;
    targetVersion?: string;
}

/**
 * Renders the QuickPick dropdown showing all projects and version details.
 */
export async function showProjectVersionsQuickPick(
    projects: ProjectInfo[],
    onAlign: (projectPath: string, version: string) => Promise<void>
): Promise<void> {
    const items: VersionQuickPickItem[] = [];

    if (projects.length === 0) {
        items.push({
            label: 'No projects found in the workspace.',
            detail: 'Please open a directory containing supported project files.'
        });
    } else {
        for (const proj of projects) {
            const diag = proj.diagnostics;

            // Add project group item
            items.push({
                label: `$(file-submodule) ${proj.name}`,
                description: `Version: ${diag.primaryVersion || 'None'}`,
                detail: diag.isOutOfSync ? '$(warning) Out of sync! (See files below)' : '$(check) Synchronized',
                alwaysShow: true
            });

            if (diag.isOutOfSync) {
                for (const verDetail of diag.versions) {
                    items.push({
                        label: `   $(file-code) ${verDetail.sourceName}`,
                        description: `Version: ${verDetail.version}`,
                        detail: `      $(arrow-right) Click to align all project files to ${verDetail.version}`,
                        action: 'align',
                        projectPath: proj.projectPath,
                        targetVersion: verDetail.version
                    });
                }
            } else if (!diag.primaryVersion) {
                // Initializer row
                items.push({
                    label: '   $(add) Initialize version configuration',
                    description: 'Set version to 1.0.0',
                    detail: '      $(arrow-right) Add versioning to all project files',
                    action: 'initialize',
                    projectPath: proj.projectPath,
                    targetVersion: proj.projectPath.endsWith('.rc') || proj.projectPath.endsWith('package.appxmanifest') ? '1.0.0.0' : '1.0.0'
                });
            }
        }
    }

    const quickPick = vscode.window.createQuickPick<VersionQuickPickItem>();
    quickPick.title = 'Solution Project Versions';
    quickPick.placeholder = 'Search projects or click a file to align versions...';
    quickPick.items = items;
    quickPick.ignoreFocusOut = false;

    quickPick.onDidAccept(async () => {
        const selected = quickPick.selectedItems[0];
        if (selected && selected.action && selected.projectPath && selected.targetVersion) {
            quickPick.hide();
            await onAlign(selected.projectPath, selected.targetVersion);
        } else {
            quickPick.hide();
        }
    });

    quickPick.show();
}
