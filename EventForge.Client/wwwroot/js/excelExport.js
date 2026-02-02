window.downloadFileFromBytes = function(fileName, byteArray, contentType) {
    contentType = contentType || 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    const blob = new Blob([new Uint8Array(byteArray)], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};
