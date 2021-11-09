/*

NOTE:

When embedding on a page, you must define these variables BEFORE importing this script:
 * basePdfSourceUrl   -- URL from where the PDF can be loaded
 * projectJsonLoadUrl -- URL from where we can load the JSON definition of the template project
 * pdfWorkerSource    -- URL of the file at ~/js/pdf.worker.js

 */

// Event bindings
document.getElementById('prev').addEventListener('click', onPrevPage);
document.getElementById('next').addEventListener('click', onNextPage);
document.getElementById('zoom-plus').addEventListener('click', onPageZoomPlus);
document.getElementById('zoom-minus').addEventListener('click', onPageZoomMinus);

// Read PDF.js exports from the ~/js/pdf.js file
const pdfJsLib = window['pdfjs-dist/build/pdf'];
pdfJsLib.GlobalWorkerOptions.workerSrc = pdfWorkerSource; // Setup the workerSrc property, as required by the library.


// PDF rendering bits
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
    pdfDoc.getPage(num).then(function(page) {
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
        renderTask.promise.then(function() {
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
function onPageZoomPlus(){
    if (scale >= 15) return;
    scale *= zoomRatio;
    
    // update any selected boxes for the new screen space
    activeBox.left *= zoomRatio;
    activeBox.top *= zoomRatio;
    activeBox.right *= zoomRatio;
    activeBox.bottom *= zoomRatio;
    
    queueRenderPage(pageNum);
}
function onPageZoomMinus(){
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
    if (pageNum <= 1) { return; }
    pageNum--;
    queueRenderPage(pageNum);
}

// Click event for loading next page
function onNextPage() {
    if (pageNum >= pdfDoc.numPages) { return; }
    pageNum++;
    queueRenderPage(pageNum);
}


// BOX DRAW SCRIPTS
// Note, this dummy data is just here to help with IDE auto-fill and type checking
let projectFile = {
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
let activeBox = {key: null, top:0, left:0, right:0, bottom:0};
let mouse = {x: 0, y: 0, buttons:0, mode:'none'};
let last_mouse = {x: 0, y: 0};

const drawSingleBox = function(def, name, xs, ys, validMapping){
                                                           if (!def){
                                                           console.log("Invalid box def: "+JSON.stringify(def));
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
                                                           const {Width,Top,Height,Left} = def;
                                                           ctx.fillRect(Left*xs, Top*ys, Width*xs, Height*ys);
                                                           ctx.strokeRect(Left*xs, Top*ys, Width*xs, Height*ys);
                                                           
                                                           // Write box name
                                                           ctx.fillStyle = "#000";
                                                           ctx.font = '14px sans-serif';
                                                           ctx.textBaseline = 'top';
                                                           ctx.fillText(name, Left*xs + 5, Top*xs + 5);
                                                           }

const drawActiveBox = function(){
                            let width = activeBox.right-activeBox.left;
                            let height = activeBox.bottom-activeBox.top;
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
                            ctx.fillRect(activeBox.left, activeBox.top, -10, -10); ctx.strokeRect(activeBox.left, activeBox.top, -10, -10);
                            ctx.fillRect(activeBox.right, activeBox.top, 10, -10); ctx.strokeRect(activeBox.right, activeBox.top, 10, -10);
                            ctx.fillRect(activeBox.right, activeBox.bottom, 10, 10); ctx.strokeRect(activeBox.right, activeBox.bottom, 10, 10);
                            ctx.fillRect(activeBox.left, activeBox.bottom, -10, 10); ctx.strokeRect(activeBox.left, activeBox.bottom, -10, 10);
                            
                            // Write box name
                            ctx.fillStyle = "#000";
                            ctx.font = '14px sans-serif';
                            ctx.textBaseline = 'top';
                            ctx.fillText(name, activeBox.left + 5, activeBox.top + 5);
                            }

function hitTestBoxes(page, mx, my) {
                                // first, test the selected box's control points
                                if (activeBox.key !== null){
                                
                                }
                                
                                
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
                                return 'select'; // hit the box body
                                }
                                }
                                }
                                
                                // missed everything, de-select any currently selected box
                                activeBox.key = null;
                                activeBox.top = activeBox.left = activeBox.top = activeBox.bottom = 0;
                                return 'none';
                                }

//////////////////////////////////////////////////////////////////////////////////////////////////// PAINT ALL
renderBoxes = function() {
                     ctx.clearRect(0, 0, boxCanvas.width, boxCanvas.height);
                     ctx.setTransform(1, 0, 0, 1, 0, 0);ctx.translate(0.5,0.5); // makes box edges a bit sharpers
                     
                     if (!projectFile.Pages) {
                     console.log("BAD DEFINITION");
                     console.dir(projectFile);
                     return;
                     }
                     let pageDef = projectFile.Pages[pageNum - 1];
                     if (!pageDef) {console.log("Bad def on page "+pageNum);return;}
                     
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
boxCanvas.addEventListener('mouseout', function(){ mouse.buttons = 0; }); // prevent drag-lock
boxCanvas.addEventListener('mousedown', function(e) {
    let mx = 0|e.offsetX;
    let my = 0|e.offsetY;

    // TODO: handle initial click (right or left?) -- might want to pop up the details modal
    // Cases:
    // * Hit one of the active box's control points (~10px of the corner) -> re-size
    // * Hit the body of the active box -> move
    // * Hit the body of a non-active box -> select
    // * Hit no box -> either start new box, or do nothing (depending on mode)
    
    let page = projectFile.Pages[pageNum-1];
    mouse.mode = hitTestBoxes(page, mx, my);
    
    let infoSpan = document.getElementById("active-box-name")
    let editButton = document.getElementById("box-edit");
    if (infoSpan) {infoSpan.innerText = " "+activeBox.key;}
    if (editButton){editButton.disabled = (activeBox.key === null);}
    
    
    if (e.buttons === 2 && mouse.mode === 'none') {
        // initial right-click. If we drag from here, a new box should be created
        
    }
}, false);

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE UP
boxCanvas.addEventListener('mouseup', function(e) {
    //activeBox.right = 0|e.offsetX;
    //activeBox.bottom = 0|e.offsetY;

    renderBoxes();

    // TODO: handle release. We might want to send changes back to the server
    // NOTE: send an incremented version ID in the project index file, to prevent old versions overwriting later changes.
}, false);

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE MOVE
boxCanvas.addEventListener('mousemove', function(e) {
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
    if (mouse.mode === 'select'){
        return; // Nothing
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
        // TODO: behaviour for other handles (this is just correct for bottom-right)
        activeBox.right = mouse.x;
        activeBox.bottom = mouse.y;
    }

    if (mouse.buttons !== 0) {
        renderBoxes();
    }
    
}, false);


// PDF LOAD TRIGGER
// Actually load the PDF (async then call our page render)
// This requires a modern web browser that supports promises.
pdfJsLib.getDocument(basePdfSourceUrl).promise.then(function(pdfDoc_) {
    pdfDoc = pdfDoc_;
    document.getElementById('page_count').textContent = pdfDoc.numPages;

    // Initial/first page rendering
    renderPage(pageNum);
});

// PROJECT LOAD AND STORE SCRIPTS
let oReq = new XMLHttpRequest();

oReq.addEventListener("progress", updateProgress);
oReq.addEventListener("error", transferFailed);
oReq.addEventListener("abort", transferCanceled);

oReq.onload = function(e) {
    console.dir(e);
    // store the project to variable, and trigger the box render code
    projectFile = oReq.response;
    renderBoxes();
}

oReq.open('GET', projectJsonLoadUrl);
oReq.responseType = "json";
oReq.send();

// progress on transfers from the server to the client (downloads)
function updateProgress (oEvent) {
    if (oEvent.lengthComputable) {
        let percentComplete = oEvent.loaded / oEvent.total * 100;
        console.log(`Loading ${percentComplete}%`);
        // ...
    } else {
        // Unable to compute progress information since the total size is unknown
        console.log("Loading");
    }
}

function transferFailed(evt) {
    console.log("An error occurred while transferring the file.");
    console.dir(evt);
}

function transferCanceled(evt) {
    console.log("The transfer has been canceled by the user.");
    console.dir(evt);
}
    