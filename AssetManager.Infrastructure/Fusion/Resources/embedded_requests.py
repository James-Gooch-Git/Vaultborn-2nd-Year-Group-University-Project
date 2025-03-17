"""
Simple HTTP client that mimics the basic functionality of the requests library
but uses only the standard library.
"""

import urllib.request
import urllib.error
import urllib.parse
import json
import ssl
import time

def get(url, headers=None, params=None, timeout=30):
    """Simple GET request"""
    return request('GET', url, headers=headers, params=params, timeout=timeout)

def post(url, headers=None, json=None, data=None, timeout=30):
    """Simple POST request"""
    return request('POST', url, headers=headers, json=json, data=data, timeout=timeout)

def put(url, headers=None, data=None, timeout=30):
    """Simple PUT request"""
    return request('PUT', url, headers=headers, data=data, timeout=timeout)

def request(method, url, headers=None, params=None, json=None, data=None, timeout=30):
    """Make an HTTP request"""
    # Add URL parameters if provided
    if params:
        query_string = urllib.parse.urlencode(params)
        url = f"{url}?{query_string}"
    
    # Create the request object
    req = urllib.request.Request(url, method=method)
    
    # Add headers
    if headers:
        for key, value in headers.items():
            req.add_header(key, value)
    
    # Add data if provided
    body = None
    if json is not None:
        body = json_dumps(json).encode('utf-8')
        req.add_header('Content-Type', 'application/json')
    elif data is not None:
        if isinstance(data, dict):
            body = urllib.parse.urlencode(data).encode('utf-8')
            req.add_header('Content-Type', 'application/x-www-form-urlencoded')
        else:
            body = data if isinstance(data, bytes) else data.encode('utf-8')
    
    # Make the request and capture the response
    try:
        context = ssl._create_unverified_context()
        response = urllib.request.urlopen(req, data=body, timeout=timeout, context=context)
        return Response(response)
    except urllib.error.HTTPError as e:
        return Response(e)

def json_dumps(obj):
    """Convert object to JSON string"""
    return json.dumps(obj)

class Response:
    """Simple response object to mimic requests.Response"""
    def __init__(self, response):
        self.response = response
        self.status_code = response.status if hasattr(response, 'status') else response.code
        self._content = None
        self._text = None
        self._json = None
    
    @property
    def content(self):
        """Get response content as bytes"""
        if self._content is None:
            self._content = self.response.read()
        return self._content
    
    @property
    def text(self):
        """Get response content as text"""
        if self._text is None:
            self._text = self.content.decode('utf-8')
        return self._text
    
    def json(self):
        """Parse response as JSON"""
        if self._json is None:
            self._json = json.loads(self.text)
        return self._json