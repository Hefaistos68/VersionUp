import * as vscode from 'vscode';
import * as path from 'path';
import { ProjectInfo, getWorkspaceDiagnostics } from '../utils/projectScanner';

/**
 * Manages the VersionUp status bar item inside VS Code.
 */
export class VersionStatusBar {
    private statusBarItem: vscode.StatusBarItem;
    private currentProjects: ProjectInfo[] = [];

    constructor() {
        this.statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
        this.statusBarItem.command = 'versionup.showProjectVersions';
        this.statusBarItem.text = '$(file-code) No version';
        this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
    }

    public show(): void {
        this.statusBarItem.show();
    }

    public hide(): void {
        this.statusBarItem.hide();
    }

    public dispose(): void {
        this.statusBarItem.dispose();
    }

    /**
     * Re-scans workspace projects and updates the status bar state accordingly.
     */
    public async update(): Promise<void> {
        try {
            this.currentProjects = await getWorkspaceDiagnostics();
            const activeEditor = vscode.window.activeTextEditor;

            if (this.currentProjects.length === 0) {
                this.statusBarItem.text = '$(file-code) No version';
                this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
                this.statusBarItem.backgroundColor = undefined;
                return;
            }

            let activeProject: ProjectInfo | undefined;
            if (activeEditor) {
                const activePath = activeEditor.document.fileName;
                activeProject = this.currentProjects.find(proj => {
                    const isFile = proj.projectPath.endsWith('.json') || 
                                   proj.projectPath.endsWith('.csproj') || 
                                   proj.projectPath.endsWith('.fsproj') || 
                                   proj.projectPath.endsWith('.vbproj');
                    const projDir = isFile ? path.dirname(proj.projectPath) : proj.projectPath;
                    return activePath.startsWith(projDir + path.sep) || activePath === proj.projectPath;
                });
            }

            if (!activeProject) {
                activeProject = this.currentProjects[0];
            }

            const diag = activeProject.diagnostics;
            const primaryVersion = diag.primaryVersion || 'No version';

            if (diag.isOutOfSync) {
                this.statusBarItem.text = `$(warning) ${activeProject.name}: ${primaryVersion}`;
                this.statusBarItem.tooltip = `Active Project Version: ${primaryVersion} (Out of Sync!) (Click to see details)`;
                this.statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.warningBackground');
            } else {
                this.statusBarItem.text = `$(file-code) ${activeProject.name}: ${primaryVersion}`;
                this.statusBarItem.tooltip = `Active Project Version: ${primaryVersion} (Click to see all project versions)`;
                this.statusBarItem.backgroundColor = undefined;
            }
        } catch {
            this.statusBarItem.text = '$(file-code) No version';
            this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
            this.statusBarItem.backgroundColor = undefined;
        }
    }
}
