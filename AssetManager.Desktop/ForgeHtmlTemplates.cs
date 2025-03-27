using System.Windows.Automation.Text;

public static class ForgeHtmlTemplates
{
    public static string GetPdfViewerHtml(string encodedUrn, string accessToken)
    {
        // Paste your PDF viewer HTML template here, use `encodedUrn` and `accessToken` with interpolation
        return  $@"<!DOCTYPE html>
                  <html>
                  <head>
                      <meta charset='UTF-8'>
                      <meta http-equiv='X-UA-Compatible' content='IE=Edge' />
                      <title>PDF Viewer</title>
                      <script src='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/viewer3D.js'></script>
                      <link rel='stylesheet' href='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/style.css' type='text/css'>
                      <style>
                          body, html {{ height: 100%; margin: 0; padding: 0; overflow: hidden; }}
                          #toolbar {{ position: absolute; top: 10px; left: 10px; z-index: 1000; background: rgba(255,255,255,0.9); padding: 5px; border-radius: 5px; }}
                          #forgeViewer {{ width: 100%; height: 100vh; position: relative; }}
                          #log {{ position: fixed; bottom: 10px; left: 10px; right: 10px; height: 150px; background: rgba(0,0,0,0.7); color: white; overflow: auto; padding: 10px; font-family: monospace; z-index: 1000; display: block; }}
                          #pageLabel {{ margin-left: 20px; font-weight: bold; }}
                          #pageControls {{ margin-left: 10px; }}
                          .pageButton {{ margin: 0 2px; padding: 2px 8px; cursor: pointer; }}
                      </style>
                  </head>
                  <body>
                      <div id='toolbar'>
                          <label for='pageSelect'>Page:</label>
                          <select id='pageSelect'></select>
                          <span id='pageControls'>
                              <button id='prevPage' class='pageButton'>←</button>
                              <button id='nextPage' class='pageButton'>→</button>
                          </span>
                          <span id='pageLabel'>Page 1 of 1</span>
                          <button id='toggleLog' style='margin-left: 20px;'>Hide Logs</button>
                      </div>
                      <div id='forgeViewer'></div>
                      <div id='log'></div>

                      <script>
                          // Debug logging function
                          function log(message) {{
                              console.log(message);
                              var logDiv = document.getElementById('log');
                              var date = new Date();
                              var timestamp = date.getHours() + ':' + date.getMinutes() + ':' + date.getSeconds() + '.' + date.getMilliseconds();
                              var formattedMsg = '[' + timestamp + '] ' + (typeof message === 'object' ? JSON.stringify(message) : message);
      
                              var line = document.createElement('div');
                              line.textContent = formattedMsg;
                              logDiv.appendChild(line);
                              logDiv.scrollTop = logDiv.scrollHeight;
                          }}
                     

                          document.getElementById('toggleLog').addEventListener('click', function() {{
                              var logDiv = document.getElementById('log');
                              if (logDiv.style.display === 'none') {{
                                  logDiv.style.display = 'block';
                                  this.textContent = 'Hide Logs';
                              }} else {{
                                  logDiv.style.display = 'none';
                                  this.textContent = 'Show Logs';
                              }}
                          }});

                          log('Script started');
  
                          // Handle any errors
                          window.addEventListener('error', function(event) {{
                              log('ERROR: ' + event.message + ' at ' + event.filename + ':' + event.lineno);
                          }});

                          var viewer;
                          var doc;
                          var viewables = [];
                          var currentModel = null;
                          var currentPageIndex = 0;

                          var options = {{
                              env: 'AutodeskProduction',
                              api: 'derivativeV2',
                              getAccessToken: function(onTokenReady) {{
                                  log('Getting access token');
                                  onTokenReady('{accessToken}', 3599);
                              }}
                          }};

                          var documentId = 'urn:{encodedUrn}';
                          log('Document ID: ' + documentId);

                          try {{
                              log('Initializing Autodesk Viewer...');
                              Autodesk.Viewing.Initializer(options, function () {{
                                  log('Viewer initialized successfully');
          
                                  try {{
                                      // Create the viewer
                                      var viewerDiv = document.getElementById('forgeViewer');
                                      log('Creating viewer in div: ' + (viewerDiv ? 'Found' : 'Not found'));
              
                                      // Initialize with PDF extension and enable debugging
                                      viewer = new Autodesk.Viewing.GuiViewer3D(viewerDiv, {{ 
                                          extensions: ['Autodesk.PDF'],
                                          loaderExtensions: {{ pdf: true }},
                                          enablePDFJS: true
                                      }});
              
                                      log('Starting viewer...');
                                      var startedViewer = viewer.start();
                                      log('Viewer start result: ' + startedViewer);
              
                                      // Set background color for better visibility
                                      viewer.setBackgroundColor(250, 250, 250, 250, 250, 250);
              
                                      log('Loading document: ' + documentId);
              
                                      Autodesk.Viewing.Document.load(
                                          documentId, 
                                          // onLoadSuccess
                                          function (loadedDoc) {{
                                              log('Document loaded successfully');
                                              doc = loadedDoc;
                      
                                              try {{
                                                  // Log document structure for debugging
                                                  var rootItem = doc.getRoot();
                                                  log('Root item: ' + (rootItem ? 'Found' : 'Not found'));
                          
                                                  if (rootItem) {{
                                                      log('Root item type: ' + rootItem.type);
                                                      log('Children count: ' + (rootItem.children ? rootItem.children.length : 0));
                                                  }}
                          
                                                  // Method 1: Direct recursive search
                                                  var items = [];
                          
                                                  function getAllLeafNodes(node) {{
                                                      if (!node) return;
                                                      log('Examining node: ' + (node.data ? node.data.guid : 'No GUID'));
                              
                                                      if (node.children && node.children.length > 0) {{
                                                          log('Node has ' + node.children.length + ' children');
                                                          node.children.forEach(getAllLeafNodes);
                                                      }} else {{
                                                          log('Leaf node found. Role: ' + (node.data ? node.data.role : 'unknown'));
                                                          if (node.data && (node.data.role === '2d' || node.data.role === 'thumbnail')) {{
                                                              items.push(node);
                                                              log('Found PDF page: ' + (node.data.name || 'Unnamed') + ', GUID: ' + node.data.guid);
                                                          }}
                                                      }}
                                                  }}
                          
                                                  getAllLeafNodes(doc.getRoot());
                                                  log('Method 1 - Found ' + items.length + ' pages');
                          
                                                  // Method 2: Use API method (sometimes more reliable)
                                                  try {{
                                                      var items2 = Autodesk.Viewing.Document.getSubItemsWithProperties(
                                                          doc.getRoot(), 
                                                          {{ 'type': 'geometry', 'role': '2d' }}, 
                                                          true
                                                      );
                                                      log('Method 2 - Found ' + (items2 ? items2.length : 0) + ' pages');
                                                  }} catch (e) {{
                                                      log('Error in Method 2: ' + e.message);
                                                      items2 = [];
                                                  }}
                          
                                                  // Use whichever method found pages
                                                  viewables = items.length > 0 ? items : (items2 && items2.length > 0 ? items2 : []);
                          
                                                  // If we found viewables, set up the page selector
                                                  if (viewables && viewables.length > 0) {{
                                                      log('Found ' + viewables.length + ' viewables to display');
                                                      populatePageSelector(viewables);
                                                      loadPage(0); // Load first page
                                                      updatePageLabel(1, viewables.length);
                                                      setupPageControls(viewables.length);
                                                  }} else {{
                                                      // No 2D viewables found, try default geometry
                                                      log('No 2D viewables found, trying default geometry...');
                              
                                                      // Try to get default geometry
                                                      var defaultGeometry;
                                                      try {{
                                                          defaultGeometry = doc.getRoot().getDefaultGeometry();
                                                          log('Default geometry: ' + (defaultGeometry ? 'Found' : 'Not found'));
                                                      }} catch (e) {{
                                                          log('Error getting default geometry: ' + e.message);
                                                      }}
                              
                                                      if (defaultGeometry) {{
                                                          log('Loading default geometry: ' + (defaultGeometry.guid || 'unknown'));
                                                          viewer.loadDocumentNode(doc, defaultGeometry)
                                                              .then(function(model) {{
                                                                  log('Default geometry loaded successfully');
                                                                  currentModel = model;
                                                              }})
                                                              .catch(function(err) {{
                                                                  log('Error loading default geometry: ' + err.message);
                                                              }});
                                                      }} else {{
                                                          log('No default geometry found, trying bubble geometry...');
                                  
                                                          // Last-ditch effort: Try to find any geometry to display
                                                          try {{
                                                              var bubbleNode = doc.getRoot();
                                                              var allViewables = [];
                                      
                                                              // Try to find any geometry
                                                              function findAnyGeometry(node) {{
                                                                  if (!node) return;
                                          
                                                                  if (node.data && node.data.type === 'geometry') {{
                                                                      allViewables.push(node);
                                                                      log('Found a geometry node: ' + (node.data.name || 'Unnamed'));
                                                                  }}
                                          
                                                                  if (node.children && node.children.length > 0) {{
                                                                      node.children.forEach(findAnyGeometry);
                                                                  }}
                                                              }}
                                      
                                                              findAnyGeometry(bubbleNode);
                                                              log('Found ' + allViewables.length + ' total geometry items');
                                      
                                                              if (allViewables.length > 0) {{
                                                                  var anyViewable = allViewables[0];
                                                                  log('Attempting to load any viewable: ' + (anyViewable.data ? anyViewable.data.guid : 'unknown'));
                                                                  viewer.loadDocumentNode(doc, anyViewable)
                                                                      .then(function(model) {{
                                                                          log('Viewable loaded successfully');
                                                                          currentModel = model;
                                                                      }})
                                                                      .catch(function(err) {{
                                                                          log('Error loading viewable: ' + err.message);
                                                                      }});
                                                              }} else {{
                                                                  log('No viewables found at all. Check translation status.');
                                          
                                                                  // Final attempt - try to access the first page directly
                                                                  log('Attempting one final method to find viewable...');
                                                                  try {{
                                                                      if (bubbleNode && bubbleNode.children && bubbleNode.children.length > 0) {{
                                                                          var firstChild = bubbleNode.children[0];
                                                                          log('Loading first child node as last resort');
                                                                          viewer.loadDocumentNode(doc, firstChild)
                                                                              .then(function(model) {{
                                                                                  log('First child node loaded successfully');
                                                                                  currentModel = model;
                                                                              }})
                                                                              .catch(function(err) {{
                                                                                  log('Error loading first child node: ' + err.message);
                                                                              }});
                                                                      }}
                                                                  }} catch (e) {{
                                                                      log('Final attempt failed: ' + e.message);
                                                                  }}
                                                              }}
                                                          }} catch (e) {{
                                                              log('Error in last-ditch effort: ' + e.message);
                                                          }}
                                                      }}
                                                  }}
                                              }} catch (docError) {{
                                                  log('Error processing document: ' + docError.message);
                                              }}
                                          }}, 
                                          // onLoadError
                                          function (errorCode, errorMsg) {{
                                              log('Error loading document: ' + errorCode + ' - ' + errorMsg);
                                          }},
                                          // options
                                          {{ checkAEC: false }}
                                      );
                                  }} catch (e) {{
                                      log('Error in viewer creation/document loading: ' + e.message);
                                  }}
                              }});
                          }} catch (e) {{
                              log('Fatal error initializing viewer: ' + e.message);
                          }}

                          function updatePageLabel(current, total) {{
                              document.getElementById('pageLabel').textContent = 'Page ' + current + ' of ' + total;
                          }}

                          function setupPageControls(totalPages) {{
                              var prevButton = document.getElementById('prevPage');
                              var nextButton = document.getElementById('nextPage');
      
                              prevButton.addEventListener('click', function() {{
                                  if (currentPageIndex > 0) {{
                                      currentPageIndex--;
                                      loadPage(currentPageIndex);
                                      document.getElementById('pageSelect').value = currentPageIndex;
                                      updatePageLabel(currentPageIndex + 1, totalPages);
                                  }}
                              }});
      
                              nextButton.addEventListener('click', function() {{
                                  if (currentPageIndex < totalPages - 1) {{
                                      currentPageIndex++;
                                      loadPage(currentPageIndex);
                                      document.getElementById('pageSelect').value = currentPageIndex;
                                      updatePageLabel(currentPageIndex + 1, totalPages);
                                  }}
                              }});
                          }}

                          function populatePageSelector(viewables) {{
                              try {{
                                  var select = document.getElementById('pageSelect');
                                  select.innerHTML = ''; // Clear any existing options
          
                                  viewables.forEach(function (v, i) {{
                                      var option = document.createElement('option');
                                      option.value = i;
              
                                      // Create better page labels
                                      var pageName = 'Page ' + (i + 1);
              
                                      // If the viewable has metadata, try to use that
                                      if (v.data) {{
                                          if (v.data.name && v.data.name !== 'Initial') {{
                                              pageName = v.data.name;
                                          }}
                  
                                          // Log some additional data for debugging
                                          log('Page ' + (i + 1) + ' metadata: ' +
                                              'name=' + (v.data.name || 'N/A') +
                                              ', guid=' + (v.data.guid || 'N/A'));
                                      }}
              
                                      option.text = pageName;
                                      select.appendChild(option);
                                      log('Added option for ' + pageName);
                                  }});
          
                                  select.addEventListener('change', function() {{
                                      var pageIndex = parseInt(this.value);
                                      currentPageIndex = pageIndex;
                                      log('Page selector changed to index: ' + pageIndex);
                                      loadPage(pageIndex);
                                      updatePageLabel(pageIndex + 1, viewables.length);
                                  }});
                              }} catch (e) {{
                                  log('Error in populatePageSelector: ' + e.message);
                              }}
                          }}
  
                          function loadPage(index) {{
                              try {{
                                  if (!doc || !viewer || !viewables[index]) {{
                                      log('Cannot load page: Viewer or document not ready');
                                      return;
                                  }}

                                  if (currentModel) {{
                                      log('Unloading current model');
                                      viewer.unloadModel(currentModel);
                                      currentModel = null;
                                  }}

                                  const viewable = viewables[index];
                                  log('Loading page ' + (index + 1) + ', viewable ID: ' + (viewable.data ? viewable.data.guid : 'unknown'));

                                  viewer.loadDocumentNode(doc, viewable)
                                      .then(function(model) {{
                                          currentModel = model;
                                          log('Successfully loaded page ' + (index + 1));
                                          currentPageIndex = index;

                                          // Fit to view
                                          viewer.fitToView();
                                      }})
                                      .catch(function(err) {{
                                          log('Failed to load page: ' + (err.message || JSON.stringify(err)));
                                      }});
                              }} catch (e) {{
                                  log('Error in loadPage: ' + e.message);
                              }}
                          }}
                      </script>
                  </body>
                  </html>";
    }



    public static string GetEnhancedModelViewerHtml(string encodedUrn, string accessToken)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=Edge' />
    <title>Forge Viewer</title>
    <script src='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/viewer3D.js'></script>
    <link rel='stylesheet' href='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/style.css' type='text/css'>
    <style>
        body, html {{ height: 100%; margin: 0; padding: 0; overflow: hidden; }}
        #forgeViewer {{ width: 100%; height: 100vh; position: relative; }}
        #log {{ position: fixed; bottom: 10px; left: 10px; right: 10px; height: 150px; background: rgba(0,0,0,0.7); color: white; overflow: auto; padding: 10px; font-family: monospace; z-index: 1000; display: none; }}
        #skyboxControls {{ position: absolute; top: 10px; left: 10px; z-index: 1000; background: rgba(255,255,255,0.9); padding: 10px; border-radius: 5px; }}
        .skyboxButton {{ margin-right: 5px; cursor: pointer; padding: 5px 10px; }}
        #snapshotButton {{ background-color: #4CAF50; color: white; font-weight: bold; border: none; }}
        #snapshotButton:hover {{ background-color: #45a049; }}
    </style>
</head>
<body>
    <div id='skyboxControls'>
     <button id='skybox1' class='skyboxButton'>Castle Skybox</button>
     <button id='skybox2' class='skyboxButton'>Dwarven Skybox</button>
     <button id='skybox3' class='skyboxButton'>Elven Skybox</button>
     <button id='skybox4' class='skyboxButton'>Floating Island Skybox</button>
     <button id='skybox5' class='skyboxButton'>War Torn Skybox</button>
     <button id='skybox6' class='skyboxButton'>Pirate Skybox</button>
     <button id='noSkybox' class='skyboxButton'>No Skybox</button>
     <button id='snapshotButton' class='skyboxButton'>Take Snapshot</button>
     <button id='toggleLogs' class='skyboxButton'>Show Logs</button>
</div>
    <div id='forgeViewer'></div>
    <div id='log'></div>

    <script>
        // Debug logging function
        function log(message) {{
            console.log(message);
            var logDiv = document.getElementById('log');
            var date = new Date();
            var timestamp = date.getHours() + ':' + date.getMinutes() + ':' + date.getSeconds();
            var line = document.createElement('div');
            line.textContent = '[' + timestamp + '] ' + (typeof message === 'object' ? JSON.stringify(message) : message);
            logDiv.appendChild(line);
            logDiv.scrollTop = logDiv.scrollHeight;
        }}

        // Handle any errors
        window.addEventListener('error', function(event) {{
            log('ERROR: ' + event.message + ' at ' + event.filename + ':' + event.lineno);
        }});

        log('Loading 3D model viewer with skybox support...');

        // Define the Skybox Extension
        // Streamlined SkyboxExtension to avoid interference with Forge Viewer internals
class SkyboxExtension extends Autodesk.Viewing.Extension {{
    constructor(viewer, options) {{
        super(viewer, options);
        this.viewer = viewer;
        this.name = 'SkyboxExtension';

        // Define skybox images (6 images for each skybox - positive/negative x, y, z)
        this.skyboxes = {{
                   castle: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/px.png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/nx.png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/py.png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/ny.png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pz.png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/nz.png'
  ],
  dwarven: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+px+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+nx+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+py+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+ny+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+pz+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/dwarven+nz+(1).png'
  ],
  elven: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+px+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+nx+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+py+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+ny+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+pz+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/elven+nz+(1).png'
  ],
  floatingIsland: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+px+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+nx+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+py+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+ny+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+pz+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/floating+island+nz+(1).png'
  ],
  warTorn: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+px+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+nx+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+py+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+ny+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+pz+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/war+torn+nz+(1).png'
  ],
  pirate: [
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+px+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+nx+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+py+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+ny+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+pz+(1).png',
    'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pirate+nz+(1).png'
  ]

        }};

        // Store resources for cleanup
        this.skyboxMesh = null;
        this.cubeTexture = null;
        this.sphereMesh = null;
    }}

    load() {{
        log('SkyboxExtension loaded');
        return true;
    }}

    unload() {{
        this.removeSkybox();
        log('SkyboxExtension unloaded');
        return true;
    }}

    setSkybox(skyboxName) {{
        const viewer = this.viewer;
        
        log('Setting skybox: ' + skyboxName);
        
        if (!viewer || !viewer.impl) {{
            log('Viewer not available for skybox');
            return;
        }}
        
        const urls = this.skyboxes[skyboxName];
        if (!urls) {{
            log('Skybox not found: ' + skyboxName);
            return;
        }}
        
        // Remove existing skybox if any
        this.removeSkybox();
        
        try {{
            // Step 1: Find THREE.js - using exact same approach that worked before
            let THREE = null;
            
            // First try to get THREE from Autodesk namespace
            try {{
                THREE = window.Autodesk.Viewing.THREE || Autodesk.Viewing.Private.THREE;
            }} catch (e) {{
                log('Could not get THREE from Autodesk namespace: ' + e.message);
            }}
            
            // If not found, try alternative locations
            if (!THREE) {{
                log('THREE.js not found in Autodesk namespace. Trying alternative approach...');
                
                // Try to find THREE in other possible locations
                const possibleTHREE = viewer.impl.THREE || 
                                    (viewer.impl.runtime && viewer.impl.runtime.THREE) ||
                                    window.THREE;
                
                if (!possibleTHREE) {{
                    log('THREE.js could not be found in any known location');
                    return;
                }}
                
                // Use the found THREE instance
                log('Found THREE.js in alternative location');
                THREE = possibleTHREE;
            }}
            
            log('Using THREE.js version: ' + (THREE.REVISION || 'unknown'));
            
            // Method 1: Try to create a background scene for the skybox
            // This is the most compatible approach that avoids ray casting issues
            try {{
                // Create a separate scene for the skybox to avoid interference with Forge's ray casting
                if (!this.skyboxScene) {{
                    this.skyboxScene = new THREE.Scene();
                    log('Created separate skybox scene');
                }}
                
                // Test with a sphere approach (often works better with older THREE.js)
                const sphereGeometry = new THREE.SphereGeometry(10000, 24, 24);
                
                // Create materials - simpler approach with a single texture on a sphere
                // Choose one representative image for the skybox (front image works well)
                const frontImage = urls[4]; // Front image index
                
                log('Loading skybox texture from: ' + frontImage);
                
                // Create texture and material
                let texture;
                if (THREE.ImageUtils && THREE.ImageUtils.loadTexture) {{
                    texture = THREE.ImageUtils.loadTexture(frontImage);
                }} else if (THREE.TextureLoader) {{
                    const loader = new THREE.TextureLoader();
                    loader.setCrossOrigin('anonymous');
                    texture = loader.load(frontImage);
                }}
                
                if (!texture) {{
                    log('Failed to load texture');
                    return;
                }}
                
                const sphereMaterial = new THREE.MeshBasicMaterial({{
                    map: texture,
                    side: THREE.BackSide
                }});
                
                // Create sphere mesh
                const sphereMesh = new THREE.Mesh(sphereGeometry, sphereMaterial);
                
                // Add to Forge's overlay scene to avoid ray casting issues
                if (viewer.impl.overlayScenes && !viewer.impl.overlayScenes.skybox) {{
                    // Create a new overlay scene for the skybox
                    log('Creating skybox overlay scene');
                    viewer.impl.createOverlayScene('skybox');
                }}
                
                // Add to the overlay scene
                if (viewer.impl.overlayScenes && viewer.impl.overlayScenes.skybox) {{
                    log('Adding skybox to overlay scene');
                    viewer.impl.addOverlay('skybox', sphereMesh);
                    
                    // Store for cleanup
                    this.sphereMesh = sphereMesh;
                    
                    // Set appropriate lighting
                    viewer.setLightPreset(0);
                    
                    // Force redraw
                    viewer.impl.invalidate(true, true, true);
                    
                    log('Skybox added to overlay scene');
                    return; // Exit if successful
                }} else {{
                    log('Overlay scene not available, trying fallback method');
                }}
            }} catch (e) {{
                log('Error using overlay scene approach: ' + e.message);
                log('Trying fallback method');
            }}
            
            // Method 2: Try with a simpler approach using just a single texture
            try {{
                // Create a simple full-screen background plane
                const planeGeometry = new THREE.PlaneGeometry(100000, 100000);
                
                // Choose the front image as the background
                const backgroundImage = urls[4]; // Front image
                
                log('Loading background image: ' + backgroundImage);
                
                // Create texture and material
                let texture;
                if (THREE.ImageUtils && THREE.ImageUtils.loadTexture) {{
                    texture = THREE.ImageUtils.loadTexture(backgroundImage);
                }} else if (THREE.TextureLoader) {{
                    const loader = new THREE.TextureLoader();
                    loader.setCrossOrigin('anonymous');
                    texture = loader.load(backgroundImage);
                }}
                
                if (!texture) {{
                    log('Failed to load background texture');
                    return;
                }}
                
                const planeMaterial = new THREE.MeshBasicMaterial({{
                    map: texture,
                    depthWrite: false,
                    depthTest: false
                }});
                
                // Create plane mesh and position it to face the camera
                const planeMesh = new THREE.Mesh(planeGeometry, planeMaterial);
                
                // Position far behind the camera
                const camera = viewer.impl.camera;
                const cameraDirection = new THREE.Vector3(0, 0, -1);
                cameraDirection.applyQuaternion(camera.quaternion);
                
                planeMesh.position.copy(camera.position);
                planeMesh.position.sub(cameraDirection.multiplyScalar(50000));
                planeMesh.lookAt(camera.position);
                
                // Add to scene
                viewer.impl.scene.add(planeMesh);
                
                // Store for cleanup
                this.planeMesh = planeMesh;
                
                // Set appropriate lighting
                viewer.setLightPreset(0);
                
                // Force redraw
                viewer.impl.invalidate(true, true, true);
                
                log('Simple background plane added');
                
                // Add camera change listener to update plane position
                this.onCameraChange = () => {{
                    const cameraDir = new THREE.Vector3(0, 0, -1);
                    cameraDir.applyQuaternion(camera.quaternion);
                    
                    planeMesh.position.copy(camera.position);
                    planeMesh.position.sub(cameraDir.multiplyScalar(50000));
                    planeMesh.lookAt(camera.position);
                    
                    viewer.impl.invalidate(true, true, true);
                }};
                
                // Add event listener for camera change
                viewer.addEventListener(Autodesk.Viewing.CAMERA_CHANGE_EVENT, this.onCameraChange);
                
            }} catch (e) {{
                log('Error creating simple background: ' + e.message);
            }}
            
        }} catch (err) {{
            log('Fatal error in setSkybox: ' + (err.message || err));
        }}
    }}

    removeSkybox() {{
        const viewer = this.viewer;

        if (!viewer || !viewer.impl) {{
            log('Viewer not available for cleanup');
            return;
        }}

        try {{
            // Remove overlay scene if used
            if (viewer.impl.overlayScenes && viewer.impl.overlayScenes.skybox) {{
                if (this.sphereMesh) {{
                    viewer.impl.removeOverlay('skybox', this.sphereMesh);
                }}
                viewer.impl.removeOverlayScene('skybox');
                log('Removed skybox overlay scene');
            }}
            
            // Remove sphere mesh if it exists
            if (this.sphereMesh) {{
                if (this.sphereMesh.material && this.sphereMesh.material.map) {{
                    this.sphereMesh.material.map.dispose();
                }}
                if (this.sphereMesh.material) {{
                    this.sphereMesh.material.dispose();
                }}
                if (this.sphereMesh.geometry) {{
                    this.sphereMesh.geometry.dispose();
                }}
                this.sphereMesh = null;
                log('Disposed sphere mesh resources');
            }}
            
            // Remove plane mesh if it exists
            if (this.planeMesh) {{
                viewer.impl.scene.remove(this.planeMesh);
                
                if (this.planeMesh.material && this.planeMesh.material.map) {{
                    this.planeMesh.material.map.dispose();
                }}
                if (this.planeMesh.material) {{
                    this.planeMesh.material.dispose();
                }}
                if (this.planeMesh.geometry) {{
                    this.planeMesh.geometry.dispose();
                }}
                this.planeMesh = null;
                log('Removed and disposed plane mesh');
                
                // Remove camera change listener
                if (this.onCameraChange) {{
                    viewer.removeEventListener(Autodesk.Viewing.CAMERA_CHANGE_EVENT, this.onCameraChange);
                    this.onCameraChange = null;
                }}
            }}

            // Restore default lighting
            viewer.setLightPreset(1);

            // Force redraw
            viewer.impl.invalidate(true, true, true);

            log('Skybox completely removed');
        }} catch (e) {{
            log('Error removing skybox: ' + e);
        }}
    }}
}}

// Register the extension
Autodesk.Viewing.theExtensionManager.registerExtension('SkyboxExtension', SkyboxExtension);

        // Register the extension with Forge Viewer
        
        var viewer;
        var skyboxExt;

        var options = {{
            env: 'AutodeskProduction',
            api: 'derivativeV2',
            getAccessToken: function(onTokenReady) {{
                log('Getting access token...');
                onTokenReady('{accessToken}', 3599);
            }}
        }};

        var documentId = 'urn:{encodedUrn}';
        log('Document ID: ' + documentId);

        // Set up button handlers
       document.getElementById('skybox1').addEventListener('click', function() {{
    log('Castle skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('castle');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('skybox2').addEventListener('click', function() {{
    log('Dwarven skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('dwarven');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('skybox3').addEventListener('click', function() {{
    log('Elven skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('elven');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('skybox4').addEventListener('click', function() {{
    log('Floating Island skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('floatingIsland');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('skybox5').addEventListener('click', function() {{
    log('War Torn skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('warTorn');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('skybox6').addEventListener('click', function() {{
    log('Pirate skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.setSkybox('pirate');
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('noSkybox').addEventListener('click', function() {{
    log('No skybox button clicked');
    if (skyboxExt) {{
        skyboxExt.removeSkybox();
    }} else {{
        log('Skybox extension not available yet');
    }}
}});

document.getElementById('toggleLogs').addEventListener('click', function() {{
    var logDiv = document.getElementById('log');
    if (logDiv.style.display === 'none') {{
        logDiv.style.display = 'block';
        this.textContent = 'Hide Logs';
    }} else {{
        logDiv.style.display = 'none';
        this.textContent = 'Show Logs';
    }}
}});

// Add snapshot button handler
document.getElementById('snapshotButton').addEventListener('click', function() {{
    log('Snapshot button clicked');
    if (viewer) {{
        try {{
            // Use Forge Viewer's built-in screenshot capability
            viewer.getScreenShot(viewer.container.clientWidth, viewer.container.clientHeight, function(blobUrl) {{
                log('Screenshot captured');
                
                // Create a temporary link element to trigger download
                var link = document.createElement('a');
                link.href = blobUrl;
                
                // Generate a filename with current date/time
                var date = new Date();
                var timestamp = date.getFullYear() + 
                               '-' + ('0' + (date.getMonth() + 1)).slice(-2) + 
                               '-' + ('0' + date.getDate()).slice(-2) + 
                               '_' + ('0' + date.getHours()).slice(-2) + 
                               '-' + ('0' + date.getMinutes()).slice(-2) + 
                               '-' + ('0' + date.getSeconds()).slice(-2);
                link.download = 'model_snapshot_' + timestamp + '.png';
                
                // Append to body, click and remove
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                
                log('Screenshot downloaded');
            }});
        }} catch (error) {{
            log('Error capturing screenshot: ' + error.message);
            
            // Fallback method if viewer.getScreenShot fails
            try {{
                log('Trying alternative screenshot method...');
                
                // Get the canvas element from the viewer
                const canvas = viewer.canvas;
                if (!canvas) {{
                    throw new Error('Canvas not available');
                }}
                
                // Convert canvas to data URL
                const dataUrl = canvas.toDataURL('image/png');
                
                // Create download link
                const link = document.createElement('a');
                link.href = dataUrl;
                
                // Generate filename
                var date = new Date();
                var timestamp = date.getFullYear() + 
                               '-' + ('0' + (date.getMonth() + 1)).slice(-2) + 
                               '-' + ('0' + date.getDate()).slice(-2) + 
                               '_' + ('0' + date.getHours()).slice(-2) + 
                               '-' + ('0' + date.getMinutes()).slice(-2) + 
                               '-' + ('0' + date.getSeconds()).slice(-2);
                link.download = 'model_snapshot_' + timestamp + '.png';
                
                // Trigger download
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                
                log('Screenshot downloaded using alternative method');
            }} catch (fallbackError) {{
                log('All screenshot methods failed: ' + fallbackError.message);
                alert('Unable to take a screenshot. Please try again or use your browser\'s screenshot function.');
            }}
        }}
    }} else {{
        log('Viewer not available for screenshot');
        alert('Viewer not ready. Please wait for the model to load completely.');
    }}
}});

        // Initialize viewer
        Autodesk.Viewing.Initializer(options, function() {{
            log('Viewer initialized');

            var viewerDiv = document.getElementById('forgeViewer');
            viewer = new Autodesk.Viewing.GuiViewer3D(viewerDiv);

            log('Starting viewer...');
            var started = viewer.start();
            log('Viewer started: ' + started);

            // Load the skybox extension
            log('Loading skybox extension...');
            viewer.loadExtension('SkyboxExtension').then(extension => {{
                skyboxExt = extension;
                log('Skybox extension loaded successfully and stored');
            }}).catch(err => {{
                log('Error loading skybox extension: ' + err.message);
            }});

            log('Loading document...');
            Autodesk.Viewing.Document.load(documentId, function(doc) {{
                log('Document loaded successfully');
                var defaultModel = doc.getRoot().getDefaultGeometry();

                if (defaultModel) {{
                    log('Loading default model: ' + defaultModel.guid);
                    viewer.loadDocumentNode(doc, defaultModel)
                        .then(function(model) {{
                            log('Model loaded successfully');
                            viewer.fitToView();
                        }})
                        .catch(function(error) {{
                            log('Error loading model: ' + error.message);
                        }});
                }} else {{
                    log('No default geometry found');
                }}
            }}, function(errorCode, errorMsg) {{
                log('Error loading document: ' + errorCode + ' - ' + errorMsg);
            }});
        }});
    </script>
</body>
</html>";
    }
}
