import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating the package identity version in AppX packaging manifests.
 */
export class AppxManifestVersionHandler implements IVersionFileHandler {
    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const fileName = path.basename(filePath).toLowerCase();
        return fileName === 'package.appxmanifest';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        const match = fileContent.match(/<Identity[^>]*\s+Version="([^"]+)"/i);
        if (match) {
            return this.normalizeToFourSegments(match[1]);
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }

        const normalized = this.normalizeToFourSegments(newVersion);
        if (/<Identity[^>]*\s+Version="([^"]+)"/i.test(fileContent)) {
            return fileContent.replace(/(<Identity[^>]*\s+Version=")[^"]+(")/i, `$1${normalized}$2`);
        }

        return fileContent;
    }

    private normalizeToFourSegments(version: string): string {
        if (!version) {
            return "1.0.0.0";
        }

        const parts = version.split('.');
        const segments: string[] = [];
        for (let i = 0; i < 4; i++) {
            if (i < parts.length) {
                segments.push(parts[i]);
            } else {
                segments.push("0");
            }
        }

        return segments.join(".");
    }
}
