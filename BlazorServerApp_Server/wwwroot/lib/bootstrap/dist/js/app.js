window.blazorPageHistory = {
    // Saves an array of strings to localStorage
    save: function (historyArray) {
        try {
            localStorage.setItem('pageHistory', JSON.stringify(historyArray));
        } catch (e) {
            console.error("Error saving to localStorage:", e);
        }
    },
    // Loads history from localStorage as an array of strings
    load: function () {
        try {
            var history = localStorage.getItem('pageHistory');
            return history ? JSON.parse(history) : [];
        } catch (e) {
            console.error("Error loading from localStorage:", e);
            return []; // Return empty array on error
        }
    },
    // Clears the history from localStorage
    clear: function () {
        try {
            localStorage.removeItem('pageHistory');
        } catch (e) {
            console.error("Error clearing localStorage:", e);
        }
    }
};