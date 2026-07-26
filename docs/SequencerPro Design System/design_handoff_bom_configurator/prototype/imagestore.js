/* ============================================================
   BoM Configurator — author-defined preview image store
   window.PreviewStore

   The product preview is NOT hard-coded to the car. An author can
   upload a BASE image (the product in its default state) plus per-
   option OVERLAY images (ideally transparent PNGs) that composite
   on top when that option is selected — the standard layered-render
   approach used by real product configurators (robots, 3D printers,
   machinery, anything). Stored in localStorage as downscaled data
   URLs so it survives reloads. If no images are set, the preview
   falls back to the built-in schematic (the car example).
   ============================================================ */
(function () {
  const KEY = "sqpro.bomcfg.preview.v2"; // v2: option ids changed (car → robot platform)
  let cache = null;
  const subs = new Set();

  function load() {
    if (cache) return cache;
    try { cache = JSON.parse(localStorage.getItem(KEY)) || {}; } catch (e) { cache = {}; }
    if (!cache.overlays) cache.overlays = {};
    if (!cache.meta) cache.meta = {};
    return cache;
  }
  function save() {
    try { localStorage.setItem(KEY, JSON.stringify(cache)); }
    catch (e) { console.warn("PreviewStore: localStorage full — image not saved", e); }
    subs.forEach(f => { try { f(); } catch (_) {} });
  }

  // Read a File, downscale to maxDim, return a PNG data URL (keeps transparency).
  function fileToDataUrl(file, maxDim) {
    return new Promise((resolve, reject) => {
      if (!file || !/^image\//.test(file.type)) return reject(new Error("Not an image"));
      const fr = new FileReader();
      fr.onerror = () => reject(fr.error);
      fr.onload = () => {
        const img = new Image();
        img.onerror = () => reject(new Error("Bad image"));
        img.onload = () => {
          let w = img.naturalWidth, h = img.naturalHeight;
          const m = maxDim || 1000;
          if (w > m || h > m) { const s = Math.min(m / w, m / h); w = Math.round(w * s); h = Math.round(h * s); }
          const cv = document.createElement("canvas"); cv.width = w; cv.height = h;
          cv.getContext("2d").drawImage(img, 0, 0, w, h);
          resolve({ src: cv.toDataURL("image/png"), w, h });
        };
        img.src = fr.result;
      };
      fr.readAsDataURL(file);
    });
  }

  const api = {
    get() { return load(); },
    base() { return load().base || null; },
    overlay(id) { return load().overlays[id] || null; },
    setBase(rec) { load(); cache.base = rec.src; cache.meta.baseW = rec.w; cache.meta.baseH = rec.h; save(); },
    setOverlay(id, rec) { load(); cache.overlays[id] = rec.src; save(); },
    remove(id) { load(); if (id === "__base__") { delete cache.base; delete cache.meta.baseW; delete cache.meta.baseH; } else delete cache.overlays[id]; save(); },
    clearAll() { cache = { overlays: {}, meta: {} }; save(); },
    hasImages() { load(); return !!cache.base || Object.keys(cache.overlays).length > 0; },
    aspect() { load(); return (cache.meta.baseW && cache.meta.baseH) ? cache.meta.baseW / cache.meta.baseH : 16 / 9; },
    overlayCount() { return Object.keys(load().overlays).length; },
    subscribe(fn) { subs.add(fn); return () => subs.delete(fn); },
    fileToDataUrl,
  };
  window.PreviewStore = api;
})();
