mergeInto(LibraryManager.library,
{
    Vibrate: function(duration)
    {
        if (typeof navigator.vibrate === "function") {
            navigator.vibrate(duration);
        }
    }
});