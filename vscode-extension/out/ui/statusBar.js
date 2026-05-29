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
exports.VersionStatusBar = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const projectScanner_1 = require("../utils/projectScanner");
/**
 * Manages the VersionUp status bar item inside VS Code.
 */
class VersionStatusBar {
    statusBarItem;
    currentProjects = [];
    constructor() {
        this.statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
        this.statusBarItem.command = 'versionup.showProjectVersions';
        this.statusBarItem.text = '$(file-code) No version';
        this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
    }
    show() {
        this.statusBarItem.show();
    }
    hide() {
        this.statusBarItem.hide();
    }
    dispose() {
        this.statusBarItem.dispose();
    }
    /**
     * Re-scans workspace projects and updates the status bar state accordingly.
     */
    async update() {
        try {
            this.currentProjects = await (0, projectScanner_1.getWorkspaceDiagnostics)();
            const activeEditor = vscode.window.activeTextEditor;
            if (this.currentProjects.length === 0) {
                this.statusBarItem.text = '$(file-code) No version';
                this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
                this.statusBarItem.backgroundColor = undefined;
                return;
            }
            let activeProject;
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
            }
            else {
                this.statusBarItem.text = `$(file-code) ${activeProject.name}: ${primaryVersion}`;
                this.statusBarItem.tooltip = `Active Project Version: ${primaryVersion} (Click to see all project versions)`;
                this.statusBarItem.backgroundColor = undefined;
            }
        }
        catch {
            this.statusBarItem.text = '$(file-code) No version';
            this.statusBarItem.tooltip = 'Active Project Version (Click to see all project versions)';
            this.statusBarItem.backgroundColor = undefined;
        }
    }
}
exports.VersionStatusBar = VersionStatusBar;
//# sourceMappingURL=statusBar.js.map