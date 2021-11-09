/**
 * Read a HTML form, and serialise its state to a URI-encoded string.
 * @param formEle the form to encode
 * @returns {string}
 */
const serialize = function (formEle) {
    // Get all fields
    const fields = [].slice.call(formEle.elements, 0);

    return fields
        .map(function (ele) {
            const name = ele.name;
            const type = ele.type;

            // We ignore
            // - field that don't have a name
            // - disabled fields
            // - file input types
            // - unselected checkbox/radio items
            if (!name || ele.disabled || type === 'file' || (/(checkbox|radio)/.test(type) && !ele.checked)) {
                return '';
            }

            // Multiple select
            if (type === 'select-multiple') {
                return ele.options
                    .map(function (opt) {
                        return opt.selected ? `${encodeURIComponent(name)}=${encodeURIComponent(opt.value)}` : '';
                    })
                    .filter(function (item) {
                        return item;
                    })
                    .join('&');
            }

            return `${encodeURIComponent(name)}=${encodeURIComponent(ele.value)}`;
        })
        .filter(function (item) {
            return item;
        })
        .join('&');
};

/**
 * Submit a form to its default target, but do so in a background request.
 * This doesn't change the current browser page.
 * @param formEle The form to submit
 * @returns {Promise<string>}
 */
const submit = function (formEle) {
    return new Promise(function (resolve, reject) {
        // Serialize form data
        const params = serialize(formEle);

        // Create new Ajax request
        const req = new XMLHttpRequest();
        req.open('POST', formEle.action, true);
        req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');

        // Handle the events
        req.onload = function () {
            if (req.status >= 200 && req.status < 400) {
                resolve(req.responseText);
            }
        };
        req.onerror = function () {
            reject();
        };

        // Send it
        req.send(params);
    });
};