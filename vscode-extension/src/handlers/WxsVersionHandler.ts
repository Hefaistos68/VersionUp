import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating the version attribute in WiX installer setup (.wxs) files.
 */
export class WxsVersionHandler implements IVersionFileHandler {
    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const ext = path.extname(filePath).toLowerCase();
        return ext === '.wxs';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        const match = fileContent.match(/<(Product|Package|Module)[^>]*\s+Version="([^"]+)"/i);
        if (match) {
            return match[2];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }

        if (/<(Product|Package|Module)[^>]*\s+Version="([^"]+)"/i.test(fileContent)) {
            return fileContent.replace(/(<(Product|Package|Module)[^>]*\s+Version=")[^"]+(")/i, `$1${newVersion}$3`);
        }

        return fileContent;
    }
}
