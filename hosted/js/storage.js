window.crewhopStorage = {
  get(key) {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set(key, value) {
    try {
      localStorage.setItem(key, value);
    } catch {
      // Ignore quota / private mode failures in the demo.
    }
  },
  remove(key) {
    try {
      localStorage.removeItem(key);
    } catch {
      // Ignore.
    }
  }
};

window.crewhopImage = {
  async readAsDataUrl(file, maxWidth = 1200, quality = 0.82) {
    if (!file) {
      return null;
    }

    const bitmap = await createImageBitmap(file);
    const scale = Math.min(1, maxWidth / bitmap.width);
    const width = Math.max(1, Math.round(bitmap.width * scale));
    const height = Math.max(1, Math.round(bitmap.height * scale));
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(bitmap, 0, 0, width, height);
    bitmap.close?.();
    return canvas.toDataURL("image/jpeg", quality);
  },

  async fromStream(streamRef, contentType, maxWidth = 1200, quality = 0.82) {
    const data = await streamRef.arrayBuffer();
    const type = contentType || "image/jpeg";
    const blob = new Blob([data], { type });
    try {
      return await window.crewhopImage.readAsDataUrl(blob, maxWidth, quality);
    } catch {
      const bytes = new Uint8Array(data);
      let binary = "";
      for (let i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
      }
      return `data:${type};base64,${btoa(binary)}`;
    }
  }
};
