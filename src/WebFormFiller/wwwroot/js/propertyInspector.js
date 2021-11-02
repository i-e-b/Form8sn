function toggle(btnID, direction) {
    let i;
    
    // Get the button that triggered this
    const button = document.getElementById(btnID);
    if (!button) {return;}

    const childRowIds = button.getAttribute("childIds");
    const childRowElements = document.querySelectorAll(childRowIds);

    // we specifically want to close, or the button is currently open
    if (direction === "close" || button.getAttribute("aria-expanded") !== "false") {
        // Loop through the rows and hide them
        for (i = 0; i < childRowElements.length; i++) {
            // if this row is also collapsable, AND is expanded, collapse it.
            if (childRowElements[i].classList.contains("shown")) {
                let childBtnId = "btn-" + childRowElements[i].id;
                let found = document.getElementById(childBtnId);
                if (found && found.onclick) {
                    toggle(childBtnId, "close");
                }
            }

            // Hide the row
            childRowElements[i].classList.add("hidden");
            childRowElements[i].classList.remove("shown");
        }
        // Now set the button to collapsed
        button.setAttribute("aria-expanded", "false"); 
    } else if (button.getAttribute("aria-expanded") === "false") {// The button is closed
        // Show each row
        for (i = 0; i < childRowElements.length; i++) {
            childRowElements[i].classList.add("shown");
            childRowElements[i].classList.remove("hidden");
        }
        // Update recorded state
        button.setAttribute("aria-expanded", "true");
    } else {
        console.log(`Toggle binding error: ${btnID}`);
    }
}