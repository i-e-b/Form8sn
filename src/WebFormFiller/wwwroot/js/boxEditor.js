// noinspection JSUnresolvedVariable

"use strict";
/*

NOTE:

When embedding on a page, you must define these variables BEFORE importing this script:
 * basePdfName             -- Name of base PDF. This is loaded from 'fileLoadUrl'
 * fileLoadUrl             -- URL *prefix* from where we can load base PDFs and other 'local' data
 * projectJsonLoadUrl      -- URL from where we can load the JSON definition of the template project
 * projectJsonStoreUrl     -- URL to which we can post an updated JSON definition of the template project
 * addFilterUrl            -- URL to add a new filter
 * deleteFilterUrl         -- URL to delete an existing filter
 
 * boxEditPartialUrl       -- URL for template box partial view
 * dataPickerPartialUrl    -- URL for data path picker partial view
 * displayFormatPartialUrl -- URL for display format partial view
 * docInfoPartialUrl       -- URL for document wide settings partial view
 * pageInfoPartialUrl      -- URL for page wide settings partial view
 * filterEditPartialUrl    -- URL for data filter edit partial view
 
 * pdfWorkerSource         -- URL of the file at ~/js/pdf.worker.js
 * boxMoveUrl              -- URL used to send back box movements & resizes
 * boxSampleUrl            -- URL used to get example data for rendering during edit

    see BoxEditor.cshtml
 */

// Event bindings
document.getElementById('prev').addEventListener('click', onPrevPage);
document.getElementById('next').addEventListener('click', onNextPage);
document.getElementById('zoom-plus').addEventListener('click', onPageZoomPlus);
document.getElementById('zoom-minus').addEventListener('click', onPageZoomMinus);

document.getElementById('document-edit').addEventListener('click', showDocumentInfoModal);
document.getElementById('page-edit').addEventListener('click', showPageInfoModal);
document.getElementById('box-edit').addEventListener('click', showBoxEditModal);

// Read PDF.js exports from the ~/js/pdf.js file
const pdfJsLib = window['pdfjs-dist/build/pdf'];
pdfJsLib.GlobalWorkerOptions.workerSrc = pdfWorkerSource; // Setup the workerSrc property, as required by the library.


// PDF RENDERING ======================================================================================================
let pdfDoc         = null,                                  // the PDF loaded into memory
    pageNum        = 1,                                     // 1-based PDF page number
    pageRendering  = false,                                 // are we waiting for a page render worker to finish?
    pageNumPending = null,                                  // page we are waiting to render
    scale          = 2.0,                                   // current zoom level of the PDF
    pdfCanvas      = document.getElementById('pdf-render'), // HTML canvas for PDF (goes under the box drawing canvas)
    pdfCtx         = pdfCanvas.getContext('2d');            // draw context for PDF
const zoomRatio    = 1.2;                                   // how much we scale during zoom in/out

// Box rendering bits
const boxCanvas      = document.getElementById("box-render"); // HTML canvas for box drawing and mouse events (goes over the PDF canvas)
const boxCtx         = boxCanvas.getContext("2d");            // draw context for boxes
const previewImages  = {};                                                // name => image of loaded previews

/**
 * Get page info from document, resize canvas accordingly, and render page.
 * num is Page number.
 */
function renderPage(num) {
    pageRendering = true;
    // Using promise to fetch the page
    pdfDoc.getPage(num).then(function (page) {
        const viewport = page.getViewport({scale: scale});
        pdfCanvas.height = viewport.height;
        pdfCanvas.width = viewport.width;

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
                boxCanvas.height = viewport.height;
                boxCanvas.width = viewport.width;
                
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

function onPrevPage() {
    if (pageNum <= 1) {
        return;
    }
    clearActiveBox();updateEditButton();
    pageNum--;
    queueRenderPage(pageNum);
}
function onNextPage() {
    if (pageNum >= pdfDoc.numPages) {
        return;
    }
    clearActiveBox();updateEditButton();
    pageNum++;
    queueRenderPage(pageNum);
}


// HTML MODAL CONTROL FOR BOX DETAIL EDITING ==========================================================================
function serverCallback(url, verb, success, failure) {
    let xhr = new XMLHttpRequest();
    xhr.onerror = function (evt) {failure(evt);}
    xhr.onabort = function (evt) {failure(evt);}
    xhr.onload = function (evt) {
        if (xhr.status >= 200 && xhr.status < 400) {success(evt, xhr);
        } else {failure(evt);}
    };
        

    xhr.open(verb, url);
    xhr.send();
}
function loadPartialToModal(url, targetId, nextAction) {
    let targetElement = document.getElementById(targetId);
    if (!targetElement) {console.error(`Could not load from ${url}, as target element #${targetId} was not found`);return;}
    
    serverCallback(url, 'GET', function (evt, xhr) {
        targetElement.innerHTML = xhr.responseText;
        if (nextAction) nextAction(evt);
    }, function (evt) {
        targetElement.innerText = "An error occurred while transferring this UI element";
        console.dir(evt);
    })
}
function setValue(elementId, newValue){
    let elem = document.getElementById(elementId);
    if (elem) {elem.value = newValue;}
    else {console.error(`Failed to copy value '${newValue}' in element #${elementId}`)}
}

//////////////////////////////////////////////////////////////////////////////////////////////////// Edit box
function deleteSelectedBox(){
    if (!activeBox.key) return;

    let pageDef = projectFile.Pages[pageNum - 1];
    delete pageDef.Boxes[activeBox.key];
    activeBox.key = null;
    updateEditButton();
    
    storeAndReloadProjectFile(function(){
        closeBoxEditModal();
        renderBoxes();
    });
}
function showBoxEditModal() {
    if (!activeBox.key) {
        return;
    }
    if (!boxEditPartialUrl) {
        console.error('boxEditPartialUrl was not bound');
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

    let newBoxName = document.getElementById('BoxName').value;

    submit(formEle).then(function() {
        // close the modal box and refresh everything
        reloadProjectFile(function() {
            closeBoxEditModal();
            selectActiveBox(newBoxName);
            renderBoxes();
        });
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
function reloadSampleData(pageIdx, boxKey, boxObject){
    // make an async request to load sample data for a box
    // this loads directly into the supplied object, then
    // triggers a re-draw.

    if (!boxObject) return;
    if (!boxKey) {console.log("lost box key!"); return;}
    if (!boxSampleUrl || boxSampleUrl === "") return; // ignore if no callback
    
    boxObject.sampleData = "loading"; // prevent multiple calls
    
    let url = `${boxSampleUrl}&docVersion=${projectFile.Version}&pageIndex=${pageIdx}&boxKey=${boxKey}`;
    serverCallback(url, 'GET', function (evt, xhr) {
        boxObject.sampleData = xhr.responseText;
        if (boxObject.sampleData) {
            renderBoxes();
        }
    }, function (evt) {
        console.dir(evt);
    })
}
function tryLoadSampleImage(pageIdx, boxKey, boxObject){
    if (!boxObject) return;
    if (!boxObject.sampleData) return;
    
    
    let url = boxObject.sampleData;
    if (boxObject.MappingPath && boxObject.MappingPath[0] === "img"){
        if (!boxObject.MappingPath[1]) return; // invalid
            // build a custom url
        url = `${fileLoadUrl}${boxObject.MappingPath[1]}.jpg`;
    }
    
    if (previewImages[url]){ // includes images still being loaded
        boxObject.sampleImage = previewImages[url];
        return;
    }
    
    let img = new Image();
    
    boxObject.sampleImage = img;
    previewImages[url] = img;

    img.onload = function () {
        renderBoxes();
    };
    img.src = url;
}

//////////////////////////////////////////////////////////////////////////////////////////////////// Doc info
function showDocumentInfoModal(){
    if (!docInfoPartialUrl) {
        console.error('docInfoPartialUrl was not bound');
        return;
    }
    let modal = document.getElementById('EditDocument_DocumentInfo');
    if (!modal) return;
    
    modal.classList.add("active"); // Document info screen might need to read all system fonts
    loadPartialToModal(docInfoPartialUrl,
        'EditDocument_DocumentInfo_Content', function () {
        });
}
function saveDocumentInfoChanges(){
    const formEle = document.getElementById('editDocumentSettingsForm');
    if (!formEle) return;

    submit(formEle).then(function() {
        // close the modal box and refresh everything
        reloadProjectFile(function() {
            closeDocumentInfoModal();
            renderBoxes();
        });
        queueRenderPage(pageNum);
    });
}
function closeDocumentInfoModal(){
    let modal = document.getElementById('EditDocument_DocumentInfo');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditDocument_DocumentInfo_Content');
    if (deadContent) deadContent.innerHTML = "Loading...";
}

//////////////////////////////////////////////////////////////////////////////////////////////////// Data Filters
function addNewDataFilter(pageIdx){
    if (!addFilterUrl) {console.error("addFilterUrl is not bound"); return;}
    
    let url = addFilterUrl;
    if (typeof pageIdx != 'undefined'){
        url += `&pageIdx=${pageIdx}`;
    }
    
    serverCallback(url, 'GET', function(){
        // reload the modal
        if (typeof pageIdx != 'undefined') {
            showPageInfoModal();
        } else {
            showDocumentInfoModal();
        }
    }, function(evt){
        console.error("Server call to add filter failed");
        console.dir(evt);
    });
}
function editDataFilter(filterKey, pageIdx){
    if (!filterEditPartialUrl) {console.error("filterEditPartialUrl is not bound");return;}

    let modal = document.getElementById('EditTemplateBox_DataFilter');
    if (!modal) return;
    
    let url = `${filterEditPartialUrl}&filterKey=${encodeURIComponent(filterKey)}`;
    if (typeof pageIdx != 'undefined'){
        url += `&pageIndex=${pageIdx}`;
    }

    loadPartialToModal(url,
        'EditTemplateBox_DataFilter_Content', function () {
            let filterSrc = document.getElementById('DataFilterType');
            if (filterSrc) filterSrc.addEventListener('change', updateDataFilterVisibility);
            updateDataFilterVisibility();
            modal.classList.add("active"); // Document info screen might need to read all system fonts
        });
}
function deleteDataFilter(filterKey, pageIdx){
    if (!deleteFilterUrl) {console.error("deleteFilterUrl is not bound"); return;}

    let url = `${deleteFilterUrl}&name=${encodeURIComponent(filterKey)}`;
    if (typeof pageIdx != 'undefined'){
        url += `&pageIdx=${pageIdx}`;
    }
    
    serverCallback(url, 'GET', function(){
        // reload the modal
        if (typeof pageIdx != 'undefined') {
            showPageInfoModal();
        } else {
            showDocumentInfoModal();
        }
    }, function(evt){
        console.error("Server call to delete filter failed");
        console.dir(evt);
    });
}

function updateDataFilterVisibility(){
    // Scan all 'format-filter-detail' elements. Hide any that don't apply to the currently selected format type.
    let all = document.getElementsByClassName('data-filter-detail');
    let filterSrc = document.getElementById('DataFilterType');

    if (!filterSrc) { console.error("filter set id missing"); return; }
    let pattern = "display-"+filterSrc.value;

    for(let i = 0; i < all.length; i++) {
        let elem = all[i];

        elem.style.display = (elem.classList.contains(pattern)) ? "block" : "none";
    }
}
function closeDataFilterModal(){
    let modal = document.getElementById('EditTemplateBox_DataFilter');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditTemplateBox_DataFilter_Content');
    if (deadContent) deadContent.innerHTML = "";
}
function saveDataFilterChanges(){
    const formEle = document.getElementById('editDataFilterForm');
    if (!formEle) return;

    let pageIdx = formEle.elements["PageIndex"].value || -1;

    submit(formEle).then(function() {
        reloadProjectFile(function() {
            // reload the parent modal
            if (pageIdx >= 0) { showPageInfoModal();
            } else { showDocumentInfoModal(); }
            closeDataFilterModal();
        });
    });
}

//////////////////////////////////////////////////////////////////////////////////////////////////// Page info
function showPageInfoModal(){
    if (!pageInfoPartialUrl) {
        console.error('docInfoPartialUrl was not bound');
        return;
    }
    let modal = document.getElementById('EditDocument_PageInfo');
    if (!modal) return;
    let pageIdx = pageNum - 1;

    loadPartialToModal(`${pageInfoPartialUrl}&pageIndex=${pageIdx}`,
        'EditDocument_PageInfo_Content', function () {
            modal.classList.add("active");
        });
}
function savePageInfoChanges() {
    const formEle = document.getElementById('editPageSettingsForm');
    if (!formEle) return;

    submit(formEle).then(function() {
        // close the modal box and refresh everything
        reloadProjectFile(function() {
            closePageInfoModal();
            renderBoxes();
        });
        queueRenderPage(pageNum);
    });
}
function closePageInfoModal(){
    let modal = document.getElementById('EditDocument_PageInfo');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditDocument_PageInfo_Content');
    if (deadContent) deadContent.innerHTML = "Loading...";
}

//////////////////////////////////////////////////////////////////////////////////////////////////// Display format
function updateDisplayFormatVisibility(){
    // Scan all 'format-filter-detail' elements. Hide any that don't apply to the currently selected format type.
    let all = document.getElementsByClassName('format-filter-detail');
    let filterSrc = document.getElementById('FormatFilterType');

    if (!filterSrc) { console.error("filter set id missing"); return; }
    let pattern = "display-"+filterSrc.value;

    for(let i = 0; i < all.length; i++) {
        let elem = all[i];
        
        elem.style.display = (elem.classList.contains(pattern)) ? "block" : "none";
    }
}
function showDisplayFormatModal() {
    if (!activeBox.key) {return;}
    if (!displayFormatPartialUrl) {console.error('displayFormatPartialUrl was not bound');return;}

    // Synthesise a url, then show the modal.
    let pageIdx = pageNum - 1;

    let modal = document.getElementById('EditTemplateBox_DisplayFormat');
    if (!modal) return;
    loadPartialToModal(`${displayFormatPartialUrl}&pageIndex=${pageIdx}&boxKey=${activeBox.key}`,
        'EditTemplateBox_DisplayFormat_Content', function () {

            let filterSrc = document.getElementById('FormatFilterType');
            if (filterSrc) filterSrc.addEventListener('change', updateDisplayFormatVisibility);
            updateDisplayFormatVisibility();
            modal.classList.add("active");
        });
}
function saveDisplayFormatChanges(){
    let all = document.getElementsByClassName('format-param-value');
    let chosen = document.getElementById('FormatFilterType'); // in DisplayFormatEditor.cshtml
    if (!chosen) {console.error('lost selected filter reference');return;}

    let pattern = "display-"+chosen.value;

    let page = projectFile.Pages[pageNum - 1];
    if (!page) {console.error('lost page reference');return;}
    let box = page.Boxes[activeBox.key];
    if (!box) {console.error('lost box reference');return;}
    
    let newSettings = {Type:chosen.value, FormatParameters:{}}
    let description = chosen.value;
    
    let j = 0;
    for (let i = 0; i < all.length; i++) {
        let elem = all[i];
        
        if (!elem.parentElement.classList.contains(pattern)) continue; // value is not applicable to this filter
        
        newSettings.FormatParameters[elem.id] = elem.value;
        
        if (j>0) description+=", ";
        else description+=": ";
        
        description += `${elem.id} = ${elem.value}`
        j++;
    }
    
    // Write a summary of the filter into the box-edit screen (just to hint that everything worked)
    setValue('DisplayFormatDescription', description); // in EditTemplateBox.cshtml
    
    // Copy details from the modal into our selected box
    // Don't save -- the out box details will do this, and keep the save/cancel semantics correct
    box.DisplayFormat = newSettings;
    setValue('DisplayFormatJsonStruct', JSON.stringify(newSettings)) // in EditTemplateBox.cshtml
    
    closeDisplayFormatModal();
}
function closeDisplayFormatModal(){
    let modal = document.getElementById('EditTemplateBox_DisplayFormat');
    if (!modal) return;

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditTemplateBox_DisplayFormat_Content');
    if (deadContent) deadContent.innerHTML = "";
}

/////////////////////////////////////////////////////////////////////////////////////////////////// Data path
function captureDataPickerResult(path, targetId){
    let target = document.getElementById(targetId); // in 'EditTemplateBox.cshtml'
    if (!target) {console.error(`Lost capture target: '${targetId}'`);return;}
    
    target.value = path.replace(/\x1F/g,".");
    
    closeDataPathPicker();
}
function showDataPickerModal(target, allowMultiple, onPage) {
    if (!dataPickerPartialUrl) {console.error('dataPickerPartialUrl was not bound');return;}
    
    let targetId = target || 'DataPath';
    let targetElem = document.getElementById(targetId);
    if (!targetElem) {console.error(`showDataPickerModal is bound to an element that was not present: '${targetId}'`);return;}
    
    let modal = document.getElementById('EditTemplateBox_DataMap');
    if (!modal) {console.error("Lost modal: 'EditTemplateBox_DataMap'");return;}

    let pageIdx = pageNum - 1;
    let pathReq = encodeURIComponent(targetElem.value);
    
    let url = `${dataPickerPartialUrl}&oldPath=${pathReq}&target=${targetId}`;
    if (allowMultiple) url += "&multiplesCanBePicked=true";
    if (onPage) url += `&pageIndex=${pageIdx}`;

    loadPartialToModal(url, 'EditTemplateBox_DataMap_Content', function () {
        modal.classList.add("active");
    });
}
function closeDataPathPicker() {
    let modal = document.getElementById('EditTemplateBox_DataMap');
    if (!modal) {console.error("Lost modal 'EditTemplateBox_DataMap'");return;}

    modal.classList.remove("active");

    let deadContent = document.getElementById('EditTemplateBox_DataMap_Content');
    if (deadContent) deadContent.innerHTML = "";
}

// BOX DRAW AND INTERACTION ===========================================================================================
let projectFile = {// Note, this dummy data is just here to help with IDE auto-fill and type checking
    Version: 0, SampleFileName: null, BasePdfFile: "/File/....pdf", Notes: "", Name: "Sample template", BaseFontSize: null, FontName: null,
    Pages: [{WidthMillimetres: 210.0, HeightMillimetres: 297.0, PageFontSize: null, Name: "Page 1", Notes: null, BackgroundImage: null, RepeatMode: {Repeats: false, DataPath: null},
            Boxes: {"Sample": {WrapText: true, ShrinkToFit: true, BoxFontSize: 16, Alignment: "BottomLeft", DependsOn: "otherBoxName", Top: 10, Left: 10, Width: 10, Height: 10, MappingPath: ["path", "is", "here"],
                    DisplayFormat: {Type: "DateFormat", FormatParameters: {}}, BoxOrder: 1
                }
            },
            PageDataFilters: {},
            RenderBackground: false
        }
    ],
    DataFilters: {}
};

// Mouse co-ords and active box are in screen-space units
let activeBox = {key: null, new:false, top: 0, left: 0, right: 0, bottom: 0};
let mouse = {x: 0, y: 0, buttons: 0, mode: 'none', xControl:'right', yControl:'bottom'};
let last_mouse = {x: 0, y: 0};

//////////////////////////////////////////////////////////////////////////////////////////////////// BOX EDITING
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
function tryUpdateActiveBox(sendChanges){
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
    
    if (!sendChanges) return;
    
    // Store back to server. We do this fire-and-forget, as we might overwrite the whole document template definition later.
    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (req.responseText !== 'OK') {
            console.error("An error occurred while updating document template box location.");
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
    
    // Set the active box so user can resize at the same time as creating
    activeBox.key = name;
    activeBox.left = x;
    activeBox.top = y;
    activeBox.right = x + 2;
    activeBox.bottom = y + 2;
    mouse.xControl = 'right';
    mouse.yControl = 'bottom';
    
    // write the new box into the page definition
    page.Boxes[name] =  {
        WrapText: true,
        ShrinkToFit: true,
        BoxFontSize: null,
        Alignment: "TopLeft",
        DependsOn: null,
        Top: y*yi,
        Left: x*xi,
        Width: 10 * xi, // oversize the initial box so that we don't drop anything
        Height: 10 * yi, // too small to edit if there is an error.
        MappingPath: null,
        DisplayFormat: null,
        BoxOrder: null
    }
    
}

function selectActiveBox(name){
    let page = projectFile.Pages[pageNum - 1];
    
    let xScale = boxCanvas.width / page.WidthMillimetres;
    let yScale = boxCanvas.height / page.HeightMillimetres;
    
    let def = page.Boxes[name];
    
    if (!def) {
        activeBox.key = null;
        return;
    }
    
    let {Width, Top, Height, Left} = def;
    Width *= xScale;
    Top *= yScale;
    Left *= xScale;
    Height *= yScale;
    activeBox.key = name;
    activeBox.top = Top;
    activeBox.left = Left;
    activeBox.right = Left + Width;
    activeBox.bottom = Top + Height;
    mouse.mode = 'select';
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
    
    // As a last-ditch, try to select a box near to the click
    const offset = 5;
    for (let name in page.Boxes) {
        let def = page.Boxes[name];
        let {Width, Top, Height, Left} = def;
        Width *= xScale;
        Top *= yScale;
        Left *= xScale;
        Height *= yScale;

        if (mx >= Left-offset && my >= Top-offset) {
            if (mx - Left <= Width+offset && my - Top <= Height+offset) {
                // Did we hit the already selected box?
                if (activeBox.key === name) { // allow re-sizing of degenerate boxes
                    mouse.xControl = 'right';
                    mouse.yControl = 'bottom';
                    return 'size';
                }

                // otherwise, select this new one
                activeBox.key = name;
                activeBox.top = Top;
                activeBox.left = Left;
                activeBox.right = Left + Width;
                activeBox.bottom = Top + Height;
                return 'select';
            }
        }
    }

    // Missed everything, de-select any currently selected box
    clearActiveBox();
    return 'none';
}

//////////////////////////////////////////////////////////////////////////////////////////////////// BOX PAINTING
function drawBoxPreview(def, name, Width, Top, Height, Left, DisplayFormat){
    try {
        if (!def.MappingPath) return; // probably a newly created box. Nothing to preview.
        
        // Try to load the data if not already done.
        // This should re-trigger box drawing if it
        // is successful. Otherwise there is no box
        // preview.
        if (!def.sampleData) {
            reloadSampleData(pageNum - 1, name, def);
            return;
        }

        // Check for Image Stamp, and load from the 'fileLoadUrl'
        if (def.MappingPath && def.MappingPath[0] === "img") {
            if (!def.sampleImage) tryLoadSampleImage(pageNum - 1, name, def);
            // fall-through to text rendering while image loads
        }

        // Check to see if the box has a "RenderImage" formatter.
        // If so,  first try to load the URL through the browser.
        if (DisplayFormat && (DisplayFormat.Type === "RenderImage")) {
            tryLoadSampleImage(pageNum - 1, name, def);
            // fall-through to text rendering while image loads
        }

        // If anything above has loaded an image to display,
        // then show it with an alpha blend, behind the box.
        let oldAlpha = boxCtx.globalAlpha;
        try {
            if (def.sampleImage) {
                boxCtx.globalAlpha = 0.25;
                boxCtx.drawImage(def.sampleImage, Left, Top, Width, Height);
                boxCtx.globalAlpha = oldAlpha;
                return;
            }
        } catch (err) {
            boxCtx.fillStyle = "#000";
            boxCtx.font = '14px sans-serif';
            boxCtx.textBaseline = 'top';
            boxCtx.fillText("Fail: " + err, Left + 5, Top + 20, Width - 10);
        }
        boxCtx.globalAlpha = oldAlpha;

        // Either no special render, or it's not loaded yet;
        // but we do have some text data, so we render that.
        boxCtx.fillStyle = "rgba(0,0,0,0.56)";
        boxCtx.font = '14px sans-serif';
        boxCtx.textBaseline = 'top';
        boxCtx.fillText(def.sampleData, Left + 5, Top + 20, Width - 10);
    } catch (prevErr){
        console.log("error in box preview");
        console.dir(prevErr);
    }
}
function drawSingleBox(def, name, xs, ys, validMapping) {
    if (!def) {
        console.error("Invalid box def: " + JSON.stringify(def));
        return;
    }
    if (name === activeBox.key) return; // this box is selected and will be drawn in drawActiveBox(). Don't render it here.
    
    // Extract information
    const {Width, Top, Height, Left, DisplayFormat} = def;

    drawBoxPreview(def,name,Width * xs, Top * xs, Height * xs, Left * xs, DisplayFormat)

    if (validMapping) {
        boxCtx.fillStyle = "rgba(80, 100, 200, 0.3)";
        boxCtx.strokeStyle = "#07F";
        boxCtx.lineWidth = 2;
    } else {
        boxCtx.fillStyle = "rgba(255, 80, 80, 0.3)";
        boxCtx.strokeStyle = "#F00";
        boxCtx.lineWidth = 3;
    }

    // Draw box in place over PDF
    boxCtx.fillRect(Left * xs, Top * ys, Width * xs, Height * ys);
    boxCtx.strokeRect(Left * xs, Top * ys, Width * xs, Height * ys);

    // Write box name
    boxCtx.fillStyle = "#000";
    boxCtx.font = '14px sans-serif';
    boxCtx.textBaseline = 'top';
    boxCtx.fillText(name, 0|(Left * xs) + 5, 0|(Top * xs) + 5);
}

function drawActiveBox() {
    let width = activeBox.right - activeBox.left;
    let height = activeBox.bottom - activeBox.top;
    if (width < 1 || height < 1) return; // box is zero sized
    if (activeBox.key === null) return; // no box is active

    try {
        let pageDef = projectFile.Pages[pageNum - 1];
        let boxDef = pageDef.Boxes[activeBox.key];
        drawBoxPreview(boxDef, activeBox.key, width, activeBox.top, height, activeBox.left, boxDef.DisplayFormat);
    } catch (err){
        console.log(err);
    }
    
    boxCtx.fillStyle = "rgba(255, 190, 80, 0.3)";
    boxCtx.strokeStyle = "#FA0";
    boxCtx.lineWidth = 3;

    // The box
    boxCtx.fillRect(activeBox.left, activeBox.top, width, height);
    boxCtx.strokeRect(activeBox.left, activeBox.top, width, height);

    // resize handles
    boxCtx.fillStyle = "#FA0";
    boxCtx.strokeStyle = "#A50";
    boxCtx.lineWidth = 1;
    boxCtx.fillRect(activeBox.left, activeBox.top, -10, -10);
    boxCtx.strokeRect(activeBox.left, activeBox.top, -10, -10);
    boxCtx.fillRect(activeBox.right, activeBox.top, 10, -10);
    boxCtx.strokeRect(activeBox.right, activeBox.top, 10, -10);
    boxCtx.fillRect(activeBox.right, activeBox.bottom, 10, 10);
    boxCtx.strokeRect(activeBox.right, activeBox.bottom, 10, 10);
    boxCtx.fillRect(activeBox.left, activeBox.bottom, -10, 10);
    boxCtx.strokeRect(activeBox.left, activeBox.bottom, -10, 10);

    // Write box name
    boxCtx.fillStyle = "#000";
    boxCtx.font = '14px sans-serif';
    boxCtx.textBaseline = 'top';
    boxCtx.fillText(activeBox.key, 0|activeBox.left + 5, 0|activeBox.top + 5);
}
function middleOfBox(box, xs, ys){
    const {Width, Top, Height, Left} = box;
    let x = (Left + (Width / 2)) * xs;
    let y = (Top + (Height / 2)) * ys;
    return {x:x, y:y}
}
function canvas_arrow(from, to, headSize) {
    const dx = to.x - from.x;
    const dy = to.y - from.y;
    const angle = Math.atan2( dy, dx );
    boxCtx.beginPath();
    boxCtx.moveTo( from.x, from.y );
    boxCtx.lineTo( to.x, to.y );
    boxCtx.stroke();
    boxCtx.beginPath();
    boxCtx.moveTo( to.x - headSize * Math.cos( angle - Math.PI / 6 ), to.y - headSize * Math.sin( angle - Math.PI / 6 ) );
    boxCtx.lineTo( to.x, to.y );
    boxCtx.lineTo( to.x - headSize * Math.cos( angle + Math.PI / 6 ), to.y - headSize * Math.sin( angle + Math.PI / 6 ) );
    boxCtx.stroke();
}
function drawBoxDependency(active, from, to, xs, ys, r,g,b){
    let midFrom = middleOfBox(from, xs, ys);
    let midTo = middleOfBox(to, xs, ys);
    
    let headSize = 8;
    
    if (active) {
        boxCtx.strokeStyle = `rgba(${r}, ${g}, ${b}, 0.8)`;
        boxCtx.lineWidth = 2;
        headSize = 10;
    } else {
        boxCtx.strokeStyle = `rgba(${r}, ${g}, ${b}, 0.3)`;
        boxCtx.lineWidth = 1;
    }
    canvas_arrow(midFrom, midTo, headSize);
}
function drawOrderArrows(orderArrows, page, xs, ys){
    if (!orderArrows.length || orderArrows.length < 2) return;
    orderArrows.sort((a, b) => { // {order:(0|def.BoxOrder), key:name}
        return a.order - b.order;
    });

    let prev = orderArrows[0]
    for (let i = 1; i < orderArrows.length; i++) {
        let current = orderArrows[i];
        
        let active = prev.key === activeBox.key || current.key === activeBox.key;
        let from = page.Boxes[prev.key];
        let to = page.Boxes[current.key];
        drawBoxDependency(active, from, to, xs, ys,  0,0,0)
        
        prev = current;
    }
}
function renderBoxes () {
    boxCtx.clearRect(0, 0, boxCanvas.width, boxCanvas.height);
    boxCtx.setTransform(1, 0, 0, 1, 0, 0);
    boxCtx.translate(0.5, 0.5); // makes box edges a bit sharpers

    if (!projectFile.Pages) {
        console.error("Invalid index file - no page definitions found");
        console.dir(projectFile);
        return;
    }
    let pageDef = projectFile.Pages[pageNum - 1];
    if (!pageDef) {
        console.error("Bad page definition for index " + (pageNum-1));
        return;
    }

    let xScale = boxCanvas.width / pageDef.WidthMillimetres;
    let yScale = boxCanvas.height / pageDef.HeightMillimetres;
    let orderArrows = [];

    for (let name in pageDef.Boxes) {
        let def = pageDef.Boxes[name];
        let active = def.MappingPath && (def.MappingPath.length > 1);
        drawSingleBox(def, name, xScale, yScale, active);
        
        // If this box 'depends on' another, draw an arrow line between the two
        if (def.DependsOn && pageDef.Boxes[def.DependsOn]) {
            drawBoxDependency(name === activeBox.key, def, pageDef.Boxes[def.DependsOn], xScale, yScale, 0,80,100);
        }
        
        // If this box is part of an order set, add it to the list we'll draw later
        if (def.BoxOrder) {
            orderArrows.push({order:(0|def.BoxOrder), key:name});
        }
    }

    drawOrderArrows(orderArrows, pageDef, xScale, yScale);
    
    // Draw the 'active' box if one is being created
    drawActiveBox();
}

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE DOWN
boxCanvas.addEventListener('mouseout', function () {mouse.buttons = 0;}); // prevent drag-lock
boxCanvas.addEventListener('mousedown', function (e) {
    let mx = 0 | e.offsetX;
    let my = 0 | e.offsetY;
    e.preventDefault();

    // Cases:
    // * Hit one of the active box's control points (~10px of the corner) -> re-size
    // * Hit the body of the active box -> move
    // * Hit the body of a non-active box -> select
    // * Hit no box -> either start new box, or do nothing (depending on mode)
    
    let page = projectFile.Pages[pageNum - 1];
    mouse.mode = hitTestBoxes(page, mx, my);
    updateEditButton();
    
    if ((e.buttons === 2 || e.shiftKey) && mouse.mode === 'none') {
        tryCreateNewBox(mx,my);
        mouse.mode = 'create';
    }
    
    renderBoxes();
}, false);

//////////////////////////////////////////////////////////////////////////////////////////////////// MOUSE UP
boxCanvas.addEventListener('mouseup', function (e) {
    e.preventDefault();

    if (mouse.mode === 'create'){
        tryUpdateActiveBox(false);
        storeAndReloadProjectFile(function(){ // we've added a new box, so update the entire project
            updateEditButton();
        });
        mouse.mode = 'select';
    }
    if (mouse.mode === 'move' || mouse.mode === 'size') {
        tryUpdateActiveBox(true); // changed an existing box, so we can just send size changes back to server
        mouse.mode = 'select';
    }

    renderBoxes();
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
    } else if (mouse.mode === 'size' || mouse.mode === 'create') {
        // free resize the active box
        let oldX = activeBox[mouse.xControl];
        let oldY = activeBox[mouse.yControl];
        activeBox[mouse.xControl] = mouse.x;
        activeBox[mouse.yControl] = mouse.y;
        
        // make sure the box is not invalid (we're measuring in screen-space here, zooming in will still let you place tiny boxes)
        if (activeBox.right - 5 < activeBox.left) activeBox[mouse.xControl] = oldX;
        if (activeBox.bottom - 5 < activeBox.top) activeBox[mouse.yControl] = oldY;

        if (e.altKey){ // force square
            let h = activeBox.bottom - activeBox.top;
            let w = activeBox.right - activeBox.left;
            let m = (h+w)/2;
            activeBox.right = activeBox.left + m;
            activeBox.bottom = activeBox.top + m;
        }
        //console.log(e.ctrlKey+"|"+e.altKey);
    }

    if (mouse.buttons !== 0) {
        renderBoxes();
    }

}, false);

// PROJECT LOAD AND STORE =============================================================================================

function storeAndReloadProjectFile(next){
    // We always reload the project file, even after an error -- this is in case the error was due to an 
    // out-of-date project definition

    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (!(req.status >= 200 && req.status < 400)) {
            console.error("An error occurred while loading document template definition: " + req.responseText);
            console.dir(evt);
        }
        reloadProjectFile(next);
    }

    req.open('POST', projectJsonStoreUrl, true);
    req.setRequestHeader('Content-Type', 'application/json; charset=UTF-8');

    req.onerror = function (evt) {
        console.error("An error occurred while loading document template definition.");
        console.dir(evt);
        reloadProjectFile();
    };
    req.onabort = function (evt) {
        console.error("Loading document template definition has been canceled by a user action.");
        console.dir(evt);
        reloadProjectFile();
    };

    req.send(JSON.stringify(projectFile));
}

function reloadProjectFile(next) {
    let req = new XMLHttpRequest();

    req.onload = function (evt) {
        if (req.status >= 200 && req.status < 400) {
            // store the project to variable, and trigger the box render code
            projectFile = req.response;
            if (next) {next();}
        } else {
            console.error(`A ${req.status} error occurred while loading document template definition from ${projectJsonLoadUrl}`);
            console.dir(evt);
        }
    }

    req.open('GET', projectJsonLoadUrl, true);
    req.responseType = "json";

    req.onerror = function (evt) {
        console.error("An error occurred while loading document template definition from "+projectJsonLoadUrl);
        console.dir(evt);
    };
    req.onabort = function (evt) {
        console.error("Loading document template definition has been canceled by a user action.");
        console.dir(evt);
    };

    req.send();
}

// PDF LOAD TRIGGER ===================================================================================================

// Actually load the PDF (async then call our page render)
// This requires a modern web browser that supports promises.
pdfJsLib.getDocument(fileLoadUrl+''+basePdfName).promise.then(function (pdfDoc_) {
    pdfDoc = pdfDoc_;
    document.getElementById('page_count').textContent = pdfDoc.numPages;

    // Initial/first page rendering
    reloadProjectFile();
    renderPage(pageNum);
});
