mergeInto(LibraryManager.library, {
SendResultToReactJS : function(jsonData) {
        try
        {
    		window.dispatchReactUnityEvent("SendResultToReactJS", UTF8ToString(jsonData));
        }
        catch (error)
        {
            console.error('Error in unityGameResultCallback: ' + error);
        }
    }
});