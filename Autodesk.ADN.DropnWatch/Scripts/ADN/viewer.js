var _auth;
var _viewer;

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function getPropertyValue(viewer, dbId, name, callback) {

    function propsCallback(result) {

        if (result.properties) {

            for (var i = 0; i < result.properties.length; i++) {

                var prop = result.properties[i];

                if (prop.displayName == name) {

                    callback(prop.displayValue);
                }
            }

            callback('');
        }
    }

    viewer.getProperties(dbId, propsCallback);
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function clearCurrentModel() {

    var viewerElement = document.getElementById('viewer3d');
    viewerElement.parentNode.removeChild(viewerElement);
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function loadDocument(auth, viewer, documentId) {

    if (documentId.indexOf('urn:') !== 0)
        documentId = 'urn:' + documentId;

    Autodesk.Viewing.Document.load(
        documentId,
        auth,
        function (viewerDoc) {

            //viewer.setBackgroundColor(0.8, 0.8, 0.8, 0.8, 0.8, 0.8);

            var items = Autodesk.Viewing.Document.getSubItemsWithProperties(
                viewerDoc.getRootItem(),
                //{ 'mime': 'application/autodesk-svf' },
                { 'type': 'geometry', 'role': '3d' },
                true);

            if (items.length > 0) {

                var item3d = viewerDoc.getViewablePath(items[0]);

                viewer.load(item3d);

                viewer.addEventListener(
                   Autodesk.Viewing.GEOMETRY_LOADED_EVENT,
                   onGeometryLoaded);
            }
        },
        function (msg) {

            AlertBox.displayError(
                document.getElementById('viewer3d'),
                "Load Error: " + msg);
        }
    );
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function createViewer() {

    var viewerContainer = document.getElementById('viewerContainer');

    var viewerElement = document.createElement("div");

    viewerElement.id = 'viewer3d';
    viewerElement.style.height = "100%";

    viewerContainer.appendChild(viewerElement);

    var viewer = new Autodesk.Viewing.Private.GuiViewer3D(viewerElement, {});

    viewer.initialize();

    viewer.addEventListener('selection', onViewerItemSelected);

    // disable scrolling on DOM document 
    // while mouse pointer is over viewer area
    $('#viewer3d').hover(
        function () {
            var scrollX = window.scrollX;
            var scrollY = window.scrollY;
            window.onscroll = function () {
                window.scrollTo(scrollX, scrollY);
            };
        },
        function () {
            window.onscroll = null;
        }
    );

    // disable default context menu on viewer div 
    $('#viewer3d').on('contextmenu', function (e) {
        e.preventDefault();
    });

    return viewer;
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function onGeometryLoaded(event) {

    _viewer.removeEventListener(
        Autodesk.Viewing.GEOMETRY_LOADED_EVENT,
        onGeometryLoaded);

    _viewer.getObjectTree(function (rootComponent) {

        _viewer.docstructure.handleAction(
            ["focus"],
            rootComponent.dbId);
    });
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function addToCombo(modelName, modelURN) {

    var combo = document.getElementById("combo");
    var option = document.createElement("option");

    option.text = modelName;
    option.modelURN = modelURN;

    try {
        combo.add(option, null);
    }
    catch (error) {
        combo.add(option); // IE only
    }
}

function onModelSelected() {

    var combo = document.getElementById("combo");

    var modelURN = combo.options[combo.selectedIndex].modelURN;

    clearCurrentModel();

    _viewer = createViewer();

    loadDocument(_auth, _viewer, modelURN);
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function onViewerItemSelected(event) {

    var dbIdArray = event.dbIdArray;

    for (var i = 0; i < dbIdArray.length; i++) {

        var dbId = dbIdArray[i];

    }
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function authenticate(accessToken) {

    var options = {};

    options.env = "AutodeskProduction";

    options.accessToken = accessToken;

    initializeEnvironmentVariable(options);
    initializeServiceEndPoints();

    return initializeAuth(null, options);
}

///////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////
function initialize() {

    $('#layoutContainer').layout({

        north__paneSelector: "#paneNorth",
        center__paneSelector: "#paneCenter",
        south__paneSelector: "#paneSouth",

        north__resizable: false,

        center__size: 500,

        center__onresize: function () {
            _viewer.resize();
        }
    });

    //$.getJSON(window.location + '/api/ViewData/credentials', function (accessToken) {
    $.getJSON(window.location + '/Home/credentials', function (accessToken) {

        _auth = authenticate(accessToken);

        $.getJSON(window.location + '/Home/models', function (result) {

            var models = JSON.parse(result);

            models.forEach(function (model) {
                addToCombo(model.Name, model.ModelId);
            });

            var urn = combo.options[0].modelURN;

            _viewer = createViewer();

            loadDocument(_auth, _viewer, urn);
        })
    })
    .done(function () { })
    .fail(function (jqXHR, textStatus, errorThrown) {
        //alert('getJSON request failed! ' + textStatus);
    })
    .always();

    $(document).keyup(function (e) {

        // esc
        if (e.keyCode == 27) {

            _viewer.getObjectTree(function (rootComponent) {

                _viewer.docstructure.handleAction(
                    ["focus"],
                    rootComponent.dbId);
            });
        }
    });
}