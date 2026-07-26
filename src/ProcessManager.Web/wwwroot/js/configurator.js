// BoM Configurator — small JS interop helpers (clipboard, download, print).
window.BomConfigurator = {
    copyText: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (e) {
            // Fallback for non-secure contexts.
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            var ok = false;
            try { ok = document.execCommand('copy'); } catch (_) { ok = false; }
            document.body.removeChild(ta);
            return ok;
        }
    },

    downloadFile: function (filename, content, mime) {
        var blob = new Blob([content], { type: mime || 'text/plain' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1500);
    },

    // Print an arbitrary HTML document in an off-screen iframe so the ISO spec
    // sheet prints cleanly without fighting the app layout.
    printHtml: function (html) {
        var existing = document.getElementById('bom-print-frame');
        if (existing) existing.remove();
        var frame = document.createElement('iframe');
        frame.id = 'bom-print-frame';
        frame.style.position = 'fixed';
        frame.style.right = '0';
        frame.style.bottom = '0';
        frame.style.width = '0';
        frame.style.height = '0';
        frame.style.border = '0';
        document.body.appendChild(frame);
        var doc = frame.contentWindow.document;
        doc.open();
        doc.write(html);
        doc.close();
        var run = function () {
            try {
                frame.contentWindow.focus();
                frame.contentWindow.print();
            } catch (e) { /* ignore */ }
            setTimeout(function () { frame.remove(); }, 1000);
        };
        // Give fonts/layout a moment to settle.
        if (frame.contentWindow.document.readyState === 'complete') setTimeout(run, 300);
        else frame.onload = function () { setTimeout(run, 300); };
    }
};
