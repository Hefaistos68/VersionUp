import * as path from 'path';
import { IVersionFileHandler } from './IVersionFileHandler';

/**
 * Handles parsing and updating version elements in MSBuild project and properties files.
 */
export class CsprojVersionHandler implements IVersionFileHandler {
    public canHandle(filePath: string): boolean {
        if (!filePath) {
            return false;
        }

        const ext = path.extname(filePath).toLowerCase();
        const filename = path.basename(filePath).toLowerCase();

        const isProject = ext === '.csproj' || ext === '.fsproj' || ext === '.vbproj';
        const isBuildProps = filename === 'directory.build.props' || filename === 'directory.build.targets';

        return isProject || isBuildProps;
    }

    public getVersion(fileContent: string): string | null {
        if (!fileContent || !fileContent.trim()) {
            return null;
        }

        const versionMatch = fileContent.match(/<Version>\s*([^<\s]+)\s*<\/Version>/i);
        if (versionMatch) {
            return versionMatch[1];
        }

        const packageVersionMatch = fileContent.match(/<PackageVersion>\s*([^<\s]+)\s*<\/PackageVersion>/i);
        if (packageVersionMatch) {
            return packageVersionMatch[1];
        }

        return null;
    }

    public updateVersion(fileContent: string, newVersion: string): string {
        if (!fileContent || !fileContent.trim()) {
            return `<Project>\n  <PropertyGroup>\n    <Version>${newVersion}</Version>\n  </PropertyGroup>\n</Project>`;
        }

        if (/<Version>\s*[^<\s]+\s*<\/Version>/i.test(fileContent)) {
            return fileContent.replace(/(<Version>\s*)[^<\s]+(\s*<\/Version>)/i, `$1${newVersion}$2`);
        }

        if (/<PackageVersion>\s*[^<\s]+\s*<\/PackageVersion>/i.test(fileContent)) {
            return fileContent.replace(/(<PackageVersion>\s*)[^<\s]+(\s*<\/PackageVersion>)/i, `$1${newVersion}$2`);
        }

        const propertyGroupMatch = fileContent.match(/<PropertyGroup>/i);
        if (propertyGroupMatch && propertyGroupMatch.index !== undefined) {
            const insertIndex = propertyGroupMatch.index + propertyGroupMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n    <Version>${newVersion}</Version>` + fileContent.slice(insertIndex);
        }

        const projectMatch = fileContent.match(/<Project[^>]*>/i);
        if (projectMatch && projectMatch.index !== undefined) {
            const insertIndex = projectMatch.index + projectMatch[0].length;
            return fileContent.slice(0, insertIndex) + `\n  <PropertyGroup>\n    <Version>${newVersion}</Version>\n  </PropertyGroup>` + fileContent.slice(insertIndex);
        }

        return fileContent;
    }
}
