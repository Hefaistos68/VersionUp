import { IVersionFileHandler } from './IVersionFileHandler';
import { CsprojVersionHandler } from './CsprojVersionHandler';
import { AssemblyInfoVersionHandler } from './AssemblyInfoVersionHandler';
import { NuspecVersionHandler } from './NuspecVersionHandler';
import { PackageJsonVersionHandler } from './PackageJsonVersionHandler';
import { VsixManifestVersionHandler } from './VsixManifestVersionHandler';
import { AppxManifestVersionHandler } from './AppxManifestVersionHandler';
import { WxsVersionHandler } from './WxsVersionHandler';
import { RcVersionHandler } from './RcVersionHandler';

export const Handlers: IVersionFileHandler[] = [
    new CsprojVersionHandler(),
    new AssemblyInfoVersionHandler(),
    new NuspecVersionHandler(),
    new PackageJsonVersionHandler(),
    new VsixManifestVersionHandler(),
    new AppxManifestVersionHandler(),
    new WxsVersionHandler(),
    new RcVersionHandler()
];

/**
 * Returns the first registered handler that can process the specified file path,
 * or null if none matches.
 * @param filePath The absolute file path.
 */
export function getHandlerForFile(filePath: string): IVersionFileHandler | null {
    for (const handler of Handlers) {
        if (handler.canHandle(filePath)) {
            return handler;
        }
    }
    return null;
}
