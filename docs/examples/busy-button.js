// Attach to the Button. Name a LoadingOverlay "busyOverlay" and Toast "savedToast".
let pending;
api.on("click", () => {
  clearTimeout(pending);
  api.set("busyOverlay", "isActive", true);
  pending = setTimeout(() => {
    api.set("busyOverlay", "isActive", false);
    api.showToast("savedToast");
  }, 1000);
});
api.cleanup(() => clearTimeout(pending));
