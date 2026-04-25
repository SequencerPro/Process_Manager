const pads = {};

function init(canvasId, dotnetRef, opts) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;

    const ctx = canvas.getContext('2d');
    const lineWidth = (opts && opts.lineWidth) || 3;
    const strokeColor = (opts && opts.strokeColor) || '#000000';

    const state = {
        canvas,
        ctx,
        dotnetRef,
        drawing: false,
        points: [],
        strokes: [],
        lineWidth,
        strokeColor
    };

    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = lineWidth;

    const getPos = (e) => {
        const rect = canvas.getBoundingClientRect();
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;
        if (e.touches && e.touches.length > 0) {
            return { x: (e.touches[0].clientX - rect.left) * scaleX, y: (e.touches[0].clientY - rect.top) * scaleY };
        }
        return { x: (e.clientX - rect.left) * scaleX, y: (e.clientY - rect.top) * scaleY };
    };

    const onStart = (e) => {
        e.preventDefault();
        state.drawing = true;
        state.points = [getPos(e)];
    };

    const onMove = (e) => {
        if (!state.drawing) return;
        e.preventDefault();
        state.points.push(getPos(e));
        redraw(state);
    };

    const onEnd = (e) => {
        if (!state.drawing) return;
        e.preventDefault();
        state.drawing = false;
        if (state.points.length > 0) {
            state.strokes.push([...state.points]);
            state.points = [];
        }
    };

    canvas.addEventListener('pointerdown', onStart);
    canvas.addEventListener('pointermove', onMove);
    canvas.addEventListener('pointerup', onEnd);
    canvas.addEventListener('pointerleave', onEnd);
    canvas.style.touchAction = 'none';

    state._listeners = { onStart, onMove, onEnd };
    pads[canvasId] = state;
    return true;
}

function redraw(state) {
    const { ctx, canvas, strokes, points, lineWidth, strokeColor } = state;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = lineWidth;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    for (const stroke of [...strokes, points]) {
        if (stroke.length < 2) continue;
        ctx.beginPath();
        ctx.moveTo(stroke[0].x, stroke[0].y);

        for (let i = 1; i < stroke.length - 1; i++) {
            const midX = (stroke[i].x + stroke[i + 1].x) / 2;
            const midY = (stroke[i].y + stroke[i + 1].y) / 2;
            ctx.quadraticCurveTo(stroke[i].x, stroke[i].y, midX, midY);
        }

        const last = stroke[stroke.length - 1];
        ctx.lineTo(last.x, last.y);
        ctx.stroke();
    }
}

function clear(canvasId) {
    const state = pads[canvasId];
    if (!state) return;
    state.strokes = [];
    state.points = [];
    state.ctx.clearRect(0, 0, state.canvas.width, state.canvas.height);
}

function isEmpty(canvasId) {
    const state = pads[canvasId];
    return !state || state.strokes.length === 0;
}

function toDataUrl(canvasId, mimeType) {
    const state = pads[canvasId];
    if (!state) return null;
    return state.canvas.toDataURL(mimeType || 'image/png');
}

function toBase64(canvasId) {
    const url = toDataUrl(canvasId, 'image/png');
    if (!url) return null;
    return url.split(',')[1];
}

function loadFromDataUrl(canvasId, dataUrl) {
    const state = pads[canvasId];
    if (!state) return;
    const img = new Image();
    img.onload = () => {
        state.ctx.clearRect(0, 0, state.canvas.width, state.canvas.height);
        state.ctx.drawImage(img, 0, 0);
    };
    img.src = dataUrl;
}

function destroy(canvasId) {
    const state = pads[canvasId];
    if (!state) return;
    const { canvas, _listeners } = state;
    canvas.removeEventListener('pointerdown', _listeners.onStart);
    canvas.removeEventListener('pointermove', _listeners.onMove);
    canvas.removeEventListener('pointerup', _listeners.onEnd);
    canvas.removeEventListener('pointerleave', _listeners.onEnd);
    delete pads[canvasId];
}

window.SignaturePad = { init, clear, isEmpty, toDataUrl, toBase64, loadFromDataUrl, destroy };
