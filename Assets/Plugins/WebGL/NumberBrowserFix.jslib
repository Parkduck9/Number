mergeInto(LibraryManager.library, {
  // Right click = parry: keep the browser context menu off the game canvas.
  NumberBlockContextMenu: function () {
    var c = Module['canvas'] || document.querySelector('canvas');
    if (c) c.addEventListener('contextmenu', function (e) { e.preventDefault(); });
  }
});
