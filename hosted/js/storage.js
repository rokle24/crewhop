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
