mergeInto(LibraryManager.library, {
  NumberBlockContextMenu: function () {
    var c = Module['canvas'] || document.querySelector('canvas');
    if (c) c.addEventListener('contextmenu', function (e) { e.preventDefault(); });
  },

  // Focus the canvas on load and before every press, so the first click on the page is not spent on focusing it.
  NumberFocusCanvas: function () {
    var c = Module['canvas'] || document.querySelector('canvas');
    if (!c) return;
    if (!c.hasAttribute('tabindex')) c.setAttribute('tabindex', '-1');
    var focus = function () { window.focus(); c.focus(); };
    focus();
    c.addEventListener('pointerdown', focus, true);
  }
});
