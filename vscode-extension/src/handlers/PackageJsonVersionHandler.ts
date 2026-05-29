import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating the version property in package.json files.
 */
export class PackageJsonVersionHandler implements IVersionFileHandler {
    private static readonly versionRegex = /"version"\s*:\s*"([^"]+)"/gi;

    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const fileName = path.basename(filePath).toLowerCase();
        return fileName === 'package.json';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        PackageJsonVersionHandler.versionRegex.lastIndex = 0;
        const match = PackageJsonVersionHandler.versionRegex.exec(fileContent);
        if (match) {
            return match[1];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return `{\n  "version": "${newVersion}"\n}`;
        }

        PackageJsonVersionHandler.versionRegex.lastIndex = 0;
        return fileContent.replace(PackageJsonVersionHandler.versionRegex, (match, rawValue) => {
            const index = match.indexOf(rawValue);
            if (index >= 0) {
                return match.slice(0, index) + newVersion + match.slice(index + rawValue.length);
            }
            return match;
        });
    }
}
