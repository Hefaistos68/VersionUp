import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating versions in native C++ resource script (.rc) files.
 */
export class RcVersionHandler implements IVersionFileHandler {
    private static readonly fileVersionKeywordRegex = /FILEVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+/gi;
    private static readonly productVersionKeywordRegex = /PRODUCTVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+/gi;
    private static readonly fileVersionValueRegex = /(VALUE\s+"FileVersion"\s*,\s*")([^"]+)(")/gi;
    private static readonly productVersionValueRegex = /(VALUE\s+"ProductVersion"\s*,\s*")([^"]+)(")/gi;

    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const ext = path.extname(filePath).toLowerCase();
        return ext === '.rc';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        RcVersionHandler.fileVersionValueRegex.lastIndex = 0;
        const valueMatch = RcVersionHandler.fileVersionValueRegex.exec(fileContent);
        if (valueMatch) {
            return valueMatch[2];
        }

        RcVersionHandler.fileVersionKeywordRegex.lastIndex = 0;
        const keywordMatch = RcVersionHandler.fileVersionKeywordRegex.exec(fileContent);
        if (keywordMatch) {
            const raw = keywordMatch[0];
            const rawNumbers = raw.replace(/[^\d,]/g, '');
            return rawNumbers.replace(/,/g, '.');
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }

        const parts = newVersion.split('.');
        const segments: string[] = [];
        for (let i = 0; i < 4; i++) {
            if (i < parts.length) {
                segments.push(parts[i]);
            } else {
                segments.push("0");
            }
        }
        const commaVersion = segments.join(',');

        let result = fileContent;
        result = result.replace(RcVersionHandler.fileVersionKeywordRegex, `FILEVERSION ${commaVersion}`);
        result = result.replace(RcVersionHandler.productVersionKeywordRegex, `PRODUCTVERSION ${commaVersion}`);
        result = result.replace(RcVersionHandler.fileVersionValueRegex, `$1${newVersion}$3`);
        result = result.replace(RcVersionHandler.productVersionValueRegex, `$1${newVersion}$3`);

        return result;
    }
}
