let activeStream = null;
let activeDetector = null;
let scanCallback = null;
let scanning = false;

async function startScan(dotnetRef, videoElementId) {
    scanCallback = dotnetRef;
    const video = document.getElementById(videoElementId);
    if (!video) return false;

    try {
        activeStream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
        });
        video.srcObject = activeStream;
        await video.play();
    } catch {
        return false;
    }

    scanning = true;

    if ('BarcodeDetector' in window) {
        activeDetector = new BarcodeDetector({ formats: ['code_128', 'code_39', 'ean_13', 'ean_8', 'qr_code', 'data_matrix', 'upc_a', 'upc_e'] });
        detectLoop(video);
    }

    return true;
}

async function detectLoop(video) {
    if (!scanning || !activeDetector) return;

    try {
        const barcodes = await activeDetector.detect(video);
        if (barcodes.length > 0) {
            const value = barcodes[0].rawValue;
            if (scanCallback) {
                await scanCallback.invokeMethodAsync('OnBarcodeDetected', value);
            }
            stopScan();
            return;
        }
    } catch { /* detection frame error, continue */ }

    if (scanning) requestAnimationFrame(() => detectLoop(video));
}

function stopScan() {
    scanning = false;
    activeDetector = null;
    if (activeStream) {
        activeStream.getTracks().forEach(t => t.stop());
        activeStream = null;
    }
    scanCallback = null;
}

function isSupported() {
    return 'BarcodeDetector' in window && navigator.mediaDevices && typeof navigator.mediaDevices.getUserMedia === 'function';
}

window.BarcodeScanner = { startScan, stopScan, isSupported };
