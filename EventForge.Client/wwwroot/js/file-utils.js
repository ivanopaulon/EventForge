/**
 * Utility functions for file operations
 */

/**
 * Downloads a file from a data URL
 * @param {string} filename - The name of the file to download
 * @param {string} dataUrl - The data URL containing the file content
 */
window.downloadFile = function (filename, dataUrl) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = filename;
    link.style.display = 'none';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

/**
 * Downloads content as a file
 * @param {string} filename - The name of the file to download
 * @param {string} content - The content to download
 * @param {string} mimeType - The MIME type (default: text/plain)
 */
window.downloadContent = function (filename, content, mimeType = 'text/plain') {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    // Clean up the object URL
    URL.revokeObjectURL(url);
};

/**
 * Downloads a CSV file
 * @param {string} filename - The name of the CSV file
 * @param {string} csvContent - The CSV content
 */
window.downloadCsv = function (filename, csvContent) {
    window.downloadContent(filename, csvContent, 'text/csv;charset=utf-8;');
};