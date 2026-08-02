window.conatradecFiles = {
    downloadFromBase64: function (fileName, contentType, base64Data) {
        if (!base64Data) {
            throw new Error("El archivo recibido está vacío.");
        }

        const binary = window.atob(base64Data);
        const bytes = new Uint8Array(binary.length);

        for (let index = 0; index < binary.length; index++) {
            bytes[index] = binary.charCodeAt(index);
        }

        const blob = new Blob(
            [bytes],
            {
                type: contentType || "application/octet-stream"
            });

        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement("a");

        link.href = objectUrl;
        link.download = fileName || "archivo";
        link.style.display = "none";

        document.body.appendChild(link);
        link.click();
        link.remove();

        window.setTimeout(
            function () {
                URL.revokeObjectURL(objectUrl);
            },
            1000);
    }
};
