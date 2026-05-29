"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getHandlerForFile = exports.Handlers = void 0;
const CsprojVersionHandler_1 = require("./CsprojVersionHandler");
const AssemblyInfoVersionHandler_1 = require("./AssemblyInfoVersionHandler");
const NuspecVersionHandler_1 = require("./NuspecVersionHandler");
const PackageJsonVersionHandler_1 = require("./PackageJsonVersionHandler");
const VsixManifestVersionHandler_1 = require("./VsixManifestVersionHandler");
const AppxManifestVersionHandler_1 = require("./AppxManifestVersionHandler");
const WxsVersionHandler_1 = require("./WxsVersionHandler");
const RcVersionHandler_1 = require("./RcVersionHandler");
exports.Handlers = [
    new CsprojVersionHandler_1.CsprojVersionHandler(),
    new AssemblyInfoVersionHandler_1.AssemblyInfoVersionHandler(),
    new NuspecVersionHandler_1.NuspecVersionHandler(),
    new PackageJsonVersionHandler_1.PackageJsonVersionHandler(),
    new VsixManifestVersionHandler_1.VsixManifestVersionHandler(),
    new AppxManifestVersionHandler_1.AppxManifestVersionHandler(),
    new WxsVersionHandler_1.WxsVersionHandler(),
    new RcVersionHandler_1.RcVersionHandler()
];
/**
 * Returns the first registered handler that can process the specified file path,
 * or null if none matches.
 * @param filePath The absolute file path.
 */
function getHandlerForFile(filePath) {
    for (const handler of exports.Handlers) {
        if (handler.canHandle(filePath)) {
            return handler;
        }
    }
    return null;
}
exports.getHandlerForFile = getHandlerForFile;
//# sourceMappingURL=handlerRegistry.js.map