mergeInto(LibraryManager.library, {
OpenAuthWindow: function(url) {
        var strUrl = UTF8ToString(url);
        var width = 800;
        var height = 650;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 4;

        window.authWindow = window.open(
            strUrl,
            "GoogleAuthWindow",
            "width=" + width + ",height=" + height + ",left=" + left + ",top=" + top + ",resizable=yes,scrollbars=yes,status=yes"
        );

        // Listen for messages from the popup
        window.addEventListener("message", function(event) {
            if (event.data.type === "oauth-success") {
                window.authWindow.close();
                var token = event.data.token;
                unityInstance.SendMessage("GoogleAuthManager", "OnWebGLTokenReceived", token);
            }
            else if (event.data.type === "oauth-error") {
                window.authWindow.close();
                unityInstance.SendMessage("GoogleAuthManager", "OnWebGLAuthError", event.data.error || "Unknown error");
            }
        }, false);

        return true;
    },
    
    CloseAuthWindow: function() {
        if (window.authWindow)
        {
            window.authWindow.close();
            window.authWindow = null;
        }
    }
});