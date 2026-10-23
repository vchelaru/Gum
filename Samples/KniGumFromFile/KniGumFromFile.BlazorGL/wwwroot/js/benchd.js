// SPIKE #6156 (not for merge): browser-native PNG decode + WebGL upload timings.
(function () {
    function median(a) { var s = a.slice().sort(function (x, y) { return x - y; }); return s[Math.floor(s.length / 2)]; }

    var benchGl = null;
    function getBenchGl() {
        if (benchGl) return benchGl;
        var c = document.createElement('canvas');
        c.width = 4; c.height = 4;
        benchGl = c.getContext('webgl2') || c.getContext('webgl');
        return benchGl;
    }

    // Forces the upload to finish by reading one texel through a framebuffer.
    function syncTexture(gl, tex) {
        var fb = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, fb);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
        var px = new Uint8Array(4);
        gl.readPixels(0, 0, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, px);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.deleteFramebuffer(fb);
    }

    function readAll(gl, tex, w, h) {
        var fb = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, fb);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
        var px = new Uint8Array(4 * w * h);
        gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, px);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.deleteFramebuffer(fb);
        return Array.from(px);
    }

    function upload(gl, source, premultiply) {
        var tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, premultiply);
        gl.pixelStorei(gl.UNPACK_COLORSPACE_CONVERSION_WEBGL, gl.NONE);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, source);
        gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, false);
        return tex;
    }

    async function decodeBitmap(u8, premultiply) {
        var blob = new Blob([u8], { type: 'image/png' });
        return await createImageBitmap(blob, { premultiplyAlpha: premultiply ? 'premultiply' : 'none', colorSpaceConversion: 'none' });
    }

    async function decodeImg(u8) {
        var url = URL.createObjectURL(new Blob([u8], { type: 'image/png' }));
        var img = new Image();
        img.src = url;
        await img.decode();
        URL.revokeObjectURL(url);
        return img;
    }

    window.gumBenchNop = function (u8) { return u8.length; };

    // Returns { key: median ms } for createImageBitmap premult/straight and HTMLImageElement premult.
    window.gumBenchD = async function (u8, reps) {
        var gl = getBenchGl();
        var r = {};
        function add(k, v) { (r[k] = r[k] || []).push(v); }
        for (var i = 0; i < reps; i++) {
            for (var variant of ['bmpPremul', 'bmpStraight', 'imgPremul']) {
                var t0 = performance.now();
                var premul = variant !== 'bmpStraight';
                var src = variant === 'imgPremul' ? await decodeImg(u8) : await decodeBitmap(u8, premul);
                var t1 = performance.now();
                var tex = upload(gl, src, premul);
                syncTexture(gl, tex);
                var t2 = performance.now();
                if (src.close) src.close();
                gl.deleteTexture(tex);
                if (i === 0) continue; // warmup
                add(variant + '.decode', t1 - t0);
                add(variant + '.upload', t2 - t1);
                add(variant + '.total', t2 - t0);
            }
        }
        var out = {};
        for (var k in r) out[k] = median(r[k]);
        out.glVersion = gl.getParameter(gl.VERSION);
        return JSON.stringify(out);
    };

    // Exact texel values after browser decode + upload, for precision checks.
    window.gumBenchDPixels = async function (u8, w, h) {
        var gl = getBenchGl();
        var out = {};
        var bp = await decodeBitmap(u8, true);
        out.bmpPremul = readAll(gl, upload(gl, bp, true), w, h);
        var bs = await decodeBitmap(u8, false);
        out.bmpStraight = readAll(gl, upload(gl, bs, false), w, h);
        var im = await decodeImg(u8);
        out.imgPremul = readAll(gl, upload(gl, im, true), w, h);
        out.imgStraight = readAll(gl, upload(gl, im, false), w, h);
        return JSON.stringify(out);
    };

    // Proof of wrapping: upload into a texture KNI already created, on KNI's own GL context.
    // KNI registers every JS object in nkJSObject.objectMap; take the newest WebGLTexture.
    window.gumBenchDIntoKni = async function (u8) {
        var map = nkJSObject.objectMap;
        var gl = null, tex = null;
        for (var i = map.length - 1; i >= 0; i--) {
            var o = map[i];
            if (!tex && o instanceof WebGLTexture) tex = o;
            if (!gl && ((typeof WebGL2RenderingContext !== 'undefined' && o instanceof WebGL2RenderingContext) || o instanceof WebGLRenderingContext)) gl = o;
        }
        if (!gl || !tex) return 'not found gl=' + !!gl + ' tex=' + !!tex;
        var t0 = performance.now();
        var bmp = await decodeBitmap(u8, true);
        var t1 = performance.now();
        var prevTex = gl.getParameter(gl.TEXTURE_BINDING_2D);
        var prevPremul = gl.getParameter(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL);
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, true);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, bmp);
        gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, prevPremul);
        gl.bindTexture(gl.TEXTURE_2D, prevTex);
        var t2 = performance.now();
        bmp.close();
        return 'ok uid=' + tex.nkUid + ' decode=' + (t1 - t0).toFixed(1) + ' upload=' + (t2 - t1).toFixed(1);
    };
})();

// SPIKE #6156 (not for merge): raw-pixel gum.pkg bench helpers. BrotliDecode is KNI's template decoder (index.html).
window.gumBrotliJs = function (bytes) {
    const out = window.BrotliDecode(new Int8Array(bytes.buffer, bytes.byteOffset, bytes.byteLength));
    return new Uint8Array(out.buffer, out.byteOffset, out.byteLength);
};
window.gumBrotliJsTimed = function (bytes) {
    const input = new Int8Array(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const t0 = performance.now();
    window.BrotliDecode(input);
    return performance.now() - t0;
};
window.gumDecompressionStreamFormats = function () {
    const ok = [];
    for (const f of ['gzip', 'deflate', 'deflate-raw', 'brotli', 'br']) {
        try { new DecompressionStream(f); ok.push(f + ':yes'); } catch (e) { ok.push(f + ':no'); }
    }
    return navigator.userAgent + ' ' + ok.join(',');
};
