/**
 * factory-canvas.js — HTML5 Canvas rendering engine for the Factory Design Suite (Phase 22).
 *
 * Imported as an ES module via Blazor JS interop:
 *   await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/factory-canvas.js");
 *
 * Manages one or more canvas instances keyed by canvasId. Each instance holds its own
 * layout data, viewport transform (pan/zoom), active tool, selection state, and event
 * handlers so multiple editors on the same page would not conflict.
 */

// ---------------------------------------------------------------------------
// Private state — one entry per canvas instance
// ---------------------------------------------------------------------------
const instances = new Map();

/** Generate a short unique id for new elements. */
function uid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
        const r = Math.random() * 16 | 0;
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

// ---------------------------------------------------------------------------
// Default visual styles per element type
// ---------------------------------------------------------------------------
const DEFAULT_STYLES = {
    room:        { fill: '#e8f5e9', stroke: '#2e7d32', strokeWidth: 2 },
    workstation: { fill: '#e3f2fd', stroke: '#1565c0', strokeWidth: 2 },
    inventory:   { fill: '#fff8e1', stroke: '#f9a825', strokeWidth: 2 },
    utility:     { fill: 'none',    stroke: '#6a1b9a', strokeWidth: 2 },
    wall:        { fill: 'none',    stroke: '#37474f', strokeWidth: 6 },
    door:        { fill: 'none',    stroke: '#00897b', strokeWidth: 3 },
    annotation:  { fill: 'none',    stroke: '#546e7f', strokeWidth: 1 },
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Snap a value to the nearest grid increment. */
function snap(value, gridSize) {
    return Math.round(value / gridSize) * gridSize;
}

/** Test whether a point (px, py) is inside an axis-aligned rectangle. */
function pointInRect(px, py, x, y, w, h) {
    return px >= x && px <= x + w && py >= y && py <= y + h;
}

/** Distance from point to a line segment (for wall/utility hit-testing). */
function pointToSegmentDist(px, py, x1, y1, x2, y2) {
    const dx = x2 - x1, dy = y2 - y1;
    const lenSq = dx * dx + dy * dy;
    if (lenSq === 0) return Math.hypot(px - x1, py - y1);
    let t = ((px - x1) * dx + (py - y1) * dy) / lenSq;
    t = Math.max(0, Math.min(1, t));
    return Math.hypot(px - (x1 + t * dx), py - (y1 + t * dy));
}

/** Convert mouse event coordinates to canvas-local (world) coordinates. */
function eventToWorld(inst, e) {
    const rect = inst.canvas.getBoundingClientRect();
    const cx = e.clientX - rect.left;
    const cy = e.clientY - rect.top;
    return {
        x: (cx - inst.panX) / inst.zoom,
        y: (cy - inst.panY) / inst.zoom,
    };
}

// ---------------------------------------------------------------------------
// Rendering
// ---------------------------------------------------------------------------

/** Clear the canvas and redraw everything: grid, elements, selection overlay. */
function render(inst) {
    const ctx = inst.ctx;
    const { width, height } = inst.canvas;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, width, height);

    // Apply viewport transform (pan + zoom)
    ctx.setTransform(inst.zoom, 0, 0, inst.zoom, inst.panX, inst.panY);

    drawGrid(inst, ctx);

    // Sort elements by zIndex then draw
    const sorted = [...inst.layout.elements].sort((a, b) => (a.zIndex || 0) - (b.zIndex || 0));
    for (const el of sorted) {
        drawElement(inst, ctx, el);
    }

    // Selection overlay
    if (inst.selectedId) {
        const sel = inst.layout.elements.find(e => e.id === inst.selectedId);
        if (sel) drawSelectionOverlay(inst, ctx, sel);
    }

    // Drawing preview (rubber-band rectangle or polyline segment)
    if (inst.drawing) {
        drawPreview(inst, ctx);
    }
}

/** Draw the background grid. */
function drawGrid(inst, ctx) {
    const gs = inst.layout.gridSize || 20;
    const lw = inst.layout.width || 1200;
    const lh = inst.layout.height || 800;

    ctx.save();
    ctx.strokeStyle = '#e0e0e0';
    ctx.lineWidth = 0.5;
    ctx.beginPath();
    for (let x = 0; x <= lw; x += gs) {
        ctx.moveTo(x, 0);
        ctx.lineTo(x, lh);
    }
    for (let y = 0; y <= lh; y += gs) {
        ctx.moveTo(0, y);
        ctx.lineTo(lw, y);
    }
    ctx.stroke();

    // Layout boundary
    ctx.strokeStyle = '#bdbdbd';
    ctx.lineWidth = 1;
    ctx.strokeRect(0, 0, lw, lh);
    ctx.restore();
}

/** Draw a single layout element. */
function drawElement(inst, ctx, el) {
    const style = { ...DEFAULT_STYLES[el.type], ...(el.style || {}) };
    ctx.save();

    // Apply element-level rotation around its centre
    if (el.rotation) {
        const cx = el.x + (el.width || 0) / 2;
        const cy = el.y + (el.height || 0) / 2;
        ctx.translate(cx, cy);
        ctx.rotate((el.rotation * Math.PI) / 180);
        ctx.translate(-cx, -cy);
    }

    // Hover highlight
    if (el.id === inst.hoveredId && el.id !== inst.selectedId) {
        ctx.save();
        ctx.strokeStyle = 'rgba(33,150,243,0.4)';
        ctx.lineWidth = (style.strokeWidth || 2) + 2;
        if (el.width != null && el.height != null) {
            ctx.strokeRect(el.x - 2, el.y - 2, el.width + 4, el.height + 4);
        }
        ctx.restore();
    }

    switch (el.type) {
        case 'room':
            drawRoom(ctx, el, style);
            break;
        case 'workstation':
            drawWorkstation(ctx, el, style);
            break;
        case 'inventory':
            drawInventory(ctx, el, style);
            break;
        case 'utility':
            drawPolyline(ctx, el, style);
            break;
        case 'wall':
            drawWall(ctx, el, style);
            break;
        case 'door':
            drawDoor(ctx, el, style);
            break;
        case 'annotation':
            drawAnnotation(ctx, el, style);
            break;
        default:
            // Unknown type — draw a generic rectangle
            ctx.fillStyle = '#fafafa';
            ctx.fillRect(el.x, el.y, el.width || 40, el.height || 40);
            break;
    }

    ctx.restore();
}

// --- Individual element draw routines ---

function drawRoom(ctx, el, style) {
    ctx.fillStyle = style.fill;
    ctx.fillRect(el.x, el.y, el.width, el.height);
    ctx.setLineDash([6, 4]);
    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.strokeRect(el.x, el.y, el.width, el.height);
    ctx.setLineDash([]);
    drawLabel(ctx, el);
}

function drawWorkstation(ctx, el, style) {
    ctx.fillStyle = style.fill;
    ctx.fillRect(el.x, el.y, el.width, el.height);
    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.strokeRect(el.x, el.y, el.width, el.height);

    // Small gear icon indicator (top-right corner)
    const ix = el.x + el.width - 16;
    const iy = el.y + 4;
    ctx.fillStyle = style.stroke;
    ctx.font = '12px sans-serif';
    ctx.fillText('\u2699', ix, iy + 12);

    drawLabel(ctx, el);
}

function drawInventory(ctx, el, style) {
    ctx.fillStyle = style.fill;
    ctx.fillRect(el.x, el.y, el.width, el.height);

    // Diagonal hatch pattern
    ctx.save();
    ctx.beginPath();
    ctx.rect(el.x, el.y, el.width, el.height);
    ctx.clip();
    ctx.strokeStyle = 'rgba(0,0,0,0.08)';
    ctx.lineWidth = 1;
    const step = 10;
    for (let d = -el.height; d < el.width; d += step) {
        ctx.beginPath();
        ctx.moveTo(el.x + d, el.y);
        ctx.lineTo(el.x + d + el.height, el.y + el.height);
        ctx.stroke();
    }
    ctx.restore();

    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.strokeRect(el.x, el.y, el.width, el.height);
    drawLabel(ctx, el);
}

function drawPolyline(ctx, el, style) {
    if (!el.points || el.points.length < 2) return;
    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.beginPath();
    ctx.moveTo(el.x + el.points[0].x, el.y + el.points[0].y);
    for (let i = 1; i < el.points.length; i++) {
        ctx.lineTo(el.x + el.points[i].x, el.y + el.points[i].y);
    }
    ctx.stroke();
    drawLabel(ctx, el);
}

function drawWall(ctx, el, style) {
    if (!el.points || el.points.length < 2) return;
    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(el.x + el.points[0].x, el.y + el.points[0].y);
    for (let i = 1; i < el.points.length; i++) {
        ctx.lineTo(el.x + el.points[i].x, el.y + el.points[i].y);
    }
    ctx.stroke();
}

function drawDoor(ctx, el, style) {
    // Draw as an arc indicating a swing door — semicircle from p0 to p1
    if (!el.points || el.points.length < 2) return;
    const p0 = el.points[0];
    const p1 = el.points[1];
    const mx = el.x + (p0.x + p1.x) / 2;
    const my = el.y + (p0.y + p1.y) / 2;
    const radius = Math.hypot(p1.x - p0.x, p1.y - p0.y) / 2;
    const angle = Math.atan2(p1.y - p0.y, p1.x - p0.x);

    ctx.strokeStyle = style.stroke;
    ctx.lineWidth = style.strokeWidth;
    ctx.beginPath();
    ctx.arc(mx, my, radius, angle, angle + Math.PI);
    ctx.stroke();

    // Gap indicator (dashed line between endpoints)
    ctx.setLineDash([4, 3]);
    ctx.beginPath();
    ctx.moveTo(el.x + p0.x, el.y + p0.y);
    ctx.lineTo(el.x + p1.x, el.y + p1.y);
    ctx.stroke();
    ctx.setLineDash([]);
}

function drawAnnotation(ctx, el, style) {
    if (!el.label) return;
    ctx.fillStyle = style.stroke;
    ctx.font = 'bold 14px sans-serif';
    ctx.textBaseline = 'top';
    ctx.fillText(el.label, el.x, el.y);
}

/** Draw a centred label inside a rectangular element. */
function drawLabel(ctx, el) {
    if (!el.label) return;
    ctx.fillStyle = '#212121';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(el.label, el.x + (el.width || 0) / 2, el.y + (el.height || 0) / 2);
    ctx.textAlign = 'start';
    ctx.textBaseline = 'alphabetic';
}

// --- Selection overlay ---

function drawSelectionOverlay(inst, ctx, el) {
    ctx.save();
    ctx.strokeStyle = '#1976d2';
    ctx.lineWidth = 1.5;
    ctx.setLineDash([4, 3]);

    const bounds = elementBounds(el);
    if (bounds) {
        ctx.strokeRect(bounds.x - 4, bounds.y - 4, bounds.w + 8, bounds.h + 8);

        // Resize handles (8 points around the bounding box)
        ctx.setLineDash([]);
        ctx.fillStyle = '#ffffff';
        ctx.strokeStyle = '#1976d2';
        ctx.lineWidth = 1;
        const hSize = 6;
        const handles = getResizeHandles(bounds, hSize);
        for (const h of handles) {
            ctx.fillRect(h.x, h.y, hSize, hSize);
            ctx.strokeRect(h.x, h.y, hSize, hSize);
        }
    }
    ctx.restore();
}

/** Compute bounding box { x, y, w, h } for any element type. */
function elementBounds(el) {
    if (el.type === 'wall' || el.type === 'utility' || el.type === 'door') {
        if (!el.points || el.points.length === 0) return null;
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        for (const p of el.points) {
            const px = el.x + p.x, py = el.y + p.y;
            if (px < minX) minX = px;
            if (py < minY) minY = py;
            if (px > maxX) maxX = px;
            if (py > maxY) maxY = py;
        }
        return { x: minX, y: minY, w: maxX - minX || 10, h: maxY - minY || 10 };
    }
    if (el.type === 'annotation') {
        return { x: el.x, y: el.y, w: el.width || 100, h: el.height || 20 };
    }
    return { x: el.x, y: el.y, w: el.width || 0, h: el.height || 0 };
}

/** Return an array of 8 resize handle rects { x, y, cursor, pos }. */
function getResizeHandles(bounds, size) {
    const s2 = size / 2;
    const { x, y, w, h } = bounds;
    return [
        { x: x - 4 - s2,       y: y - 4 - s2,       pos: 'nw' },
        { x: x - 4 + w / 2 - s2, y: y - 4 - s2,       pos: 'n'  },
        { x: x - 4 + w + s2 - size, y: y - 4 - s2,   pos: 'ne' },
        { x: x - 4 + w + s2 - size, y: y - 4 + h / 2 - s2, pos: 'e' },
        { x: x - 4 + w + s2 - size, y: y - 4 + h + s2 - size, pos: 'se' },
        { x: x - 4 + w / 2 - s2, y: y - 4 + h + s2 - size, pos: 's'  },
        { x: x - 4 - s2,       y: y - 4 + h + s2 - size, pos: 'sw' },
        { x: x - 4 - s2,       y: y - 4 + h / 2 - s2,    pos: 'w'  },
    ];
}

/** Check whether a world point hits a resize handle; returns handle pos or null. */
function hitTestHandles(inst, wx, wy) {
    if (!inst.selectedId) return null;
    const el = inst.layout.elements.find(e => e.id === inst.selectedId);
    if (!el) return null;
    const bounds = elementBounds(el);
    if (!bounds) return null;
    const handles = getResizeHandles(bounds, 6);
    for (const h of handles) {
        if (wx >= h.x - 2 && wx <= h.x + 8 && wy >= h.y - 2 && wy <= h.y + 8) {
            return h.pos;
        }
    }
    return null;
}

/** Draw a rubber-band preview while the user drags to create an element. */
function drawPreview(inst, ctx) {
    const d = inst.drawing;
    ctx.save();
    ctx.setLineDash([4, 4]);
    ctx.strokeStyle = '#1976d2';
    ctx.lineWidth = 1;

    if (d.mode === 'rect') {
        const x = Math.min(d.startX, d.currentX);
        const y = Math.min(d.startY, d.currentY);
        const w = Math.abs(d.currentX - d.startX);
        const h = Math.abs(d.currentY - d.startY);
        ctx.strokeRect(x, y, w, h);
    } else if (d.mode === 'polyline' && d.points.length > 0) {
        ctx.beginPath();
        ctx.moveTo(d.points[0].x, d.points[0].y);
        for (let i = 1; i < d.points.length; i++) {
            ctx.lineTo(d.points[i].x, d.points[i].y);
        }
        ctx.lineTo(d.currentX, d.currentY);
        ctx.stroke();
    }
    ctx.restore();
}

// ---------------------------------------------------------------------------
// Hit testing
// ---------------------------------------------------------------------------

/** Find the topmost element at world coordinates (wx, wy). */
function hitTest(inst, wx, wy) {
    const sorted = [...inst.layout.elements].sort((a, b) => (b.zIndex || 0) - (a.zIndex || 0));
    for (const el of sorted) {
        if (isPointInElement(el, wx, wy)) return el;
    }
    return null;
}

function isPointInElement(el, wx, wy) {
    switch (el.type) {
        case 'wall':
        case 'utility':
        case 'door':
            if (!el.points || el.points.length < 2) return false;
            for (let i = 0; i < el.points.length - 1; i++) {
                const d = pointToSegmentDist(
                    wx, wy,
                    el.x + el.points[i].x, el.y + el.points[i].y,
                    el.x + el.points[i + 1].x, el.y + el.points[i + 1].y
                );
                if (d < 8) return true;
            }
            return false;
        case 'annotation':
            return pointInRect(wx, wy, el.x, el.y, el.width || 100, el.height || 20);
        default:
            return pointInRect(wx, wy, el.x, el.y, el.width || 0, el.height || 0);
    }
}

// ---------------------------------------------------------------------------
// Cursor helpers
// ---------------------------------------------------------------------------

const HANDLE_CURSORS = {
    nw: 'nwse-resize', n: 'ns-resize', ne: 'nesw-resize',
    e: 'ew-resize', se: 'nwse-resize', s: 'ns-resize',
    sw: 'nesw-resize', w: 'ew-resize',
};

function updateCursor(inst, wx, wy) {
    if (inst.activeTool !== 'select') {
        inst.canvas.style.cursor = 'crosshair';
        return;
    }
    const handle = hitTestHandles(inst, wx, wy);
    if (handle) {
        inst.canvas.style.cursor = HANDLE_CURSORS[handle] || 'pointer';
        return;
    }
    const hit = hitTest(inst, wx, wy);
    inst.canvas.style.cursor = hit ? 'move' : 'default';
}

// ---------------------------------------------------------------------------
// Event handlers
// ---------------------------------------------------------------------------

function onMouseDown(inst, e) {
    const { x: wx, y: wy } = eventToWorld(inst, e);
    const gs = inst.layout.gridSize || 20;

    if (inst.activeTool === 'select') {
        // Check for resize handle first
        const handle = hitTestHandles(inst, wx, wy);
        if (handle) {
            inst.resizing = { handle, startX: wx, startY: wy, el: findSelected(inst) };
            if (inst.resizing.el) {
                inst.resizing.origX = inst.resizing.el.x;
                inst.resizing.origY = inst.resizing.el.y;
                inst.resizing.origW = inst.resizing.el.width;
                inst.resizing.origH = inst.resizing.el.height;
            }
            return;
        }

        // Hit-test elements
        const hit = hitTest(inst, wx, wy);
        if (hit) {
            selectElement(inst, hit.id);
            inst.dragging = { el: hit, startX: wx, startY: wy, origX: hit.x, origY: hit.y };
        } else {
            selectElement(inst, null);
        }
    } else if (['room', 'workstation', 'inventory'].includes(inst.activeTool)) {
        // Rectangle drawing tools — start rubber band
        const sx = snap(wx, gs);
        const sy = snap(wy, gs);
        inst.drawing = { mode: 'rect', type: inst.activeTool, startX: sx, startY: sy, currentX: sx, currentY: sy };
    } else if (['wall', 'utility', 'door'].includes(inst.activeTool)) {
        // Polyline drawing tools — add point on click
        const sx = snap(wx, gs);
        const sy = snap(wy, gs);
        if (!inst.drawing) {
            inst.drawing = { mode: 'polyline', type: inst.activeTool, points: [{ x: sx, y: sy }], currentX: sx, currentY: sy };
        } else {
            inst.drawing.points.push({ x: sx, y: sy });
            // Double-click or 2 points for wall/door finishes the polyline
            if ((inst.activeTool === 'wall' || inst.activeTool === 'door') && inst.drawing.points.length >= 2) {
                finishPolyline(inst);
            }
        }
    } else if (inst.activeTool === 'annotation') {
        const sx = snap(wx, gs);
        const sy = snap(wy, gs);
        const newEl = {
            id: uid(), type: 'annotation',
            x: sx, y: sy, width: 100, height: 20,
            rotation: 0, label: 'Text', style: {}, locked: false, zIndex: 0,
        };
        inst.layout.elements.push(newEl);
        selectElement(inst, newEl.id);
        notifyChanged(inst, newEl);
        render(inst);
    }
}

function onMouseMove(inst, e) {
    const { x: wx, y: wy } = eventToWorld(inst, e);
    const gs = inst.layout.gridSize || 20;

    // Update hovered element for highlight
    if (inst.activeTool === 'select' && !inst.dragging && !inst.resizing) {
        const hit = hitTest(inst, wx, wy);
        const newHovered = hit ? hit.id : null;
        if (newHovered !== inst.hoveredId) {
            inst.hoveredId = newHovered;
            render(inst);
        }
    }

    updateCursor(inst, wx, wy);

    // Dragging an element
    if (inst.dragging) {
        const d = inst.dragging;
        d.el.x = snap(d.origX + (wx - d.startX), gs);
        d.el.y = snap(d.origY + (wy - d.startY), gs);
        render(inst);
        return;
    }

    // Resizing via handle
    if (inst.resizing) {
        applyResize(inst, wx, wy, gs);
        render(inst);
        return;
    }

    // Drawing preview
    if (inst.drawing) {
        inst.drawing.currentX = snap(wx, gs);
        inst.drawing.currentY = snap(wy, gs);
        render(inst);
    }
}

function onMouseUp(inst, e) {
    const gs = inst.layout.gridSize || 20;

    // Finish drag
    if (inst.dragging) {
        notifyChanged(inst, inst.dragging.el);
        inst.dragging = null;
        return;
    }

    // Finish resize
    if (inst.resizing) {
        if (inst.resizing.el) notifyChanged(inst, inst.resizing.el);
        inst.resizing = null;
        return;
    }

    // Finish rectangle drawing
    if (inst.drawing && inst.drawing.mode === 'rect') {
        const d = inst.drawing;
        const x = snap(Math.min(d.startX, d.currentX), gs);
        const y = snap(Math.min(d.startY, d.currentY), gs);
        const w = snap(Math.abs(d.currentX - d.startX), gs);
        const h = snap(Math.abs(d.currentY - d.startY), gs);
        if (w >= gs && h >= gs) {
            const newEl = {
                id: uid(), type: d.type,
                x, y, width: w, height: h,
                rotation: 0, label: labelForType(d.type),
                style: {}, points: null, locked: false, zIndex: 0,
            };
            inst.layout.elements.push(newEl);
            selectElement(inst, newEl.id);
            notifyChanged(inst, newEl);
        }
        inst.drawing = null;
        render(inst);
    }
    // Note: polyline drawing continues across clicks — finished via finishPolyline
}

function onDblClick(inst, e) {
    // Double-click finishes polyline drawing
    if (inst.drawing && inst.drawing.mode === 'polyline') {
        finishPolyline(inst);
    }
}

function onKeyDown(inst, e) {
    if (e.key === 'Delete' || e.key === 'Backspace') {
        deleteSelectedInternal(inst);
    } else if (e.key === 'Escape') {
        if (inst.drawing) {
            inst.drawing = null;
            render(inst);
        } else {
            selectElement(inst, null);
        }
    }
}

// ---------------------------------------------------------------------------
// Resize logic
// ---------------------------------------------------------------------------

function applyResize(inst, wx, wy, gs) {
    const r = inst.resizing;
    if (!r.el) return;
    const dx = snap(wx - r.startX, gs);
    const dy = snap(wy - r.startY, gs);

    let { origX, origY, origW, origH } = r;

    switch (r.handle) {
        case 'nw': origX += dx; origY += dy; origW -= dx; origH -= dy; break;
        case 'n':  origY += dy; origH -= dy; break;
        case 'ne': origW += dx; origY += dy; origH -= dy; break;
        case 'e':  origW += dx; break;
        case 'se': origW += dx; origH += dy; break;
        case 's':  origH += dy; break;
        case 'sw': origX += dx; origW -= dx; origH += dy; break;
        case 'w':  origX += dx; origW -= dx; break;
    }

    // Enforce minimum size
    const minSize = gs;
    if (origW < minSize) { origW = minSize; }
    if (origH < minSize) { origH = minSize; }

    r.el.x = origX;
    r.el.y = origY;
    r.el.width = origW;
    r.el.height = origH;
}

// ---------------------------------------------------------------------------
// Polyline finalisation
// ---------------------------------------------------------------------------

function finishPolyline(inst) {
    const d = inst.drawing;
    if (!d || d.points.length < 2) { inst.drawing = null; render(inst); return; }

    // Use first point as origin, store offsets
    const originX = d.points[0].x;
    const originY = d.points[0].y;
    const pts = d.points.map(p => ({ x: p.x - originX, y: p.y - originY }));

    const newEl = {
        id: uid(), type: d.type,
        x: originX, y: originY, width: 0, height: 0,
        rotation: 0, label: labelForType(d.type),
        style: {}, points: pts, locked: false, zIndex: 0,
    };

    inst.layout.elements.push(newEl);
    selectElement(inst, newEl.id);
    notifyChanged(inst, newEl);
    inst.drawing = null;
    render(inst);
}

function labelForType(type) {
    const labels = {
        room: 'Room', workstation: 'Workstation', inventory: 'Storage',
        utility: 'Utility', wall: '', door: '', annotation: 'Text',
    };
    return labels[type] || type;
}

// ---------------------------------------------------------------------------
// Selection & notification helpers
// ---------------------------------------------------------------------------

function findSelected(inst) {
    return inst.layout.elements.find(e => e.id === inst.selectedId) || null;
}

function selectElement(inst, id) {
    if (inst.selectedId === id) return;
    inst.selectedId = id;
    render(inst);
    if (id && inst.dotNetRef) {
        inst.dotNetRef.invokeMethodAsync('OnElementSelected', id);
    }
}

function notifyChanged(inst, el) {
    if (!inst.dotNetRef) return;
    inst.dotNetRef.invokeMethodAsync('OnElementChanged', JSON.stringify(el));
}

function deleteSelectedInternal(inst) {
    if (!inst.selectedId) return;
    const idx = inst.layout.elements.findIndex(e => e.id === inst.selectedId);
    if (idx === -1) return;
    const el = inst.layout.elements[idx];
    if (el.locked) return;
    inst.layout.elements.splice(idx, 1);
    inst.selectedId = null;
    render(inst);
    notifyChanged(inst, { ...el, _deleted: true });
}

// ---------------------------------------------------------------------------
// Canvas sizing helper — match parent container
// ---------------------------------------------------------------------------

function resizeCanvas(inst) {
    const parent = inst.canvas.parentElement;
    if (!parent) return;
    const dpr = window.devicePixelRatio || 1;
    const w = parent.clientWidth;
    const h = parent.clientHeight;
    inst.canvas.width = w * dpr;
    inst.canvas.height = h * dpr;
    inst.canvas.style.width = w + 'px';
    inst.canvas.style.height = h + 'px';
    inst.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    render(inst);
}

// ---------------------------------------------------------------------------
// Exported API
// ---------------------------------------------------------------------------

/**
 * Initialise a canvas instance, parse the layout, render, and wire up events.
 * @param {string} canvasId     DOM id of the <canvas> element
 * @param {object} dotNetRef    .NET object reference for invoking Blazor callbacks
 * @param {string} layoutJson   JSON string matching the LayoutJson schema
 */
export function initCanvas(canvasId, dotNetRef, layoutJson) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) throw new Error(`Canvas element '${canvasId}' not found.`);

    const ctx = canvas.getContext('2d');
    const layout = layoutJson ? JSON.parse(layoutJson) : { width: 1200, height: 800, gridSize: 20, elements: [] };

    const inst = {
        canvas,
        ctx,
        dotNetRef,
        layout,
        zoom: 1,
        panX: 0,
        panY: 0,
        activeTool: 'select',
        selectedId: null,
        hoveredId: null,
        dragging: null,
        resizing: null,
        drawing: null,
        handlers: {},
    };

    // Bind event handlers (store references for cleanup)
    inst.handlers.mousedown  = (e) => onMouseDown(inst, e);
    inst.handlers.mousemove  = (e) => onMouseMove(inst, e);
    inst.handlers.mouseup    = (e) => onMouseUp(inst, e);
    inst.handlers.dblclick   = (e) => onDblClick(inst, e);
    inst.handlers.keydown    = (e) => onKeyDown(inst, e);
    inst.handlers.resize     = ()  => resizeCanvas(inst);
    inst.handlers.wheel      = (e) => { e.preventDefault(); onWheel(inst, e); };

    canvas.addEventListener('mousedown', inst.handlers.mousedown);
    canvas.addEventListener('mousemove', inst.handlers.mousemove);
    canvas.addEventListener('mouseup', inst.handlers.mouseup);
    canvas.addEventListener('dblclick', inst.handlers.dblclick);
    canvas.addEventListener('wheel', inst.handlers.wheel, { passive: false });
    // Key events on the canvas require it to be focusable
    canvas.setAttribute('tabindex', '0');
    canvas.addEventListener('keydown', inst.handlers.keydown);
    window.addEventListener('resize', inst.handlers.resize);

    instances.set(canvasId, inst);
    resizeCanvas(inst);
    console.log(`[FactoryCanvas] Initialised '${canvasId}' with ${layout.elements.length} elements.`);
}

/** Replace the layout and re-render. */
export function updateLayout(canvasId, layoutJson) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    inst.layout = JSON.parse(layoutJson);
    inst.selectedId = null;
    inst.drawing = null;
    render(inst);
}

/** Return the current layout as a JSON string. */
export function getLayoutJson(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return '{}';
    return JSON.stringify(inst.layout);
}

/** Set the active drawing/interaction tool. */
export function setActiveTool(canvasId, toolName) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    inst.activeTool = toolName;
    inst.drawing = null;
    if (toolName !== 'select') inst.selectedId = null;
    render(inst);
}

/** Zoom in (x1.25). */
export function zoomIn(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    setZoom(inst, inst.zoom * 1.25);
}

/** Zoom out (x0.8). */
export function zoomOut(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    setZoom(inst, inst.zoom * 0.8);
}

/** Reset zoom to 1 and pan to origin. */
export function resetView(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    inst.zoom = 1;
    inst.panX = 0;
    inst.panY = 0;
    render(inst);
}

/** Delete the currently selected element. */
export function deleteSelected(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return;
    deleteSelectedInternal(inst);
}

/** Remove all event listeners and free state for a canvas instance. */
export function dispose(canvasId) {
    const inst = instances.get(canvasId);
    if (!inst) return;

    inst.canvas.removeEventListener('mousedown', inst.handlers.mousedown);
    inst.canvas.removeEventListener('mousemove', inst.handlers.mousemove);
    inst.canvas.removeEventListener('mouseup', inst.handlers.mouseup);
    inst.canvas.removeEventListener('dblclick', inst.handlers.dblclick);
    inst.canvas.removeEventListener('wheel', inst.handlers.wheel);
    inst.canvas.removeEventListener('keydown', inst.handlers.keydown);
    window.removeEventListener('resize', inst.handlers.resize);

    instances.delete(canvasId);
    console.log(`[FactoryCanvas] Disposed '${canvasId}'.`);
}

// ---------------------------------------------------------------------------
// Internal zoom helper (shared by zoomIn/Out and mouse wheel)
// ---------------------------------------------------------------------------

function setZoom(inst, newZoom) {
    inst.zoom = Math.max(0.2, Math.min(5, newZoom));
    render(inst);
}

function onWheel(inst, e) {
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    const { x: wx, y: wy } = eventToWorld(inst, e);
    const oldZoom = inst.zoom;
    const newZoom = Math.max(0.2, Math.min(5, oldZoom * delta));

    // Zoom towards cursor position
    inst.panX -= wx * (newZoom - oldZoom);
    inst.panY -= wy * (newZoom - oldZoom);
    inst.zoom = newZoom;
    render(inst);
}
