import { VersionIncrementer, VersionSegment } from '../utils/versionIncrementer';
import { CsprojVersionHandler } from '../handlers/CsprojVersionHandler';
import { AssemblyInfoVersionHandler } from '../handlers/AssemblyInfoVersionHandler';
import { PackageJsonVersionHandler } from '../handlers/PackageJsonVersionHandler';
import { NuspecVersionHandler } from '../handlers/NuspecVersionHandler';
import { AppxManifestVersionHandler } from '../handlers/AppxManifestVersionHandler';
import { RcVersionHandler } from '../handlers/RcVersionHandler';
import { VsixManifestVersionHandler } from '../handlers/VsixManifestVersionHandler';
import { WxsVersionHandler } from '../handlers/WxsVersionHandler';

const logger = {
    log: (_msg: string) => {}
};

function assert(condition: boolean, message: string) {
    if (!condition) {
        throw new Error(`Assertion Failed: ${message}`);
    }
}

export function runAllTests() {
    console.log('Running VersionUp Unit Tests...');

    // 1. VersionIncrementer tests
    console.log(' - Testing VersionIncrementer...');
    const incrementer = new VersionIncrementer(logger);
    
    assert(incrementer.increment('1.2.3', VersionSegment.Major) === '2.0.0', 'Major increment');
    assert(incrementer.increment('1.2.3.4', VersionSegment.Minor) === '1.3.0', 'Minor increment');
    assert(incrementer.increment('1.2.3.4', VersionSegment.Build) === '1.2.4', 'Build increment');
    assert(incrementer.increment('1.2.3.4', VersionSegment.Revision) === '1.2.3.5', 'Revision increment');
    assert(incrementer.increment('invalid', VersionSegment.Minor) === '1.0.0', 'Invalid version fallback');

    // 2. CsprojVersionHandler tests
    console.log(' - Testing CsprojVersionHandler...');
    const csproj = new CsprojVersionHandler();
    const csprojContent = `<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <Version>1.2.3</Version>\n  </PropertyGroup>\n</Project>`;
    assert(csproj.canHandle('test.csproj'), 'CanHandle csproj');
    assert(csproj.canHandle('Directory.Build.props'), 'CanHandle props');
    assert(csproj.getVersion(csprojContent) === '1.2.3', 'GetVersion csproj');
    assert(csproj.updateVersion(csprojContent, '1.2.4').includes('<Version>1.2.4</Version>'), 'UpdateVersion csproj');

    // 3. AssemblyInfoVersionHandler tests
    console.log(' - Testing AssemblyInfoVersionHandler...');
    const assemblyInfo = new AssemblyInfoVersionHandler();
    const assemblyContent = `[assembly: AssemblyVersion("1.0.2.3")]\n[assembly: AssemblyFileVersion("1.0.2.3")]`;
    assert(assemblyInfo.canHandle('AssemblyInfo.cs'), 'CanHandle AssemblyInfo.cs');
    assert(assemblyInfo.getVersion(assemblyContent) === '1.0.2.3', 'GetVersion AssemblyInfo');
    assert(assemblyInfo.updateVersion(assemblyContent, '2.0.0.0').includes('"2.0.0.0"'), 'UpdateVersion AssemblyInfo');

    // 4. PackageJsonVersionHandler tests
    console.log(' - Testing PackageJsonVersionHandler...');
    const packageJson = new PackageJsonVersionHandler();
    const packageJsonContent = `{\n  "name": "test",\n  "version": "1.0.0"\n}`;
    assert(packageJson.canHandle('package.json'), 'CanHandle package.json');
    assert(packageJson.getVersion(packageJsonContent) === '1.0.0', 'GetVersion package.json');
    assert(packageJson.updateVersion(packageJsonContent, '1.1.0').includes('"version": "1.1.0"'), 'UpdateVersion package.json');

    // 5. NuspecVersionHandler tests
    console.log(' - Testing NuspecVersionHandler...');
    const nuspec = new NuspecVersionHandler();
    const nuspecContent = `<package><metadata><version>1.5.0</version></metadata></package>`;
    assert(nuspec.canHandle('test.nuspec'), 'CanHandle nuspec');
    assert(nuspec.getVersion(nuspecContent) === '1.5.0', 'GetVersion nuspec');
    assert(nuspec.updateVersion(nuspecContent, '1.6.0').includes('<version>1.6.0</version>'), 'UpdateVersion nuspec');

    // 6. AppxManifestVersionHandler tests
    console.log(' - Testing AppxManifestVersionHandler...');
    const appx = new AppxManifestVersionHandler();
    const appxContent = `<Identity Name="Test" Version="1.2.3" />`;
    assert(appx.canHandle('package.appxmanifest'), 'CanHandle appxmanifest');
    assert(appx.getVersion(appxContent) === '1.2.3.0', 'GetVersion appx (normalized)');
    assert(appx.updateVersion(appxContent, '2.0.0').includes('Version="2.0.0.0"'), 'UpdateVersion appx (normalized)');

    // 7. RcVersionHandler tests
    console.log(' - Testing RcVersionHandler...');
    const rc = new RcVersionHandler();
    const rcContent = `FILEVERSION 1,2,3,4\nPRODUCTVERSION 1,2,3,4\nVALUE "FileVersion", "1.2.3.4"\nVALUE "ProductVersion", "1.2.3.4"`;
    assert(rc.canHandle('resource.rc'), 'CanHandle resource.rc');
    assert(rc.getVersion(rcContent) === '1.2.3.4', 'GetVersion rc');
    assert(rc.updateVersion(rcContent, '2.0.0.0').includes('FILEVERSION 2,0,0,0'), 'UpdateVersion rc keyword');
    assert(rc.updateVersion(rcContent, '2.0.0.0').includes('VALUE "FileVersion", "2.0.0.0"'), 'UpdateVersion rc value');

    // 8. VsixManifestVersionHandler tests
    console.log(' - Testing VsixManifestVersionHandler...');
    const vsix = new VsixManifestVersionHandler();
    const vsixContent = `<Identity Id="Test" Version="1.0.0" />`;
    assert(vsix.canHandle('source.extension.vsixmanifest'), 'CanHandle vsixmanifest');
    assert(vsix.getVersion(vsixContent) === '1.0.0', 'GetVersion vsix');
    assert(vsix.updateVersion(vsixContent, '1.1.0').includes('Version="1.1.0"'), 'UpdateVersion vsix');

    // 9. WxsVersionHandler tests
    console.log(' - Testing WxsVersionHandler...');
    const wxs = new WxsVersionHandler();
    const wxsContent = `<Product Id="*" Version="1.0.0.0" />`;
    assert(wxs.canHandle('setup.wxs'), 'CanHandle setup.wxs');
    assert(wxs.getVersion(wxsContent) === '1.0.0.0', 'GetVersion wxs');
    assert(wxs.updateVersion(wxsContent, '2.0.0.0').includes('Version="2.0.0.0"'), 'UpdateVersion wxs');

    console.log('All tests completed successfully!');
}
