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

    public static string GetModelViewerHtml(string encodedUrn, string accessToken)
    {
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=Edge' />
    <title>Forge Viewer with Environment Skybox</title>
    <script src='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/viewer3D.js'></script>
    <link rel='stylesheet' href='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/style.css' type='text/css'>
    <style>
        html, body {{ margin: 0; padding: 0; height: 100%; overflow: hidden; }}
        #forgeViewer {{ width: 100%; height: 100vh; }}
        #skyboxControls {{ position: absolute; top: 10px; left: 10px; background: rgba(255,255,255,0.9); padding: 10px; border-radius: 5px; z-index: 1000; }}
        .skyboxButton {{ margin-right: 5px; padding: 5px 10px; cursor: pointer; }}
        #debug {{ position: fixed; bottom: 0; left: 0; right: 0; height: 100px; background: rgba(0,0,0,0.7); color: #0f0; font-family: monospace; overflow-y: auto; display: none; z-index: 1000; }}
    </style>
</head>
<body>
    <div id='skyboxControls'>
        <button id='skybox1' class='skyboxButton'>Space Skybox</button>
        <button id='skybox2' class='skyboxButton'>Sunset Skybox</button>
        <button id='skybox3' class='skyboxButton'>Volcano Skybox</button>
        <button id='noSkybox' class='skyboxButton'>No Skybox</button>
        <button id='setScene' class='skyboxButton'>Set Camera + BG</button>
        <button id='toggleLogs' class='skyboxButton'>Show Logs</button>
    </div>
    <div id='forgeViewer'></div>
    <div id='debug'></div>

    <script>
        function debug(msg) {{
            console.log(msg);
            var d = document.getElementById('debug');
            var line = document.createElement('div');
            line.textContent = msg;
            d.appendChild(line);
            d.scrollTop = d.scrollHeight;
        }}

        class SkyboxExtension extends Autodesk.Viewing.Extension {{
            constructor(viewer, options) {{
                super(viewer, options);
                this.viewer = viewer;
                this.skyboxes = {{
                    space: [
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Fright.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Fleft.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Ftop.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Fbottom.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Ffront.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/space%2Fback.png?alt=media'
                    ],
                    sunset: [
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Fright.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Fleft.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Ftop.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Fbottom.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Ffront.png?alt=media',
                        'https://firebasestorage.googleapis.com/v0/b/skybox-test-a3ffd.appspot.com/o/sunset%2Fback.png?alt=media'
                    ],
                    volcano: [
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/px.png',
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/nx.png',
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/py.png',
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/ny.png',
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/pz.png',
                        'https://my-skybox-images.s3.eu-north-1.amazonaws.com/nz.png'
                    ]
                }};
            }}

            load() {{
                debug('SkyboxExtension loaded');
                return true;
            }}

            unload() {{
                this.viewer.impl.scene.environmentMap = null;
                this.viewer.setLightPreset(1);
                this.viewer.impl.invalidate(true, true, true);
                return true;
            }}

            setSkybox(name) {{
                const viewer = this.viewer;
                const urls = this.skyboxes[name];
                if (!urls || !viewer) return;

                let THREE = Autodesk?.Viewing?.Private?.THREE;
                if (!THREE || !THREE.TextureLoader || !THREE.CubeTexture) {{
                    debug('Missing THREE.TextureLoader or CubeTexture');
                    return;
                }}

                const loader = new THREE.TextureLoader();
                loader.setCrossOrigin('Anonymous');

                const faces = [];
                let loaded = 0;
                for (let i = 0; i < 6; i++) {{
                    loader.load(urls[i], function(texture) {{
                        faces[i] = texture.image;
                        loaded++;
                        if (loaded === 6) {{
                            const cubeMap = new THREE.CubeTexture(faces);
                            cubeMap.needsUpdate = true;
                            viewer.impl.setEnvironmentMap(cubeMap);
                            viewer.setLightPreset(0);
                            viewer.impl.invalidate(true, true, true);
                            debug('Skybox applied: ' + name);
                        }}
                    }}, undefined, function(err) {{
                        debug('Error loading texture ' + urls[i]);
                    }});
                }}
            }}
        }}

        Autodesk.Viewing.theExtensionManager.registerExtension('SkyboxExtension', SkyboxExtension);

        var viewer;
        var skyboxExt;
        var options = {{
            env: 'AutodeskProduction',
            api: 'derivativeV2',
            getAccessToken: function(onTokenReady) {{
                onTokenReady('{accessToken}', 3599);
            }}
        }};
        var documentId = 'urn:{encodedUrn}';

        document.getElementById('skybox1').onclick = () => skyboxExt && skyboxExt.setSkybox('space');
        document.getElementById('skybox2').onclick = () => skyboxExt && skyboxExt.setSkybox('sunset');
        document.getElementById('skybox3').onclick = () => skyboxExt && skyboxExt.setSkybox('volcano');
        document.getElementById('noSkybox').onclick = () => {{
            skyboxExt?.unload();
            debug('Skybox cleared');
        }};
document.getElementById('setScene').onclick = () => {{
    if (!viewer) return;

    const THREE = Autodesk.Viewing.Private.THREE;

    // Camera setup
    const position = new THREE.Vector3(20, 15, 20); // camera position
    const target = new THREE.Vector3(0, 0, 0);      // look-at target
    viewer.navigation.setView(position, target);
    debug('Camera set to custom view.');

    // Load texture
    const loader = new THREE.TextureLoader();
    loader.setCrossOrigin('anonymous');
    loader.load('https://my-skybox-images.s3.eu-north-1.amazonaws.com/jaygooch_00588_magical_castle_dnd_artwork_--v_6.1_247bd40c-b633-4955-8744-359909b4d7c8_3.png', function(texture) {{
        const geometry = new THREE.PlaneGeometry(30, 20); // width, height
        const material = new THREE.MeshBasicMaterial({{ map: texture, side: THREE.DoubleSide }});
        const plane = new THREE.Mesh(geometry, material);

        plane.position.set(0, 10, -30); // place behind the model
        plane.name = 'CustomBackgroundPlane';

        viewer.impl.scene.add(plane);
        viewer.impl.invalidate(true, true, true);

        debug('Image plane added to viewer.');
    }}, undefined, function(err) {{
        debug('Failed to load image.');
    }});
}};


        document.getElementById('toggleLogs').onclick = function() {{
            const debugEl = document.getElementById('debug');
            if (debugEl.style.display === 'none') {{
                debugEl.style.display = 'block';
                this.textContent = 'Hide Logs';
            }} else {{
                debugEl.style.display = 'none';
                this.textContent = 'Show Logs';
            }}
        }};

        Autodesk.Viewing.Initializer(options, function() {{
            viewer = new Autodesk.Viewing.GuiViewer3D(document.getElementById('forgeViewer'));
            viewer.start();
            viewer.loadExtension('SkyboxExtension').then(ext => {{
                skyboxExt = ext;
            }});
            Autodesk.Viewing.Document.load(documentId, function(doc) {{
                var defaultModel = doc.getRoot().getDefaultGeometry();
                viewer.loadDocumentNode(doc, defaultModel).then(() => {{
                    viewer.fitToView();
                }});
            }}, function(code, msg) {{
                debug('Error loading model: ' + code + ' - ' + msg);
            }});
        }});
    </script>
</body>
</html>";
    }
}
