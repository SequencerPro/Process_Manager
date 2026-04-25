const DB_NAME = 'ProcessManagerOfflineQueue';
const DB_VERSION = 1;
const STORE_NAME = 'pendingResponses';

let db = null;
let syncInProgress = false;
let statusCallback = null;

function openDb() {
    return new Promise((resolve, reject) => {
        if (db) { resolve(db); return; }
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = (e) => {
            const d = e.target.result;
            if (!d.objectStoreNames.contains(STORE_NAME)) {
                const store = d.createObjectStore(STORE_NAME, { keyPath: 'clientId' });
                store.createIndex('stepExecutionId', 'stepExecutionId', { unique: false });
                store.createIndex('queuedAt', 'queuedAt', { unique: false });
            }
        };
        req.onsuccess = (e) => { db = e.target.result; resolve(db); };
        req.onerror = (e) => reject(e.target.error);
    });
}

async function enqueue(stepExecutionId, clientId, processStepContentId, stepTemplateContentId, responseValue, overrideNote) {
    const d = await openDb();
    return new Promise((resolve, reject) => {
        const tx = d.transaction(STORE_NAME, 'readwrite');
        tx.objectStore(STORE_NAME).put({
            clientId,
            stepExecutionId,
            processStepContentId: processStepContentId || null,
            stepTemplateContentId: stepTemplateContentId || null,
            responseValue,
            overrideNote: overrideNote || null,
            queuedAt: Date.now()
        });
        tx.oncomplete = () => { notifyStatus(); resolve(); };
        tx.onerror = (e) => reject(e.target.error);
    });
}

async function getPendingCount() {
    const d = await openDb();
    return new Promise((resolve, reject) => {
        const tx = d.transaction(STORE_NAME, 'readonly');
        const req = tx.objectStore(STORE_NAME).count();
        req.onsuccess = () => resolve(req.result);
        req.onerror = (e) => reject(e.target.error);
    });
}

async function getAllPending() {
    const d = await openDb();
    return new Promise((resolve, reject) => {
        const tx = d.transaction(STORE_NAME, 'readonly');
        const req = tx.objectStore(STORE_NAME).index('queuedAt').getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = (e) => reject(e.target.error);
    });
}

async function removeBatch(clientIds) {
    const d = await openDb();
    return new Promise((resolve, reject) => {
        const tx = d.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        for (const id of clientIds) store.delete(id);
        tx.oncomplete = () => { notifyStatus(); resolve(); };
        tx.onerror = (e) => reject(e.target.error);
    });
}

async function flushQueue(apiBaseUrl) {
    if (syncInProgress) return { synced: 0, failed: 0 };
    syncInProgress = true;
    let synced = 0, failed = 0;

    try {
        const items = await getAllPending();
        if (items.length === 0) return { synced: 0, failed: 0 };

        const grouped = {};
        for (const item of items) {
            if (!grouped[item.stepExecutionId]) grouped[item.stepExecutionId] = [];
            grouped[item.stepExecutionId].push(item);
        }

        for (const [seId, batch] of Object.entries(grouped)) {
            const payload = {
                items: batch.map(b => ({
                    clientId: b.clientId,
                    processStepContentId: b.processStepContentId,
                    stepTemplateContentId: b.stepTemplateContentId,
                    responseValue: b.responseValue,
                    overrideNote: b.overrideNote
                }))
            };

            try {
                const resp = await fetch(`${apiBaseUrl}/api/step-executions/${seId}/prompt-responses/batch`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                    credentials: 'include'
                });

                if (resp.ok || resp.status === 204) {
                    await removeBatch(batch.map(b => b.clientId));
                    synced += batch.length;
                } else {
                    failed += batch.length;
                }
            } catch {
                failed += batch.length;
            }
        }
    } finally {
        syncInProgress = false;
        notifyStatus();
    }

    return { synced, failed };
}

function notifyStatus() {
    if (statusCallback) {
        getPendingCount().then(count => {
            statusCallback.invokeMethodAsync('OnSyncStatusChanged', navigator.onLine, count, syncInProgress);
        }).catch(() => {});
    }
}

function init(dotnetRef) {
    statusCallback = dotnetRef;

    window.addEventListener('online', () => {
        notifyStatus();
        flushQueue(window.__operatorSyncApiBase || '').catch(() => {});
    });
    window.addEventListener('offline', notifyStatus);

    notifyStatus();
}

function setApiBase(url) {
    window.__operatorSyncApiBase = url;
}

function dispose() {
    statusCallback = null;
}

window.OperatorSync = { init, dispose, enqueue, flushQueue, getPendingCount, getAllPending, setApiBase };
