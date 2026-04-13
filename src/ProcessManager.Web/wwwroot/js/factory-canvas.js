/**
 * factory-canvas.js — Three.js 3D Factory Design viewer/editor (Phase 22).
 *
 * Imported as an ES module via Blazor JS interop:
 *   await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/factory-canvas.js");
 *
 * Exports the same public API that the Blazor FactoryDesignEditor.razor expects:
 *   initCanvas, setActiveTool, getLayoutJson, updateLayout,
 *   zoomIn, zoomOut, resetView, deleteSelected, dispose
 */

import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// ---------------------------------------------------------------------------
// Private state — one entry per container instance
// ---------------------------------------------------------------------------
const instances = new Map();

function uid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
        const r = Math.random() * 16 | 0;
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

// ---------------------------------------------------------------------------
// Colour palettes per element type
// ---------------------------------------------------------------------------
const TYPE_COLORS = {
    room:        { base: 0x81c784, hover: 0xa5d6a7, select: 0x66bb6a },
    workstation: { base: 0x64b5f6, hover: 0x90caf9, select: 0x42a5f5 },
    inventory:   { base: 0xffd54f, hover: 0xffe082, select: 0xffca28 },
    utility:     { base: 0xce93d8, hover: 0xe1bee7, select: 0xba68c8 },
    wall:        { base: 0x78909c, hover: 0x90a4ae, select: 0x607d8b },
    door:        { base: 0x4db6ac, hover: 0x80cbc4, select: 0x26a69a },
    annotation:  { base: 0xbcaaa4, hover: 0xd7ccc8, select: 0xa1887f },
};

// 3D heights per element type (world-units)
const TYPE_HEIGHTS = {
    room:        2,
    workstation: 120,
    inventory:   160,
    utility:     10,
    wall:        200,
    door:        180,
    annotation:  5,
};

// ---------------------------------------------------------------------------
// Initialisation
// ---------------------------------------------------------------------------

/**
 * @param {string} containerId  DOM id of the host element (div or canvas)
 * @param {object} dotNetRef    .NET DotNetObjectReference for callbacks
 * @param {string} layoutJson   JSON layout from the API
 */
export function initCanvas(containerId, dotNetRef, layoutJson) {
    console.log(`[Factory3D] initCanvas: id='${containerId}', layoutLen=${layoutJson?.length ?? 0}`);

    let container = document.getElementById(containerId);
    if (!container) {
        throw new Error(`Container '${containerId}' not found.`);
    }

    // If the host is a <canvas>, replace it with a <div> (Three.js manages its own canvas)
    if (container.tagName === 'CANVAS') {
        const div = document.createElement('div');
        div.id = container.id;
        div.className = container.className;
        div.style.cssText = container.style.cssText + ';overflow:hidden;';
        container.parentElement.replaceChild(div, container);
        container = div;
    }

    // Parse layout
    const raw = layoutJson ? JSON.parse(layoutJson) : {};
    const layout = {
        width:  raw.width  || raw.canvasWidth  || 5000,
        height: raw.height || raw.canvasHeight  || 3000,
        gridSize: raw.gridSize || 500,
        backgroundColor: raw.backgroundColor || '#f5f5f5',
        elements: raw.elements || [],
    };

    // ── Scene ──
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f4f8);
    scene.fog = new THREE.Fog(0xf0f4f8, layout.width * 0.8, layout.width * 1.5);

    // ── Renderer ──
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.1;
    container.innerHTML = '';
    container.appendChild(renderer.domElement);

    // ── Camera ──
    const aspect = container.clientWidth / Math.max(container.clientHeight, 1);
    const camera = new THREE.PerspectiveCamera(50, aspect, 1, layout.width * 5);
    // Position camera to see the whole floor
    const camDist = Math.max(layout.width, layout.height) * 0.75;
    camera.position.set(layout.width / 2, camDist, layout.height * 0.9);
    camera.lookAt(layout.width / 2, 0, layout.height / 2);

    // ── Orbit Controls ──
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.target.set(layout.width / 2, 0, layout.height / 2);
    controls.maxPolarAngle = Math.PI / 2 - 0.02;   // Don't go below the floor
    controls.minDistance = 100;
    controls.maxDistance = layout.width * 3;
    controls.update();

    // ── Lighting ──
    const ambient = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambient);

    const hemi = new THREE.HemisphereLight(0xb1e1ff, 0xb97a20, 0.4);
    scene.add(hemi);

    const sun = new THREE.DirectionalLight(0xffffff, 1.2);
    sun.position.set(layout.width * 0.6, layout.width * 0.5, layout.height * 0.3);
    sun.castShadow = true;
    sun.shadow.mapSize.set(2048, 2048);
    const shadowExtent = Math.max(layout.width, layout.height) * 0.6;
    sun.shadow.camera.left   = -shadowExtent;
    sun.shadow.camera.right  =  shadowExtent;
    sun.shadow.camera.top    =  shadowExtent;
    sun.shadow.camera.bottom = -shadowExtent;
    sun.shadow.camera.near = 1;
    sun.shadow.camera.far = layout.width * 2;
    scene.add(sun);

    // ── Ground plane ──
    const groundGeo = new THREE.PlaneGeometry(layout.width, layout.height);
    const groundMat = new THREE.MeshStandardMaterial({
        color: 0xe8e8e8,
        roughness: 0.9,
        metalness: 0.0,
    });
    const ground = new THREE.Mesh(groundGeo, groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.position.set(layout.width / 2, 0, layout.height / 2);
    ground.receiveShadow = true;
    ground.userData._isGround = true;
    scene.add(ground);

    // ── Grid ──
    const gridHelper = new THREE.GridHelper(
        Math.max(layout.width, layout.height),
        Math.max(layout.width, layout.height) / layout.gridSize,
        0xbbbbbb, 0xdddddd
    );
    gridHelper.position.set(layout.width / 2, 0.5, layout.height / 2);
    scene.add(gridHelper);

    // ── Floor border ──
    const borderGeo = new THREE.EdgesGeometry(new THREE.PlaneGeometry(layout.width, layout.height));
    const borderMat = new THREE.LineBasicMaterial({ color: 0x999999 });
    const borderLine = new THREE.LineSegments(borderGeo, borderMat);
    borderLine.rotation.x = -Math.PI / 2;
    borderLine.position.set(layout.width / 2, 1, layout.height / 2);
    scene.add(borderLine);

    // ── Instance state ──
    const inst = {
        container,
        renderer,
        scene,
        camera,
        controls,
        ground,
        layout,
        dotNetRef,
        activeTool: 'select',
        selectedId: null,
        hoveredId: null,
        meshMap: new Map(),        // elementId → THREE.Group
        dragging: null,
        drawing: null,
        raycaster: new THREE.Raycaster(),
        mouse: new THREE.Vector2(),
        animId: null,
        handlers: {},
    };

    // ── Build existing elements ──
    for (const el of layout.elements) {
        createMesh(inst, el);
    }

    // ── Event wiring ──
    const canvas = renderer.domElement;
    inst.handlers.pointerdown  = (e) => onPointerDown(inst, e);
    inst.handlers.pointermove  = (e) => onPointerMove(inst, e);
    inst.handlers.pointerup    = (e) => onPointerUp(inst, e);
    inst.handlers.keydown      = (e) => onKeyDown(inst, e);

    canvas.addEventListener('pointerdown', inst.handlers.pointerdown);
    canvas.addEventListener('pointermove', inst.handlers.pointermove);
    canvas.addEventListener('pointerup',   inst.handlers.pointerup);
    canvas.setAttribute('tabindex', '0');
    canvas.addEventListener('keydown', inst.handlers.keydown);

    // ── Resize handling ──
    function onResize() {
        const w = container.clientWidth;
        const h = container.clientHeight;
        if (w === 0 || h === 0) return;
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
        renderer.setSize(w, h);
    }
    inst.handlers.resize = onResize;
    if (typeof ResizeObserver !== 'undefined') {
        inst.resizeObserver = new ResizeObserver(onResize);
        inst.resizeObserver.observe(container);
    }
    window.addEventListener('resize', onResize);
    onResize();

    // ── Render loop ──
    function animate() {
        inst.animId = requestAnimationFrame(animate);
        controls.update();
        renderer.render(scene, camera);
    }
    animate();

    instances.set(containerId, inst);
    console.log(`[Factory3D] Initialised '${containerId}' — ${layout.width}x${layout.height}, grid=${layout.gridSize}, elements=${layout.elements.length}`);
}

// ---------------------------------------------------------------------------
// Mesh creation per element type
// ---------------------------------------------------------------------------

function createMesh(inst, el) {
    const colors = TYPE_COLORS[el.type] || TYPE_COLORS.room;
    const h = TYPE_HEIGHTS[el.type] || 40;
    const group = new THREE.Group();
    group.userData.elementId = el.id;
    group.userData.elementType = el.type;

    switch (el.type) {
        case 'room':
            buildRoom(group, el, colors, h);
            break;
        case 'workstation':
            buildWorkstation(group, el, colors, h);
            break;
        case 'inventory':
            buildInventory(group, el, colors, h);
            break;
        case 'wall':
            buildWall(group, el, colors, h);
            break;
        case 'door':
            buildDoor(group, el, colors, h);
            break;
        case 'utility':
            buildUtility(group, el, colors, h);
            break;
        case 'annotation':
            buildAnnotation(group, el, colors);
            break;
        default:
            buildRoom(group, el, colors, h);
            break;
    }

    // Position — el.x/el.y are in layout coords (x → x, y → z in Three.js)
    group.position.set(el.x + (el.width || 0) / 2, 0, el.y + (el.height || 0) / 2);
    if (el.rotation) group.rotation.y = -(el.rotation * Math.PI) / 180;

    inst.scene.add(group);
    inst.meshMap.set(el.id, group);
}

function buildRoom(group, el, colors, h) {
    const w = el.width || 200;
    const d = el.height || 200;
    // Thin slab for floor area
    const geo = new THREE.BoxGeometry(w, h, d);
    const mat = new THREE.MeshStandardMaterial({
        color: colors.base, roughness: 0.7, metalness: 0.1, transparent: true, opacity: 0.65,
    });
    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.y = h / 2;
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    group.add(mesh);

    // Wireframe outline
    const edge = new THREE.LineSegments(
        new THREE.EdgesGeometry(geo),
        new THREE.LineBasicMaterial({ color: colors.select })
    );
    edge.position.y = h / 2;
    group.add(edge);

    addLabel(group, el.label || 'Room', h + 15, colors.select);
}

function buildWorkstation(group, el, colors, h) {
    const w = el.width || 200;
    const d = el.height || 200;

    // Main machine body
    const bodyGeo = new THREE.BoxGeometry(w * 0.9, h, d * 0.9);
    const bodyMat = new THREE.MeshStandardMaterial({ color: colors.base, roughness: 0.4, metalness: 0.3 });
    const body = new THREE.Mesh(bodyGeo, bodyMat);
    body.position.y = h / 2;
    body.castShadow = true;
    body.receiveShadow = true;
    group.add(body);

    // Control panel (small box on top)
    const panelGeo = new THREE.BoxGeometry(w * 0.3, 20, d * 0.15);
    const panelMat = new THREE.MeshStandardMaterial({ color: 0x263238, roughness: 0.2, metalness: 0.6 });
    const panel = new THREE.Mesh(panelGeo, panelMat);
    panel.position.set(0, h + 10, d * 0.3);
    panel.castShadow = true;
    group.add(panel);

    // Status light (green sphere)
    const lightGeo = new THREE.SphereGeometry(6, 16, 16);
    const lightMat = new THREE.MeshStandardMaterial({ color: 0x4caf50, emissive: 0x2e7d32, emissiveIntensity: 0.5 });
    const statusLight = new THREE.Mesh(lightGeo, lightMat);
    statusLight.position.set(w * 0.2, h + 20, d * 0.3);
    group.add(statusLight);

    addLabel(group, el.label || 'Workstation', h + 50, colors.select);
}

function buildInventory(group, el, colors, h) {
    const w = el.width || 200;
    const d = el.height || 100;
    const shelves = 4;
    const shelfH = h / shelves;

    // Uprights (4 pillars)
    const pillarGeo = new THREE.BoxGeometry(6, h, 6);
    const pillarMat = new THREE.MeshStandardMaterial({ color: 0x616161, roughness: 0.6, metalness: 0.4 });
    const offsets = [
        [-w / 2 + 3, -d / 2 + 3],
        [ w / 2 - 3, -d / 2 + 3],
        [-w / 2 + 3,  d / 2 - 3],
        [ w / 2 - 3,  d / 2 - 3],
    ];
    for (const [ox, oz] of offsets) {
        const pillar = new THREE.Mesh(pillarGeo, pillarMat);
        pillar.position.set(ox, h / 2, oz);
        pillar.castShadow = true;
        group.add(pillar);
    }

    // Shelves
    const shelfGeo = new THREE.BoxGeometry(w - 8, 4, d - 8);
    const shelfMat = new THREE.MeshStandardMaterial({ color: colors.base, roughness: 0.7 });
    for (let i = 0; i < shelves; i++) {
        const shelf = new THREE.Mesh(shelfGeo, shelfMat);
        shelf.position.y = shelfH * i + 2;
        shelf.castShadow = true;
        shelf.receiveShadow = true;
        group.add(shelf);
    }

    addLabel(group, el.label || 'Storage', h + 25, colors.select);
}

function buildWall(group, el, colors, h) {
    if (!el.points || el.points.length < 2) return;
    const thickness = 12;

    for (let i = 0; i < el.points.length - 1; i++) {
        const p0 = el.points[i];
        const p1 = el.points[i + 1];
        const dx = p1.x - p0.x;
        const dz = p1.y - p0.y;
        const len = Math.hypot(dx, dz);
        if (len < 1) continue;

        const wallGeo = new THREE.BoxGeometry(len, h, thickness);
        const wallMat = new THREE.MeshStandardMaterial({ color: colors.base, roughness: 0.9 });
        const wallMesh = new THREE.Mesh(wallGeo, wallMat);

        // Position at midpoint
        const mx = (p0.x + p1.x) / 2;
        const mz = (p0.y + p1.y) / 2;
        wallMesh.position.set(mx - (el.width || 0) / 2, h / 2, mz - (el.height || 0) / 2);
        wallMesh.rotation.y = -Math.atan2(dz, dx);
        wallMesh.castShadow = true;
        wallMesh.receiveShadow = true;
        group.add(wallMesh);
    }
}

function buildDoor(group, el, colors, h) {
    if (!el.points || el.points.length < 2) return;
    const p0 = el.points[0];
    const p1 = el.points[1];
    const dx = p1.x - p0.x;
    const dz = p1.y - p0.y;
    const doorWidth = Math.hypot(dx, dz);

    // Door frame
    const frameGeo = new THREE.BoxGeometry(doorWidth, h, 8);
    const frameMat = new THREE.MeshStandardMaterial({ color: colors.base, roughness: 0.5, metalness: 0.2, transparent: true, opacity: 0.5 });
    const frame = new THREE.Mesh(frameGeo, frameMat);
    const mx = (p0.x + p1.x) / 2;
    const mz = (p0.y + p1.y) / 2;
    frame.position.set(mx - (el.width || 0) / 2, h / 2, mz - (el.height || 0) / 2);
    frame.rotation.y = -Math.atan2(dz, dx);
    frame.castShadow = true;
    group.add(frame);
}

function buildUtility(group, el, colors, h) {
    if (!el.points || el.points.length < 2) return;
    const radius = 5;

    for (let i = 0; i < el.points.length - 1; i++) {
        const p0 = el.points[i];
        const p1 = el.points[i + 1];
        const dx = p1.x - p0.x;
        const dz = p1.y - p0.y;
        const len = Math.hypot(dx, dz);
        if (len < 1) continue;

        const pipeGeo = new THREE.CylinderGeometry(radius, radius, len, 8);
        const pipeMat = new THREE.MeshStandardMaterial({ color: colors.base, roughness: 0.3, metalness: 0.5 });
        const pipe = new THREE.Mesh(pipeGeo, pipeMat);
        pipe.rotation.z = Math.PI / 2;

        const mx = (p0.x + p1.x) / 2;
        const mz = (p0.y + p1.y) / 2;
        pipe.position.set(mx - (el.width || 0) / 2, h / 2 + 80, mz - (el.height || 0) / 2);
        pipe.rotation.y = -Math.atan2(dz, dx);
        pipe.castShadow = true;
        group.add(pipe);
    }
}

function buildAnnotation(group, el, colors) {
    // Floating signboard
    const w = el.width || 100;
    const signGeo = new THREE.BoxGeometry(w, 30, 4);
    const signMat = new THREE.MeshStandardMaterial({ color: 0xfafafa, roughness: 0.5 });
    const sign = new THREE.Mesh(signGeo, signMat);
    sign.position.y = 200;
    sign.castShadow = true;
    group.add(sign);

    // Pole
    const poleGeo = new THREE.CylinderGeometry(2, 2, 200, 6);
    const poleMat = new THREE.MeshStandardMaterial({ color: 0x9e9e9e });
    const pole = new THREE.Mesh(poleGeo, poleMat);
    pole.position.y = 100;
    group.add(pole);

    addLabel(group, el.label || 'Text', 225, 0x546e7f);
}

// ---------------------------------------------------------------------------
// Label helper (uses canvas texture for 3D text)
// ---------------------------------------------------------------------------

function addLabel(group, text, yPos, color) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    canvas.width = 512;
    canvas.height = 64;

    ctx.fillStyle = 'rgba(0,0,0,0)';
    ctx.fillRect(0, 0, 512, 64);

    ctx.font = 'bold 36px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillStyle = '#' + (color || 0x333333).toString(16).padStart(6, '0');
    ctx.fillText(text, 256, 32);

    const texture = new THREE.CanvasTexture(canvas);
    texture.minFilter = THREE.LinearFilter;

    const spriteMat = new THREE.SpriteMaterial({ map: texture, transparent: true, depthTest: false });
    const sprite = new THREE.Sprite(spriteMat);
    sprite.position.y = yPos;
    sprite.scale.set(200, 25, 1);
    group.add(sprite);
}

// ---------------------------------------------------------------------------
// Raycasting helpers
// ---------------------------------------------------------------------------

function getWorldHit(inst, e) {
    const rect = inst.renderer.domElement.getBoundingClientRect();
    inst.mouse.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    inst.mouse.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    inst.raycaster.setFromCamera(inst.mouse, inst.camera);

    // Hit ground plane for placement coordinates
    const groundHits = inst.raycaster.intersectObject(inst.ground);
    const groundPoint = groundHits.length > 0 ? groundHits[0].point : null;

    return groundPoint;
}

function getElementHit(inst, e) {
    const rect = inst.renderer.domElement.getBoundingClientRect();
    inst.mouse.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    inst.mouse.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    inst.raycaster.setFromCamera(inst.mouse, inst.camera);

    // Collect all meshes from element groups
    const meshes = [];
    for (const [, grp] of inst.meshMap) {
        grp.traverse(child => {
            if (child.isMesh) meshes.push(child);
        });
    }

    const hits = inst.raycaster.intersectObjects(meshes, false);
    if (hits.length === 0) return null;

    // Walk up to find the group with an elementId
    let obj = hits[0].object;
    while (obj && !obj.userData.elementId) obj = obj.parent;
    return obj ? obj.userData.elementId : null;
}

// ---------------------------------------------------------------------------
// Snap to grid
// ---------------------------------------------------------------------------

function snap(value, gs) {
    return Math.round(value / gs) * gs;
}

// ---------------------------------------------------------------------------
// Selection visuals
// ---------------------------------------------------------------------------

function setSelection(inst, elementId) {
    // Restore previous selection colour
    if (inst.selectedId) {
        const prevGroup = inst.meshMap.get(inst.selectedId);
        if (prevGroup) setGroupEmissive(prevGroup, 0x000000);
    }

    inst.selectedId = elementId;

    // Highlight new selection
    if (elementId) {
        const grp = inst.meshMap.get(elementId);
        if (grp) setGroupEmissive(grp, 0x2196f3, 0.3);
    }

    if (elementId && inst.dotNetRef) {
        inst.dotNetRef.invokeMethodAsync('OnElementSelected', elementId);
    }
}

function setHover(inst, elementId) {
    if (elementId === inst.hoveredId) return;

    // Un-hover previous
    if (inst.hoveredId && inst.hoveredId !== inst.selectedId) {
        const prev = inst.meshMap.get(inst.hoveredId);
        if (prev) setGroupEmissive(prev, 0x000000);
    }

    inst.hoveredId = elementId;

    // Hover new (unless it's already selected — that keeps its own highlight)
    if (elementId && elementId !== inst.selectedId) {
        const grp = inst.meshMap.get(elementId);
        if (grp) setGroupEmissive(grp, 0xffffff, 0.12);
    }
}

function setGroupEmissive(group, color, intensity) {
    group.traverse(child => {
        if (child.isMesh && child.material && child.material.emissive) {
            child.material.emissive.setHex(color);
            if (intensity !== undefined) child.material.emissiveIntensity = intensity;
        }
    });
}

// ---------------------------------------------------------------------------
// Event handlers
// ---------------------------------------------------------------------------

function onPointerDown(inst, e) {
    if (e.button !== 0) return; // Left-click only

    const groundPt = getWorldHit(inst, e);
    if (!groundPt) return;

    const gs = inst.layout.gridSize;

    if (inst.activeTool === 'select') {
        const hitId = getElementHit(inst, e);
        setSelection(inst, hitId);

        if (hitId) {
            // Start dragging
            const el = inst.layout.elements.find(el => el.id === hitId);
            const grp = inst.meshMap.get(hitId);
            if (el && grp && !el.locked) {
                inst.controls.enabled = false;
                inst.dragging = {
                    id: hitId,
                    el,
                    group: grp,
                    startMouse: groundPt.clone(),
                    origX: el.x,
                    origY: el.y,
                };
            }
        }
        return;
    }

    // Placement tools — click-to-place for workstation/inventory/annotation
    if (['workstation', 'inventory', 'annotation'].includes(inst.activeTool)) {
        const sx = snap(groundPt.x, gs);
        const sy = snap(groundPt.z, gs);
        const defaults = {
            workstation: { w: gs, h: gs },
            inventory:   { w: gs * 2, h: gs },
            annotation:  { w: gs, h: gs / 4 },
        };
        const d = defaults[inst.activeTool];
        const newEl = {
            id: uid(), type: inst.activeTool,
            x: sx - d.w / 2, y: sy - d.h / 2,
            width: d.w, height: d.h,
            rotation: 0, label: labelFor(inst.activeTool),
            style: {}, points: null, locked: false, zIndex: 0,
        };
        inst.layout.elements.push(newEl);
        createMesh(inst, newEl);
        setSelection(inst, newEl.id);
        notifyChanged(inst, newEl);
        return;
    }

    // Rectangle-draw tools (room) — start rubber-band
    if (inst.activeTool === 'room') {
        const sx = snap(groundPt.x, gs);
        const sy = snap(groundPt.z, gs);
        inst.controls.enabled = false;
        inst.drawing = {
            type: inst.activeTool,
            startX: sx, startY: sy,
            currentX: sx, currentY: sy,
            preview: null,
        };
        return;
    }

    // Wall / door / utility — simplified: 2-click placement
    if (['wall', 'door', 'utility'].includes(inst.activeTool)) {
        const sx = snap(groundPt.x, gs);
        const sy = snap(groundPt.z, gs);
        if (!inst.drawing) {
            inst.drawing = { type: inst.activeTool, points: [{ x: sx, y: sy }] };
        } else {
            inst.drawing.points.push({ x: sx, y: sy });
            if (inst.drawing.points.length >= 2) {
                finishPolyline(inst);
            }
        }
    }
}

function onPointerMove(inst, e) {
    const groundPt = getWorldHit(inst, e);
    if (!groundPt) return;
    const gs = inst.layout.gridSize;

    // Hover highlight (select mode only)
    if (inst.activeTool === 'select' && !inst.dragging) {
        const hitId = getElementHit(inst, e);
        setHover(inst, hitId);
        inst.renderer.domElement.style.cursor = hitId ? 'pointer' : 'default';
    }

    // Dragging element
    if (inst.dragging) {
        const d = inst.dragging;
        const dx = groundPt.x - d.startMouse.x;
        const dz = groundPt.z - d.startMouse.z;
        const newX = snap(d.origX + dx, gs);
        const newY = snap(d.origY + dz, gs);
        d.el.x = newX;
        d.el.y = newY;
        d.group.position.set(newX + (d.el.width || 0) / 2, 0, newY + (d.el.height || 0) / 2);
        return;
    }

    // Drawing preview (room rectangle)
    if (inst.drawing && inst.drawing.type === 'room') {
        inst.drawing.currentX = snap(groundPt.x, gs);
        inst.drawing.currentY = snap(groundPt.z, gs);
        updateDrawingPreview(inst);
    }
}

function onPointerUp(inst, e) {
    // Finish drag
    if (inst.dragging) {
        inst.controls.enabled = true;
        notifyChanged(inst, inst.dragging.el);
        inst.dragging = null;
        return;
    }

    // Finish room drawing
    if (inst.drawing && inst.drawing.type === 'room') {
        inst.controls.enabled = true;
        const d = inst.drawing;
        const gs = inst.layout.gridSize;
        const x = snap(Math.min(d.startX, d.currentX), gs);
        const y = snap(Math.min(d.startY, d.currentY), gs);
        const w = snap(Math.abs(d.currentX - d.startX), gs);
        const h = snap(Math.abs(d.currentY - d.startY), gs);

        // Remove preview
        if (d.preview) {
            inst.scene.remove(d.preview);
            d.preview.traverse(c => { if (c.geometry) c.geometry.dispose(); if (c.material) c.material.dispose(); });
        }

        if (w >= gs && h >= gs) {
            const newEl = {
                id: uid(), type: 'room',
                x, y, width: w, height: h,
                rotation: 0, label: 'Room',
                style: {}, points: null, locked: false, zIndex: 0,
            };
            inst.layout.elements.push(newEl);
            createMesh(inst, newEl);
            setSelection(inst, newEl.id);
            notifyChanged(inst, newEl);
        }
        inst.drawing = null;
    }
}

function onKeyDown(inst, e) {
    if (e.key === 'Delete' || e.key === 'Backspace') {
        deleteSelectedInternal(inst);
    } else if (e.key === 'Escape') {
        if (inst.drawing) {
            if (inst.drawing.preview) {
                inst.scene.remove(inst.drawing.preview);
            }
            inst.drawing = null;
            inst.controls.enabled = true;
        } else {
            setSelection(inst, null);
        }
    }
}

// ---------------------------------------------------------------------------
// Drawing preview (transparent rectangle while dragging to create a room)
// ---------------------------------------------------------------------------

function updateDrawingPreview(inst) {
    const d = inst.drawing;
    if (!d) return;

    const x = Math.min(d.startX, d.currentX);
    const y = Math.min(d.startY, d.currentY);
    const w = Math.abs(d.currentX - d.startX);
    const h = Math.abs(d.currentY - d.startY);

    if (w < 1 || h < 1) return;

    // Remove old preview
    if (d.preview) {
        inst.scene.remove(d.preview);
        d.preview.traverse(c => { if (c.geometry) c.geometry.dispose(); if (c.material) c.material.dispose(); });
    }

    const geo = new THREE.BoxGeometry(w, 10, h);
    const mat = new THREE.MeshStandardMaterial({
        color: TYPE_COLORS.room.base, transparent: true, opacity: 0.35,
    });
    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.set(x + w / 2, 5, y + h / 2);

    const edgeGeo = new THREE.EdgesGeometry(geo);
    const edgeMat = new THREE.LineBasicMaterial({ color: TYPE_COLORS.room.select });
    const edges = new THREE.LineSegments(edgeGeo, edgeMat);
    edges.position.copy(mesh.position);

    const group = new THREE.Group();
    group.add(mesh);
    group.add(edges);
    inst.scene.add(group);
    d.preview = group;
}

// ---------------------------------------------------------------------------
// Polyline finalisation (wall/door/utility)
// ---------------------------------------------------------------------------

function finishPolyline(inst) {
    const d = inst.drawing;
    if (!d || d.points.length < 2) { inst.drawing = null; return; }

    const originX = d.points[0].x;
    const originY = d.points[0].y;
    const pts = d.points.map(p => ({ x: p.x - originX, y: p.y - originY }));

    const newEl = {
        id: uid(), type: d.type,
        x: originX, y: originY, width: 0, height: 0,
        rotation: 0, label: labelFor(d.type),
        style: {}, points: pts, locked: false, zIndex: 0,
    };

    inst.layout.elements.push(newEl);
    createMesh(inst, newEl);
    setSelection(inst, newEl.id);
    notifyChanged(inst, newEl);
    inst.drawing = null;
}

function labelFor(type) {
    return { room: 'Room', workstation: 'Workstation', inventory: 'Storage',
             utility: 'Utility', wall: '', door: '', annotation: 'Text' }[type] || type;
}

// ---------------------------------------------------------------------------
// Notification helpers
// ---------------------------------------------------------------------------

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

    // Remove 3D mesh
    const grp = inst.meshMap.get(inst.selectedId);
    if (grp) {
        inst.scene.remove(grp);
        grp.traverse(c => {
            if (c.geometry) c.geometry.dispose();
            if (c.material) {
                if (c.material.map) c.material.map.dispose();
                c.material.dispose();
            }
        });
        inst.meshMap.delete(inst.selectedId);
    }

    inst.layout.elements.splice(idx, 1);
    const deletedId = inst.selectedId;
    inst.selectedId = null;
    notifyChanged(inst, { ...el, _deleted: true });
}

// ---------------------------------------------------------------------------
// Exported API (matches the contract FactoryDesignEditor.razor expects)
// ---------------------------------------------------------------------------

export function updateLayout(containerId, layoutJson) {
    const inst = instances.get(containerId);
    if (!inst) return;

    // Remove all existing meshes
    for (const [, grp] of inst.meshMap) {
        inst.scene.remove(grp);
        grp.traverse(c => {
            if (c.geometry) c.geometry.dispose();
            if (c.material) c.material.dispose();
        });
    }
    inst.meshMap.clear();

    const raw = JSON.parse(layoutJson);
    inst.layout = {
        width:  raw.width  || raw.canvasWidth  || 5000,
        height: raw.height || raw.canvasHeight  || 3000,
        gridSize: raw.gridSize || 500,
        backgroundColor: raw.backgroundColor || '#f5f5f5',
        elements: raw.elements || [],
    };

    for (const el of inst.layout.elements) createMesh(inst, el);
    inst.selectedId = null;
}

export function getLayoutJson(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return '{}';
    return JSON.stringify({
        canvasWidth:     inst.layout.width,
        canvasHeight:    inst.layout.height,
        gridSize:        inst.layout.gridSize,
        backgroundColor: inst.layout.backgroundColor,
        elements:        inst.layout.elements,
    });
}

export function setActiveTool(containerId, toolName) {
    const inst = instances.get(containerId);
    if (!inst) return;
    inst.activeTool = toolName;
    inst.drawing = null;
    inst.controls.enabled = true;
    if (toolName !== 'select') {
        setSelection(inst, null);
        inst.renderer.domElement.style.cursor = 'crosshair';
    } else {
        inst.renderer.domElement.style.cursor = 'default';
    }
}

export function zoomIn(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return;
    inst.camera.position.lerp(inst.controls.target, 0.2);
    inst.camera.updateProjectionMatrix();
}

export function zoomOut(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return;
    const dir = inst.camera.position.clone().sub(inst.controls.target).normalize();
    inst.camera.position.addScaledVector(dir, Math.max(inst.layout.width, inst.layout.height) * 0.15);
    inst.camera.updateProjectionMatrix();
}

export function resetView(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return;
    const l = inst.layout;
    const camDist = Math.max(l.width, l.height) * 0.75;
    inst.camera.position.set(l.width / 2, camDist, l.height * 0.9);
    inst.controls.target.set(l.width / 2, 0, l.height / 2);
    inst.controls.update();
}

export function deleteSelected(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return;
    deleteSelectedInternal(inst);
}

export function dispose(containerId) {
    const inst = instances.get(containerId);
    if (!inst) return;

    if (inst.animId) cancelAnimationFrame(inst.animId);

    const canvas = inst.renderer.domElement;
    canvas.removeEventListener('pointerdown', inst.handlers.pointerdown);
    canvas.removeEventListener('pointermove', inst.handlers.pointermove);
    canvas.removeEventListener('pointerup',   inst.handlers.pointerup);
    canvas.removeEventListener('keydown',      inst.handlers.keydown);
    window.removeEventListener('resize', inst.handlers.resize);
    if (inst.resizeObserver) inst.resizeObserver.disconnect();

    inst.controls.dispose();
    inst.renderer.dispose();

    // Clean up all geometries/materials
    inst.scene.traverse(child => {
        if (child.geometry) child.geometry.dispose();
        if (child.material) {
            if (Array.isArray(child.material)) child.material.forEach(m => m.dispose());
            else child.material.dispose();
        }
    });

    instances.delete(containerId);
    console.log(`[Factory3D] Disposed '${containerId}'.`);
}
