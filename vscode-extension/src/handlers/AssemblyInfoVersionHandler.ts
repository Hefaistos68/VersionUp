import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating version attributes in C#, VB, and F# AssemblyInfo files.
 */
export class AssemblyInfoVersionHandler implements IVersionFileHandler {
    private static readonly versionRegex = /(?:\[<assembly:\s*|\[assembly:\s*|<Assembly:\s*)(AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\("([^"]+)"\)(?:\]>|\]|>)/gi;

    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const fileName = path.basename(filePath).toLowerCase();
        const ext = path.extname(filePath).toLowerCase();

        const isAssemblyInfo = fileName.startsWith('assemblyinfo');
        const isSupportedExtension = ext === '.cs' || ext === '.vb' || ext === '.fs';

        return isAssemblyInfo && isSupportedExtension;
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        AssemblyInfoVersionHandler.versionRegex.lastIndex = 0;
        const matches = [...fileContent.matchAll(AssemblyInfoVersionHandler.versionRegex)];

        for (const match of matches) {
            if (match[1].toLowerCase() === 'assemblyversion') {
                return match[2];
            }
        }

        for (const match of matches) {
            if (match[1].toLowerCase() === 'assemblyfileversion') {
                return match[2];
            }
        }

        if (matches.length > 0) {
            return matches[0][2];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return fileContent;
        }

        AssemblyInfoVersionHandler.versionRegex.lastIndex = 0;
        return fileContent.replace(AssemblyInfoVersionHandler.versionRegex, (match, _attributeName, rawValue) => {
            const index = match.indexOf(rawValue);
            if (index >= 0) {
                return match.slice(0, index) + newVersion + match.slice(index + rawValue.length);
            }
            return match;
        });
    }
}
