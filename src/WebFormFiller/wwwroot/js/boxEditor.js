/*

NOTE:

When embedding on a page, you must define these variables BEFORE importing this script:
 * basePdfSourceUrl   -- URL from where the PDF can be loaded
 * projectJsonLoadUrl -- URL from where we can load the JSON definition of the template project

 */
//let projectJsonLoadUrl = projectJsonLoadUrl || undefined;  //can I do this to keep Rider quiet?

// Read PDF.js exports from the ~/js/pdf.js file
const pdfJsLib = window['pdfjs-dist/build/pdf'];


// PDF rendering bits
let pdfDoc = null,
    pageNum = 1,
    pageRendering = false,
    pageNumPending = null; const scale = 2.8, // zoom level of the PDF
    // TODO: ability to change the scale level and update the rendering
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
        outerContainer.style.width = viewport.width+'px';
        outerContainer.style.height = viewport.height+'px';

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


// Click event for loading previous page
function onPrevPage() {
    if (pageNum <= 1) { return; }
    pageNum--;
    queueRenderPage(pageNum);
}
document.getElementById('prev').addEventListener('click', onPrevPage); // bind event to button

// Click event for loading next page
function onNextPage() {
    if (pageNum >= pdfDoc.numPages) { return; }
    pageNum++;
    queueRenderPage(pageNum);
}
document.getElementById('next').addEventListener('click', onNextPage); // bind event to button


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

(function() { // Activate the overlay canvas with mouse event handlers
    const ctx = boxCtx;

    let activeBox = {top:0, left:0, right:0, bottom:0};
    let mouse = {x: 0, y: 0, buttons:0};
    let last_mouse = {x: 0, y: 0};

    const drawSingleBox = function(def, name, xs, ys, active){
        if (!def){
            console.log("Invalid box def: "+JSON.stringify(def));
            return;
        }
        if (active) {
            ctx.fillStyle = "rgba(80, 100, 200, 0.3)";
            ctx.strokeStyle = "#07F";
            ctx.lineWidth = 2;
        } else {
            ctx.fillStyle = "rgba(255, 80, 80, 0.3)";
            ctx.strokeStyle = "#F00";
            ctx.lineWidth = 3;
        }

        const {Width,Top,Height,Left} = def;
        ctx.fillRect(Left*xs, Top*ys, Width*xs, Height*ys);
        ctx.strokeRect(Left*xs, Top*ys, Width*xs, Height*ys);

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
            drawSingleBox(def, name, xScale, yScale, true); // TODO: true only if this box has a valid data path
        }

        // Draw the 'active' box if one is being created
        drawActiveBox();
    };


    //////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE DOWN
    boxCanvas.addEventListener('mouseout', function(){ mouse.buttons = 0; }); // prevent drag-lock
    boxCanvas.addEventListener('mousedown', function(e) {
        activeBox.top = 0|e.offsetY;
        activeBox.left = 0|e.offsetX;

        // TODO: handle initial click (right or left?) -- might want to pop up the details modal
    }, false);

    //////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE UP
    boxCanvas.addEventListener('mouseup', function(e) {
        activeBox.right = 0|e.offsetX;
        activeBox.bottom = 0|e.offsetY;

        renderBoxes();

        // TODO: handle release. We might want to send changes back to the server
    }, false);

    //////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE MOVE
    boxCanvas.addEventListener('mousemove', function(e) {
        let newX = e.offsetX;
        let newY = e.offsetY;
        if (mouse.buttons !== 0) { // mouse is dragging
            last_mouse.x = mouse.x;
            last_mouse.y = mouse.y;
        } else { // mouse is hovering
            last_mouse.x = newX;
            last_mouse.y = newY;
        }

        mouse.buttons = e.buttons;
        mouse.x = newX;
        mouse.y = newY;

        activeBox.right = 0|e.offsetX;
        activeBox.bottom = 0|e.offsetY;

        if (mouse.buttons !== 0) {
            renderBoxes();
        }
    }, false);

}());

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
    