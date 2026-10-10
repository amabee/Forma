// Attach to the Button. Name a LoadingOverlay "busyOverlay" and Toast "savedToast".
let pending;
forma.on("click", () => {
  clearTimeout(pending);
  forma.set("busyOverlay", "isActive", true);
  pending = setTimeout(() => {
    forma.set("busyOverlay", "isActive", false);
    forma.showToast("savedToast");
  }, 1000);
});
forma.cleanup(() => clearTimeout(pending));
