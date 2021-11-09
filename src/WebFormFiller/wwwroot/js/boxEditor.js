/*

NOTE:

When embedding on a page, you must define these variables BEFORE importing this script:
 * basePdfSourceUrl    -- URL from where the PDF can be loaded
 * projectJsonLoadUrl  -- URL from where we can load the JSON definition of the template project
 * projectJsonStoreUrl -- URL to which we can post an updated JSON definition of the template project
 * boxEditPartialUrl   -- URL for template box partial view
 * pdfWorkerSource     -- URL of the file at ~/js/pdf.worker.js
 * boxMoveUrl          -- URL used to send back box movements & resizes

 */

// Event bindings
document.getElementById('prev').addEventListener('click', onPrevPage);
document.getElementById('next').addEventListener('click', onNextPage);
document.getElementById('zoom-plus').addEventListener('click', onPageZoomPlus);
document.getElementById('zoom-minus').addEventListener('click', onPageZoomMinus);

// Read PDF.js exports from the ~/js/pdf.js file
const pdfJsLib = window['pdfjs-dist/build/pdf'];
pdfJsLib.GlobalWorkerOptions.workerSrc = pdfWorkerSource; // Setup the workerSrc property, as required by the library.


// PDF RENDERING ======================================================================================================
let pdfDoc = null,
    pageNum = 1,
    pageRendering = false,
    pageNumPending = null,
    scale = 2.0, // current zoom level of the PDF
    pdfCanvas = document.getElementById('pdf-render'),
    pdfCtx = pdfCanvas.getContext('2d');

// Box rendering bits
const outerContainer = document.getElementById("container");
const boxCanvas = document.getElementById("box-render");
const boxCtx = boxCanvas.getContext("2d");


/**
 * Get page info from document, resize canvas accordingly, and render page.
 * num is Page number.
 */
function renderPage(num) {
    pageRendering = true;
    // Using promise to fetch the page
    pdfDoc.getPage(num).then(function (page) {
        const viewport = page.getViewport({scale: scale});
        console.dir(viewport);
        console.log(`Native PDF page size is ${viewport.height}x${viewport.width}`);

        pdfCanvas.height = viewport.height;
        pdfCanvas.width = viewport.width;

        boxCanvas.height = viewport.height;
        boxCanvas.width = viewport.width;

        // Render PDF page into canvas context
        const renderContext = {
            canvasContext: pdfCtx,
            viewport: viewport
        };
        const renderTask = page.render(renderContext);

        // Wait for rendering to finish
        renderTask.promise.then(function () {
            pageRendering = false;
            if (pageNumPending !== null) {
                // New page rendering is pending
                renderPage(pageNumPending);
                pageNumPending = null;
            } else {
                // page rendering has settled, we can trigger box draw
                renderBoxes();
            }
        });
    });

    // Update page counters
    document.getElementById('page_num').textContent = num;
}

/**
 * This is the main call to trigger rendering.
 * If another page rendering in progress, waits until the rendering is
 * finished. Otherwise, executes rendering immediately.
 */
function queueRenderPage(num) {
    if (pageRendering) {
        pageNumPending = num;
    } else {
        renderPage(num);
    }
}


const zoomRatio = 1.2;

function onPageZoomPlus() {
    if (scale >= 10) return;
    scale *= zoomRatio;

    // update any selected boxes for the new screen space
    activeBox.left *= zoomRatio;
    activeBox.top *= zoomRatio;
    activeBox.right *= zoomRatio;
    activeBox.bottom *= zoomRatio;

    queueRenderPage(pageNum);
}

function onPageZoomMinus() {
    if (scale <= 0.5) return;
    scale /= zoomRatio;

    // update any selected boxes for the new screen space
    activeBox.left /= zoomRatio;
    activeBox.top /= zoomRatio;
    activeBox.right /= zoomRatio;
    activeBox.bottom /= zoomRatio;

    queueRenderPage(pageNum);
}

// Click event for loading previous page
function onPrevPage() {
    if (pageNum <= 1) {
        return;
    }
    clearActiveBox();updateEditButton();
    pageNum--;
    queueRenderPage(pageNum);
}

// Click event for loading next page
function onNextPage() {
    if (pageNum >= pdfDoc.numPages) {
        return;
    }
    clearActiveBox();updateEditButton();
    pageNum++;
    queueRenderPage(pageNum);
}


// HTML MODAL CONTROL FOR BOX DETAIL EDITING ==========================================================================
function loadPartialToModal(url, targetId, nextAction) {
    let targetElement = document.getElementById(targetId);
    if (!targetElement) return;

    let oReq = new XMLHttpRequest();

    oReq.onerror = function (evt) {
        targetElement.innerText = "An error occurred while transferring this UI element";
        console.dir(evt);
    }
    oReq.onabort = function (evt) {
        targetElement.innerText = "The transfer has been canceled by the user.";
        console.dir(evt);
    }
    oReq.onload = function (e) {
        targetElement.innerHTML = oReq.responseText;
        if (nextAction) nextAction(e);
    }

    oReq.open('GET', url);
    oReq.send();

}

function showBoxEditModal() {
    if (!activeBox.key) {
        return;
    }
    if (!boxEditPartialUrl) {
        console.log('boxEditPartialUrl was not bound');
        return;
    }

    // Synthesise a url, then show the modal.
    // Calls to EditModalsController->TemplateBox(docId, pageIndex, boxKey)
    let pageIdx = pageNum - 1;

    let modal = document.getElementById('EditTemplateBox_BoxInfo');
    if (!modal) return;
    loadPartialToModal(`${boxEditPartialUrl}&pageIndex=${pageIdx}&boxKey=${activeBox.key}`,
        'EditTemplateBox_BoxInfo_Content', function () {
            modal.classList.add("active");
        });
}
function saveBoxEditChanges(){
    const formEle = document.getElementById('editTemplateBoxForm');
    if (!formEle) return;

    submit(formEle).then(function(response) {
        // close the modal box and refresh everything
        closeBoxEditModal();
        reloadProjectFile();
        queueRenderPage(pageNum);
    });
}
function closeBoxEditModal() {
    let modal = document.getElementById('EditTemplateBox_BoxInfo');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditTemplateBox_BoxInfo_Content');
    if (deadContent) deadContent.innerHTML = "";
}


function showDisplayFormatModal() {
    // TODO: implement this
    alert("Would make the display format modal visible now");
}
function closeDisplayFormatModal(){
    alert("not implemented");
}

function showDataPickerModal() {
    let modal = document.getElementById('EditTemplateBox_DataMap');
    let link = document.getElementById('dataPickerUrl');
    if (!modal || !link) return;

    loadPartialToModal(link.value, 'EditTemplateBox_DataMap_Content', function () {
        modal.classList.add("active");
    });
}

function closeDataPathPicker() {
    let modal = document.getElementById('EditTemplateBox_DataMap');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditTemplateBox_DataMap_Content');
    if (deadContent) deadContent.innerHTML = "";
}

// BOX DRAW AND INTERACTION ===========================================================================================
// Note, this dummy data is just here to help with IDE auto-fill and type checking
let projectFile = {
    Version: 0,
    SampleFileName: null,
    BasePdfFile: "/File/....pdf",
    Notes: "",
    Name: "Sample template",
    BaseFontSize: null,
    FontName: null,
    Pages: [
        {
            WidthMillimetres: 210.0,
            HeightMillimetres: 297.0,
            PageFontSize: null,
            Name: "Page 1",
            Notes: null,
            BackgroundImage: null,
            RepeatMode: {
                Repeats: false,
                DataPath: null
            },
            Boxes: {
                "Sample": {
                    WrapText: true,
                    ShrinkToFit: true,
                    BoxFontSize: 16,
                    Alignment: "BottomLeft",
                    DependsOn: "otherBoxName",
                    Top: 10,
                    Left: 10,
                    Width: 10,
                    Height: 10,
                    MappingPath: [
                        "path",
                        "is",
                        "here"
                    ],
                    DisplayFormat: {
                        Type: "DateFormat",
                        FormatParameters: {}
                    },
                    BoxOrder: 1
                }
            },
            PageDataFilters: {},
            RenderBackground: false
        }
    ],
    DataFilters: {}
};
let renderBoxes = null;

const ctx = boxCtx;

// Mouse co-ords and active box are in screen-space units
let activeBox = {key: null, top: 0, left: 0, right: 0, bottom: 0};
let mouse = {x: 0, y: 0, buttons: 0, mode: 'none', xControl:'right', yControl:'bottom'};
let last_mouse = {x: 0, y: 0};

function clearActiveBox(){
    activeBox.key = null;
    activeBox.top = activeBox.left = activeBox.right = activeBox.bottom = 0;
}
function updateEditButton(){
    let infoSpan = document.getElementById("active-box-name")
    let editButton = document.getElementById("box-edit");
    if (infoSpan) {
        infoSpan.innerText = " " + (activeBox.key || "");
    }
    if (editButton) {
        editButton.disabled = (activeBox.key === null);
    }
}
function tryUpdateActiveBox(){
    if (activeBox.key === null) return;
    let pageDef = projectFile.Pages[pageNum - 1];
    if (!pageDef) return;
    let theBox = pageDef.Boxes[activeBox.key];
    if (!theBox) return;

    // un-apply scaling factor to get document-space units back
    let xi = pageDef.WidthMillimetres / boxCanvas.width;
    let yi = pageDef.HeightMillimetres / boxCanvas.height;
    
    // update the local definition
    theBox.Top = activeBox.top * yi;
    theBox.Left = activeBox.left * xi;
    theBox.Width = (activeBox.right - activeBox.left) * xi;
    theBox.Height = (activeBox.bottom - activeBox.top) *xi;
    
    // Store back to server. We do this fire-and-forget, as we might overwrite the whole document template definition later.
    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (req.responseText !== 'OK') {
            console.log("An error occurred while updating document template box location.");
            console.dir(evt);
        }
    }
    
    // This calls HomeController->MoveBox(docId, docVersion, pageIndex, boxKey, left, top, width, height)
    let url = `${boxMoveUrl}&docVersion=${projectFile.Version}&pageIndex=${pageNum-1}&boxKey=${activeBox.key}&left=${theBox.Left}&top=${theBox.Top}&width=${theBox.Width}&height=${theBox.Height}`;
    req.open('GET', url);
    req.send();
}

function tryCreateNewBox(x, y){
    // try to find a safe new name for this box
    let page = projectFile.Pages[pageNum-1];
    let name, found = false;
    for (let i = 1; i < 150; i++) {
        name = `Box_${i}`;
        if (page.Boxes[name]) continue;
        
        found = true;
        break;
    }
    
    if (!found) {
        alert("Too many boxes with default names. Please rename some.");
        return;
    }

    // scaling factor to change screen-space to document-space units
    let xi = page.WidthMillimetres / boxCanvas.width;
    let yi = page.HeightMillimetres / boxCanvas.height;
    
    page.Boxes[name] =  {
        WrapText: true,
        ShrinkToFit: true,
        BoxFontSize: null,
        Alignment: "TopLeft",
        DependsOn: null,
        Top: y*yi,
        Left: x*xi,
        Width: 10,
        Height: 10,
        MappingPath: null,
        DisplayFormat: null,
        BoxOrder: null
    }
    
    storeAndReloadProjectFile();
}

const drawSingleBox = function (def, name, xs, ys, validMapping) {
    if (!def) {
        console.log("Invalid box def: " + JSON.stringify(def));
        return;
    }
    if (name === activeBox.key) return; // this box is selected. Don't render it here.

    if (validMapping) {
        ctx.fillStyle = "rgba(80, 100, 200, 0.3)";
        ctx.strokeStyle = "#07F";
        ctx.lineWidth = 2;
    } else {
        ctx.fillStyle = "rgba(255, 80, 80, 0.3)";
        ctx.strokeStyle = "#F00";
        ctx.lineWidth = 3;
    }

    // Draw box in place over PDF
    const {Width, Top, Height, Left} = def;
    ctx.fillRect(Left * xs, Top * ys, Width * xs, Height * ys);
    ctx.strokeRect(Left * xs, Top * ys, Width * xs, Height * ys);

    // Write box name
    ctx.fillStyle = "#000";
    ctx.font = '14px sans-serif';
    ctx.textBaseline = 'top';
    ctx.fillText(name, Left * xs + 5, Top * xs + 5);
}

const drawActiveBox = function () {
    let width = activeBox.right - activeBox.left;
    let height = activeBox.bottom - activeBox.top;
    if (width < 1 || height < 1) return; // box is zero sized

    ctx.fillStyle = "rgba(255, 190, 80, 0.3)";
    ctx.strokeStyle = "#FA0";
    ctx.lineWidth = 3;

    // The box
    ctx.fillRect(activeBox.left, activeBox.top, width, height);
    ctx.strokeRect(activeBox.left, activeBox.top, width, height);

    // resize handles
    ctx.fillStyle = "#FA0";
    ctx.strokeStyle = "#A50";
    ctx.lineWidth = 1;
    ctx.fillRect(activeBox.left, activeBox.top, -10, -10);
    ctx.strokeRect(activeBox.left, activeBox.top, -10, -10);
    ctx.fillRect(activeBox.right, activeBox.top, 10, -10);
    ctx.strokeRect(activeBox.right, activeBox.top, 10, -10);
    ctx.fillRect(activeBox.right, activeBox.bottom, 10, 10);
    ctx.strokeRect(activeBox.right, activeBox.bottom, 10, 10);
    ctx.fillRect(activeBox.left, activeBox.bottom, -10, 10);
    ctx.strokeRect(activeBox.left, activeBox.bottom, -10, 10);

    // Write box name
    ctx.fillStyle = "#000";
    ctx.font = '14px sans-serif';
    ctx.textBaseline = 'top';
    ctx.fillText(activeBox.key, activeBox.left + 5, activeBox.top + 5);
}

function hitTestBoxes(page, mx, my) {
    // first, test the selected box's control points
    if (activeBox.key !== null) {
        if (mx + 10 >= activeBox.left && mx - 10 <= activeBox.right 
         && my + 10 >= activeBox.top  && my - 10 <= activeBox.bottom
         && (mx < activeBox.left || mx > activeBox.right)
         && (my < activeBox.top || my > activeBox.bottom)
        ){
            // We've hit one of the control points
            // store which of the box's dimensions we should adjust
            mouse.xControl = (mx <= activeBox.left) ? 'left' : 'right';
            mouse.yControl = (my <= activeBox.top) ? 'top' : 'bottom';
            return 'size';
        }
    }

    // Not in a control point, so look for box body hits
    let xScale = boxCanvas.width / page.WidthMillimetres;
    let yScale = boxCanvas.height / page.HeightMillimetres;
    for (let name in page.Boxes) {
        let def = page.Boxes[name];
        let {Width, Top, Height, Left} = def;
        Width *= xScale;
        Top *= yScale;
        Left *= xScale;
        Height *= yScale;

        if (mx >= Left && my >= Top) {
            if (mx - Left <= Width && my - Top <= Height) {
                // Did we hit the already selected box?
                if (activeBox.key === name) return 'move';

                // otherwise, select this new one
                activeBox.key = name;
                activeBox.top = Top;
                activeBox.left = Left;
                activeBox.right = Left + Width;
                activeBox.bottom = Top + Height;
                return 'select'; // hit the box body of a box we didn't select before.
            }
        }
    }

    // Missed everything, de-select any currently selected box
    clearActiveBox();
    return 'none';
}

//////////////////////////////////////////////////////////////////////////////////////////////////// PAINT ALL
renderBoxes = function () {
    ctx.clearRect(0, 0, boxCanvas.width, boxCanvas.height);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.translate(0.5, 0.5); // makes box edges a bit sharpers

    if (!projectFile.Pages) {
        console.log("BAD DEFINITION");
        console.dir(projectFile);
        return;
    }
    let pageDef = projectFile.Pages[pageNum - 1];
    if (!pageDef) {
        console.log("Bad def on page " + pageNum);
        return;
    }

    let xScale = boxCanvas.width / pageDef.WidthMillimetres;
    let yScale = boxCanvas.height / pageDef.HeightMillimetres;

    for (let name in pageDef.Boxes) {
        let def = pageDef.Boxes[name];
        let active = def.MappingPath && (def.MappingPath.length > 1);
        drawSingleBox(def, name, xScale, yScale, active);
    }

    // Draw the 'active' box if one is being created
    drawActiveBox();
};


//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE DOWN
boxCanvas.addEventListener('mouseout', function () {
    mouse.buttons = 0;
}); // prevent drag-lock
boxCanvas.addEventListener('mousedown', function (e) {
    let mx = 0 | e.offsetX;
    let my = 0 | e.offsetY;

    // TODO: handle initial click (right or left?) -- might want to pop up the details modal
    // Cases:
    // * Hit one of the active box's control points (~10px of the corner) -> re-size
    // * Hit the body of the active box -> move
    // * Hit the body of a non-active box -> select
    // * Hit no box -> either start new box, or do nothing (depending on mode)

    let page = projectFile.Pages[pageNum - 1];
    mouse.mode = hitTestBoxes(page, mx, my);
    updateEditButton();


    if ((e.buttons === 2 || e.shiftKey) && mouse.mode === 'none') {
        // initial right-click. If we drag from here, a new box should be created
        e.preventDefault();
        tryCreateNewBox(mx,my);
        renderBoxes();
    }
}, false);

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE UP
boxCanvas.addEventListener('mouseup', function (e) {
    renderBoxes();

    if (mouse.buttons === 2) {
        e.preventDefault();
    }
    if (mouse.mode === 'move' || mouse.mode === 'size') {
        tryUpdateActiveBox();
    }
}, false);

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE MOVE
boxCanvas.addEventListener('mousemove', function (e) {
    let newX = e.offsetX;
    let newY = e.offsetY;
    if (mouse.buttons !== 0) { // mouse is dragging
        last_mouse.x = mouse.x;
        last_mouse.y = mouse.y;
    } else { // mouse was hovering
        last_mouse.x = newX;
        last_mouse.y = newY;
    }

    mouse.buttons = e.buttons;
    mouse.x = newX;
    mouse.y = newY;

    // Check mouse.mode; Maybe move the current box 
    // When we click with nothing selected, we should be in select mode. Dragging has no effect on any box we just selected.
    // When we were in select mode, then clicked again, we should be in either drag or resize mode.
    //   Resize should set the right & bottom values of the active box.
    //   Move should offset all values.
    if (mouse.mode === 'select') {
        return; // No action. The user has to release the mouse and click again to edit.
    } else if (mouse.mode === 'move') {
        // Offset the active box
        let dx = mouse.x - last_mouse.x;
        let dy = mouse.y - last_mouse.y;
        activeBox.left += dx;
        activeBox.top += dy;
        activeBox.right += dx;
        activeBox.bottom += dy;
    } else if (mouse.mode === 'size') {
        // resize the active box
        activeBox[mouse.xControl] = mouse.x;
        activeBox[mouse.yControl] = mouse.y;
        // make sure the box is not invalid (we're measuring in screen-space here, zooming in will still let you place tiny boxes)
        if (activeBox.right + 5 < activeBox.left) activeBox.right = activeBox.left + 5;
        if (activeBox.bottom + 5 < activeBox.top) activeBox.bottom = activeBox.top + 5;
    }

    if (mouse.buttons !== 0) {
        renderBoxes();
    }

}, false);

// PROJECT LOAD AND STORE =============================================================================================

function storeAndReloadProjectFile(){
    // We always reload the project file, even after an error -- this is in case the error was due to an 
    // out-of-date project definition

    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (!(req.status >= 200 && req.status < 400)) {
            console.log("An error occurred while loading document template definition: " + req.responseText);
            console.dir(evt);
        }
        reloadProjectFile();
    }

    req.open('POST', projectJsonStoreUrl, true);
    req.setRequestHeader('Content-Type', 'application/json; charset=UTF-8');

    req.onerror = function (evt) {
        console.log("An error occurred while loading document template definition.");
        console.dir(evt);
        reloadProjectFile();
    };
    req.onabort = function (evt) {
        console.log("Loading document template definition has been canceled by a user action.");
        console.dir(evt);
        reloadProjectFile();
    };

    req.send(JSON.stringify(projectFile));
}

function reloadProjectFile() {
    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (req.status >= 200 && req.status < 400) {
            // store the project to variable, and trigger the box render code
            projectFile = req.response;
            renderBoxes();
        } else {
            console.log("An error occurred while loading document template definition.");
            console.dir(evt);
        }
    }

    req.open('GET', projectJsonLoadUrl, true);
    req.responseType = "json";

    req.onerror = function (evt) {
        console.log("An error occurred while loading document template definition.");
        console.dir(evt);
    };
    req.onabort = function (evt) {
        console.log("Loading document template definition has been canceled by a user action.");
        console.dir(evt);
    };

    req.send();
}

// PDF LOAD TRIGGER ===================================================================================================
// Actually load the PDF (async then call our page render)
// This requires a modern web browser that supports promises.
pdfJsLib.getDocument(basePdfSourceUrl).promise.then(function (pdfDoc_) {
    pdfDoc = pdfDoc_;
    document.getElementById('page_count').textContent = pdfDoc.numPages;

    // Initial/first page rendering
    reloadProjectFile();
    renderPage(pageNum);
});
