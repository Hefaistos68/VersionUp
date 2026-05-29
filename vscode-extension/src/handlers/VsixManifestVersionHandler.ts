import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating the identity version in Visual Studio VSIX extension manifests.
 */
export class VsixManifestVersionHandler implements IVersionFileHandler {
    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const fileName = path.basename(filePath).toLowerCase();
        return fileName === 'source.extension.vsixmanifest';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        const match = fileContent.match(/<Identity[^>]*\s+Version="([^"]+)"/i);
        if (match) {
            return match[1];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }

        if (/<Identity[^>]*\s+Version="([^"]+)"/i.test(fileContent)) {
            return fileContent.replace(/(<Identity[^>]*\s+Version=")[^"]+(")/i, `$1${newVersion}$2`);
        }

        return fileContent;
    }
}
