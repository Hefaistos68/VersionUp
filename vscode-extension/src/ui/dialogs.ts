import * as vscode from 'vscode';

/**
 * Prompts the user to enter a custom version string.
 * @param currentVersion The current version to prefill.
 */
export async function promptSetVersion(currentVersion: string): Promise<string | undefined> {
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

export type AlignmentChoice = 'Yes' | 'No' | 'Always' | 'Never';

/**
 * Prompts the user to confirm if they want to align all files in the project.
 */
export async function promptAlignment(projectName: string, newVersion: string): Promise<AlignmentChoice | undefined> {
    const message = `Incrementing version in ${projectName} to ${newVersion}. Do you want to update all other versioned files in this project to the same version?`;
    const options: vscode.MessageItem[] = [
        { title: 'Yes', isCloseAffordance: false },
        { title: 'No', isCloseAffordance: false },
        { title: 'Always for this Project', isCloseAffordance: false },
        { title: 'Never for this Project', isCloseAffordance: false }
    ];

    const selection = await vscode.window.showInformationMessage(message, { modal: false }, ...options);
    if (!selection) {
        return undefined;
    }

    if (selection.title === 'Yes') { return 'Yes'; }
    if (selection.title === 'No') { return 'No'; }
    if (selection.title === 'Always for this Project') { return 'Always'; }
    return 'Never';
}
