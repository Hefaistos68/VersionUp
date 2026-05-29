import * as vscode from 'vscode';

export interface IVersionLogger {
    log(message: string): void;
    show(): void;
}

export class VersionLogger implements IVersionLogger {
    private outputChannel: vscode.OutputChannel;

    constructor() {
        this.outputChannel = vscode.window.createOutputChannel('VersionUp');
    }

    public log(message: string): void {
        const timestamp = new Date().toISOString();
        this.outputChannel.appendLine(`[${timestamp}] [VersionUp] ${message}`);
    }

    public show(): void {
        this.outputChannel.show(true);
    }
}
