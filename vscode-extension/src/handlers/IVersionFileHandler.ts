/**
 * Defines the contract for parsing and updating version elements in specific file formats.
 */
export interface IVersionFileHandler {
    /**
     * Gets whether this handler can process the file at the specified path.
     * @param filePath The absolute path to the target file.
     */
    canHandle(filePath: string): boolean;

    /**
     * Parses the version string from the file content.
     * @param fileContent The raw content of the file.
     * @returns The parsed version string, or null if no version is defined.
     */
    getVersion(fileContent: string): string | null;

    /**
     * Updates the version string within the file content and returns the updated content.
     * @param fileContent The raw content of the file.
     * @param newVersion The new version string to write.
     * @returns The updated file content.
     */
    updateVersion(fileContent: string, newVersion: string): string;
}
