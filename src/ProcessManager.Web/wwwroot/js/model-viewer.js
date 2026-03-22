import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { STLLoader } from 'three/addons/loaders/STLLoader.js';
import { OBJLoader } from 'three/addons/loaders/OBJLoader.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

console.log('[ModelViewer] model-viewer.js loaded (v8)');

const viewers = {};
let occtReady = null; // cached initialized occt-import-js instance

/**
 * Initialize occt-import-js WASM module for STEP/IGES parsing.
 * The global `occtimportjs` is loaded via <script> tag in App.razor.
 * The WASM binary (~8 MB) is fetched on first use and cached thereafter.
 */
async function getOcctModule() {
    if (!occtReady) {
        occtReady = (async () => {
            console.log('[ModelViewer] Waiting for occt-import-js to load...');
            // Wait for the deferred script to load
            let retries = 0;
            while (typeof window.occtimportjs === 'undefined' && retries < 50) {
                await new Promise(r => setTimeout(r, 200));
                retries++;
            }
            console.log(`[ModelViewer] occtimportjs check after ${retries} retries:`, typeof window.occtimportjs);
            if (typeof window.occtimportjs === 'undefined') {
                throw new Error('occt-import-js library not loaded. Check network/CDN.');
            }
            console.log('[ModelViewer] Initializing WASM module...');
            // Must provide locateFile so Emscripten finds the .wasm binary on the CDN
            // (otherwise it resolves relative to the page URL and 404s)
            const occt = await window.occtimportjs({
                locateFile: (file) => `https://cdn.jsdelivr.net/npm/occt-import-js@0.0.23/dist/${file}`
            });
            console.log('[ModelViewer] WASM module ready');
            return occt;
        })();
    }
    return occtReady;
}

/**
 * Fetch a STEP/IGES file, parse it with OpenCascade WASM, and return a THREE.Group.
 */
async function loadStepModel(modelUrl, fileExt, material) {
    console.log('[ModelViewer] loadStepModel called:', modelUrl, fileExt);
    const occt = await getOcctModule();

    console.log('[ModelViewer] Fetching STEP file...');
    const response = await fetch(modelUrl);
    console.log('[ModelViewer] Fetch response:', response.status, response.statusText,
        'content-type:', response.headers.get('content-type'),
        'content-length:', response.headers.get('content-length'));

    if (!response.ok) {
        const errorText = await response.text().catch(() => '(unable to read body)');
        console.error('[ModelViewer] Fetch failed:', response.status, errorText);
        throw new Error(`Failed to download model: HTTP ${response.status} ${response.statusText}`);
    }

    const buffer = await response.arrayBuffer();
    console.log('[ModelViewer] File size:', buffer.byteLength, 'bytes');

    if (buffer.byteLength === 0) {
        throw new Error('Downloaded file is empty (0 bytes).');
    }

    // Quick sanity check: STEP files start with "ISO-10303-21"
    const header = new TextDecoder().decode(new Uint8Array(buffer, 0, Math.min(64, buffer.byteLength)));
    console.log('[ModelViewer] File header:', JSON.stringify(header.substring(0, 60)));

    const fileBuffer = new Uint8Array(buffer);

    // Use the correct reader based on file extension
    const isIges = (fileExt === 'iges' || fileExt === 'igs');
    let result;
    try {
        console.log('[ModelViewer] Parsing with OCCT...', isIges ? 'ReadIgesFile' : 'ReadStepFile');
        if (isIges && typeof occt.ReadIgesFile === 'function') {
            result = occt.ReadIgesFile(fileBuffer, null);
        } else {
            result = occt.ReadStepFile(fileBuffer, null);
        }
    } catch (parseErr) {
        console.error('[ModelViewer] OCCT parse threw:', parseErr);
        throw new Error(`CAD engine failed to parse file: ${parseErr.message || parseErr}`);
    }

    console.log('[ModelViewer] OCCT parse result:', result);
    console.log('[ModelViewer] Meshes found:', result.meshes ? result.meshes.length : 0);

    if (!result.meshes || result.meshes.length === 0) {
        throw new Error('STEP/IGES file parsed but contained no geometry (0 meshes).');
    }

    const group = new THREE.Group();

    for (const mesh of result.meshes) {
        const geometry = new THREE.BufferGeometry();

        // occt-import-js may return plain Arrays instead of TypedArrays —
        // Three.js requires TypedArrays for BufferAttribute, so coerce them.
        const toFloat32 = (arr) => arr instanceof Float32Array ? arr : new Float32Array(arr);
        const toUint32  = (arr) => arr instanceof Uint32Array  ? arr : new Uint32Array(arr);

        // Positions (Float32)
        geometry.setAttribute(
            'position',
            new THREE.Float32BufferAttribute(toFloat32(mesh.attributes.position.array), 3)
        );

        // Normals if available
        if (mesh.attributes.normal) {
            geometry.setAttribute(
                'normal',
                new THREE.Float32BufferAttribute(toFloat32(mesh.attributes.normal.array), 3)
            );
        } else {
            geometry.computeVertexNormals();
        }

        // Index
        if (mesh.index) {
            geometry.setIndex(new THREE.BufferAttribute(toUint32(mesh.index.array), 1));
        }

        // Per-face colour from STEP body if present, otherwise use default material
        let meshMaterial = material;
        if (mesh.color) {
            meshMaterial = new THREE.MeshPhysicalMaterial({
                color: new THREE.Color(mesh.color[0], mesh.color[1], mesh.color[2]),
                metalness: 0.3,
                roughness: 0.4,
                clearcoat: 0.2,
                clearcoatRoughness: 0.3
            });
        }

        const threeMesh = new THREE.Mesh(geometry, meshMaterial);
        group.add(threeMesh);
    }

    return group;
}

function createViewer(containerId, modelUrl, fileExtension) {
    console.log('[ModelViewer] createViewer called:', { containerId, modelUrl, fileExtension });
    // Clean up existing viewer if any
    destroyViewer(containerId);

    const container = document.getElementById(containerId);
    if (!container) return;

    const width = container.clientWidth || 400;
    const height = container.clientHeight || 400;

    // Scene
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x1a1a2e);

    // Camera
    const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 10000);

    // Renderer
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.shadowMap.enabled = true;
    container.appendChild(renderer.domElement);

    // Orbit Controls
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.enablePan = true;
    controls.enableZoom = true;

    // Lighting
    const ambientLight = new THREE.AmbientLight(0x606080, 1.2);
    scene.add(ambientLight);

    const dirLight1 = new THREE.DirectionalLight(0xffffff, 1.5);
    dirLight1.position.set(5, 10, 7);
    dirLight1.castShadow = true;
    scene.add(dirLight1);

    const dirLight2 = new THREE.DirectionalLight(0x8888ff, 0.6);
    dirLight2.position.set(-5, -3, -5);
    scene.add(dirLight2);

    const dirLight3 = new THREE.DirectionalLight(0xffffff, 0.4);
    dirLight3.position.set(0, -5, 5);
    scene.add(dirLight3);

    // Grid helper
    const gridHelper = new THREE.GridHelper(100, 20, 0x333355, 0x222244);
    scene.add(gridHelper);

    // Loading indicator
    const loadingDiv = document.createElement('div');
    loadingDiv.style.cssText = 'position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);color:#8888cc;font-size:14px;text-align:center;z-index:10;';
    loadingDiv.innerHTML = '<div class="spinner-border spinner-border-sm text-primary mb-2" role="status"></div><br>Loading model...';
    container.style.position = 'relative';
    container.appendChild(loadingDiv);

    // Load model
    const ext = (fileExtension || '').toLowerCase().replace('.', '');
    const material = new THREE.MeshPhysicalMaterial({
        color: 0x6699cc,
        metalness: 0.3,
        roughness: 0.4,
        clearcoat: 0.2,
        clearcoatRoughness: 0.3
    });

    const onModelLoaded = (object) => {
        // Remove loading indicator
        if (loadingDiv.parentNode) loadingDiv.parentNode.removeChild(loadingDiv);

        // Compute bounding box and centre/scale the model
        const box = new THREE.Box3().setFromObject(object);
        const centre = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const scale = maxDim > 0 ? 20 / maxDim : 1;

        object.position.sub(centre);
        object.scale.setScalar(scale);
        object.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true;
                child.receiveShadow = true;
            }
        });

        scene.add(object);

        // Position camera
        camera.position.set(25, 20, 25);
        controls.target.set(0, 0, 0);
        controls.update();
    };

    const onError = (err) => {
        console.error('[ModelViewer] Model load error:', err);
        const msg = err?.message || String(err);
        loadingDiv.innerHTML = `<i class="bi bi-exclamation-triangle text-warning" style="font-size:1.5rem"></i><br><span class="text-warning">Failed to load model</span><br><small class="text-muted" style="max-width:300px;display:inline-block;word-break:break-word">${msg}</small>`;
    };

    if (ext === 'stl') {
        const loader = new STLLoader();
        loader.load(modelUrl, (geometry) => {
            geometry.computeVertexNormals();
            const mesh = new THREE.Mesh(geometry, material);
            onModelLoaded(mesh);
        }, undefined, onError);
    } else if (ext === 'obj') {
        const loader = new OBJLoader();
        loader.load(modelUrl, (obj) => {
            obj.traverse((child) => {
                if (child.isMesh) child.material = material;
            });
            onModelLoaded(obj);
        }, undefined, onError);
    } else if (ext === 'glb' || ext === 'gltf') {
        const loader = new GLTFLoader();
        loader.load(modelUrl, (gltf) => {
            onModelLoaded(gltf.scene);
        }, undefined, onError);
    } else if (ext === 'step' || ext === 'stp' || ext === 'iges' || ext === 'igs') {
        loadingDiv.innerHTML = '<div class="spinner-border spinner-border-sm text-primary mb-2" role="status"></div><br>Loading CAD engine...<br><small class="text-muted">First load downloads ~8 MB WASM module</small>';
        loadStepModel(modelUrl, ext, material)
            .then(onModelLoaded)
            .catch(onError);
    }

    // Animation loop
    let animId;
    const animate = () => {
        animId = requestAnimationFrame(animate);
        controls.update();
        renderer.render(scene, camera);
    };
    animate();

    // Resize handler
    const onResize = () => {
        const w = container.clientWidth;
        const h = container.clientHeight;
        if (w > 0 && h > 0) {
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
            renderer.setSize(w, h);
        }
    };
    window.addEventListener('resize', onResize);

    viewers[containerId] = { renderer, scene, camera, controls, animId, onResize };
}

function destroyViewer(containerId) {
    const viewer = viewers[containerId];
    if (!viewer) return;

    cancelAnimationFrame(viewer.animId);
    window.removeEventListener('resize', viewer.onResize);
    viewer.controls.dispose();
    viewer.renderer.dispose();

    const container = document.getElementById(containerId);
    if (container) {
        while (container.firstChild) container.removeChild(container.firstChild);
    }

    delete viewers[containerId];
}

// Expose to Blazor via window
window.ModelViewer = {
    init: createViewer,
    destroy: destroyViewer
};
