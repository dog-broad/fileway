window.SseClient = {
    _connections: {},

    open: function (url, dotnetHelper) {
        var id = Math.random().toString(36).substring(2);
        var es = new EventSource(url);
        this._connections[id] = es;

        es.onmessage = function (e) {
            dotnetHelper.invokeMethodAsync('OnMessage', e.data, e.lastEventId || '');
        };

        es.onerror = function () {
            dotnetHelper.invokeMethodAsync('OnError');
        };

        return id;
    },

    close: function (id) {
        var es = this._connections[id];
        if (es) {
            es.close();
            delete this._connections[id];
        }
    }
};
