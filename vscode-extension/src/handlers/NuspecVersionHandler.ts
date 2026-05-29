import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating version elements in NuGet specification (.nuspec) files.
 */
export class NuspecVersionHandler implements IVersionFileHandler {
    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const extension = path.extname(filePath).toLowerCase();
        return extension === '.nuspec';
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        const versionMatch = fileContent.match(/<version>\s*([^<\s]+)\s*<\/version>/i);
        if (versionMatch) {
            return versionMatch[1];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return `<package>\n  <metadata>\n    <version>${newVersion}</version>\n  </metadata>\n</package>`;
        }

        if (/<version>\s*[^<\s]+\s*<\/version>/i.test(fileContent)) {
            return fileContent.replace(/(<version>\s*)[^<\s]+(\s*<\/version>)/i, `$1${newVersion}$2`);
        }

        const metadataMatch = fileContent.match(/<metadata>/i);
        if (metadataMatch && metadataMatch.index !== undefined) {
            const insertIndex = metadataMatch.index + metadataMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n    <version>${newVersion}</version>` + fileContent.slice(insertIndex);
        }

        const packageMatch = fileContent.match(/<package[^>]*>/i);
        if (packageMatch && packageMatch.index !== undefined) {
            const insertIndex = packageMatch.index + packageMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n  <metadata>\n    <version>${newVersion}</version>\n  </metadata>` + fileContent.slice(insertIndex);
        }

        return fileContent;
    }
}
