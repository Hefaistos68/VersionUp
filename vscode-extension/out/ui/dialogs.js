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
exports.promptAlignment = exports.promptSetVersion = void 0;
const vscode = __importStar(require("vscode"));
/**
 * Prompts the user to enter a custom version string.
 * @param currentVersion The current version to prefill.
 */
async function promptSetVersion(currentVersion) {
    const result = await vscode.window.showInputBox({
        title: 'VersionUp: Set Version',
        prompt: 'Enter a valid version format (e.g. 1.0.0 or 1.0.0.0)',
        value: currentVersion,
        valueSelection: [0, currentVersion.length],
        ignoreFocusOut: true,
        validateInput: (value) => {
            const input = value.trim();
            if (!input) {
                return 'Version cannot be empty.';
            }
            const match = input.match(/^(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?$/);
            if (!match) {
                return 'Please enter a valid version format (e.g. 1.0.0 or 1.0.0.0).';
            }
            return null;
        }
    });
    return result?.trim();
}
exports.promptSetVersion = promptSetVersion;
/**
 * Prompts the user to confirm if they want to align all files in the project.
 */
async function promptAlignment(projectName, newVersion) {
    const message = `Incrementing version in ${projectName} to ${newVersion}. Do you want to update all other versioned files in this project to the same version?`;
    const options = [
        { title: 'Yes', isCloseAffordance: false },
        { title: 'No', isCloseAffordance: false },
        { title: 'Always for this Project', isCloseAffordance: false },
        { title: 'Never for this Project', isCloseAffordance: false }
    ];
    const selection = await vscode.window.showInformationMessage(message, { modal: false }, ...options);
    if (!selection) {
        return undefined;
    }
    if (selection.title === 'Yes') {
        return 'Yes';
    }
    if (selection.title === 'No') {
        return 'No';
    }
    if (selection.title === 'Always for this Project') {
        return 'Always';
    }
    return 'Never';
}
exports.promptAlignment = promptAlignment;
//# sourceMappingURL=dialogs.js.map