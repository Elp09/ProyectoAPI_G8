// Placeholder download function for catalog items
function downloadPlaceholder(filename) {
    var sampleData = { placeholder: true, filename: filename, message: "This is sample data. Real data will come from API ingestion." };
    var blob = new Blob([JSON.stringify(sampleData, null, 2)], { type: "application/json" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
