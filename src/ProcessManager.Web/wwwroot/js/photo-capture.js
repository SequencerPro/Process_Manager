async function compressImage(file, maxWidth, maxHeight, quality) {
    maxWidth = maxWidth || 1920;
    maxHeight = maxHeight || 1440;
    quality = quality || 0.8;

    return new Promise((resolve, reject) => {
        const img = new Image();
        const url = URL.createObjectURL(file);

        img.onload = () => {
            URL.revokeObjectURL(url);

            let w = img.width, h = img.height;
            if (w > maxWidth || h > maxHeight) {
                const ratio = Math.min(maxWidth / w, maxHeight / h);
                w = Math.round(w * ratio);
                h = Math.round(h * ratio);
            }

            const canvas = document.createElement('canvas');
            canvas.width = w;
            canvas.height = h;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, w, h);

            canvas.toBlob(blob => {
                if (blob) resolve(blob);
                else reject(new Error('Compression failed'));
            }, 'image/jpeg', quality);
        };

        img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('Failed to load image')); };
        img.src = url;
    });
}

async function captureAndCompress(inputElementId, maxWidth, maxHeight, quality) {
    const input = document.getElementById(inputElementId);
    if (!input || !input.files || input.files.length === 0) return null;

    const file = input.files[0];
    if (!file.type.startsWith('image/')) return null;

    const originalSizeKb = Math.round(file.size / 1024);

    const compressed = await compressImage(file, maxWidth, maxHeight, quality);
    const compressedSizeKb = Math.round(compressed.size / 1024);

    const reader = new FileReader();
    return new Promise((resolve) => {
        reader.onload = () => {
            resolve({
                base64: reader.result.split(',')[1],
                fileName: file.name.replace(/\.[^.]+$/, '.jpg'),
                mimeType: 'image/jpeg',
                originalSizeKb,
                compressedSizeKb
            });
        };
        reader.readAsDataURL(compressed);
    });
}

window.PhotoCapture = { captureAndCompress };
