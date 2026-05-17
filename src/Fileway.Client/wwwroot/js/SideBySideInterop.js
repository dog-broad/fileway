window.SideBySideInterop = (function () {
    // Map from container element -> { dotNetRef, listeners }
    var _instances = new WeakMap();

    function init(container, dotNetRef) {
        if (!container) return;

        // Clean up any previous listeners on this container
        destroy(container);

        var isDragging = false;

        function getPercent(clientX) {
            var rect = container.getBoundingClientRect();
            if (rect.width === 0) return 50;
            var x = clientX - rect.left;
            return Math.min(100, Math.max(0, (x / rect.width) * 100));
        }

        function onPointerDown(e) {
            // Only start drag if mouse/pointer is over the divider handle or line area
            var divider = container.querySelector('.sbs-divider');
            if (!divider) return;
            var dRect = divider.getBoundingClientRect();
            var clientX = e.clientX !== undefined ? e.clientX : (e.touches && e.touches[0] ? e.touches[0].clientX : null);
            if (clientX === null) return;
            // Allow dragging if within 28px of divider center (half the 48px handle width + padding)
            var distFromDivider = Math.abs(clientX - (dRect.left + dRect.width / 2));
            if (distFromDivider > 32) return;
            isDragging = true;
            e.preventDefault();
        }

        function onPointerMove(e) {
            if (!isDragging) return;
            var clientX = e.clientX !== undefined ? e.clientX : (e.touches && e.touches[0] ? e.touches[0].clientX : null);
            if (clientX === null) return;
            var percent = getPercent(clientX);
            dotNetRef.invokeMethodAsync('SetDividerPosition', percent);
            e.preventDefault();
        }

        function onPointerUp(e) {
            isDragging = false;
        }

        container.addEventListener('mousedown', onPointerDown);
        container.addEventListener('mousemove', onPointerMove);
        container.addEventListener('mouseup', onPointerUp);
        container.addEventListener('mouseleave', onPointerUp);
        container.addEventListener('touchstart', onPointerDown, { passive: false });
        container.addEventListener('touchmove', onPointerMove, { passive: false });
        container.addEventListener('touchend', onPointerUp);

        _instances.set(container, {
            dotNetRef: dotNetRef,
            listeners: { onPointerDown, onPointerMove, onPointerUp }
        });
    }

    function destroy(container) {
        if (!container) return;
        var inst = _instances.get(container);
        if (!inst) return;
        var l = inst.listeners;
        container.removeEventListener('mousedown', l.onPointerDown);
        container.removeEventListener('mousemove', l.onPointerMove);
        container.removeEventListener('mouseup', l.onPointerUp);
        container.removeEventListener('mouseleave', l.onPointerUp);
        container.removeEventListener('touchstart', l.onPointerDown);
        container.removeEventListener('touchmove', l.onPointerMove);
        container.removeEventListener('touchend', l.onPointerUp);
        _instances.delete(container);
    }

    return { init: init, destroy: destroy };
})();
