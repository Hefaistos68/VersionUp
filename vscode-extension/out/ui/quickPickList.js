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
exports.showProjectVersionsQuickPick = void 0;
const vscode = __importStar(require("vscode"));
/**
 * Renders the QuickPick dropdown showing all projects and version details.
 */
async function showProjectVersionsQuickPick(projects, onAlign) {
    const items = [];
    if (projects.length === 0) {
        items.push({
            label: 'No projects found in the workspace.',
            detail: 'Please open a directory containing supported project files.'
        });
    }
    else {
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
            }
            else if (!diag.primaryVersion) {
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
    const quickPick = vscode.window.createQuickPick();
    quickPick.title = 'Solution Project Versions';
    quickPick.placeholder = 'Search projects or click a file to align versions...';
    quickPick.items = items;
    quickPick.ignoreFocusOut = false;
    quickPick.onDidAccept(async () => {
        const selected = quickPick.selectedItems[0];
        if (selected && selected.action && selected.projectPath && selected.targetVersion) {
            quickPick.hide();
            await onAlign(selected.projectPath, selected.targetVersion);
        }
        else {
            quickPick.hide();
        }
    });
    quickPick.show();
}
exports.showProjectVersionsQuickPick = showProjectVersionsQuickPick;
//# sourceMappingURL=quickPickList.js.map