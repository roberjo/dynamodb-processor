// Custom Swagger UI JavaScript

// Add custom header to all requests
const originalFetch = window.fetch;
window.fetch = function(url, options = {}) {
    options.headers = {
        ...options.headers,
        'X-Request-ID': generateRequestId(),
        'X-Custom-Header': 'custom-value'
    };
    return originalFetch(url, options);
};

// Generate a unique request ID
function generateRequestId() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

// Add custom response handling
const originalExecute = window.ui.execute;
window.ui.execute = function() {
    const result = originalExecute.apply(this, arguments);
    result.then(response => {
        console.log('Response:', response);
        // Add custom response handling here
    });
    return result;
};

// Add custom UI enhancements
document.addEventListener('DOMContentLoaded', function() {
    // Add copy button to code blocks
    document.querySelectorAll('pre code').forEach(block => {
        const button = document.createElement('button');
        button.className = 'copy-button';
        button.textContent = 'Copy';
        button.onclick = function() {
            navigator.clipboard.writeText(block.textContent);
            button.textContent = 'Copied!';
            setTimeout(() => {
                button.textContent = 'Copy';
            }, 2000);
        };
        block.parentNode.insertBefore(button, block);
    });

    // Add collapsible sections
    document.querySelectorAll('.opblock').forEach(block => {
        const summary = block.querySelector('.opblock-summary');
        const content = block.querySelector('.opblock-body');
        if (summary && content) {
            summary.style.cursor = 'pointer';
            summary.onclick = function() {
                content.style.display = content.style.display === 'none' ? 'block' : 'none';
            };
        }
    });

    // Add search functionality
    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.placeholder = 'Search operations...';
    searchInput.className = 'search-input';
    searchInput.oninput = function(e) {
        const searchTerm = e.target.value.toLowerCase();
        document.querySelectorAll('.opblock').forEach(block => {
            const text = block.textContent.toLowerCase();
            block.style.display = text.includes(searchTerm) ? 'block' : 'none';
        });
    };
    document.querySelector('.swagger-ui').insertBefore(
        searchInput,
        document.querySelector('.swagger-ui .wrapper')
    );

    // Add dark mode toggle
    const darkModeToggle = document.createElement('button');
    darkModeToggle.textContent = 'Toggle Dark Mode';
    darkModeToggle.className = 'dark-mode-toggle';
    darkModeToggle.onclick = function() {
        document.body.classList.toggle('dark-mode');
    };
    document.querySelector('.swagger-ui').insertBefore(
        darkModeToggle,
        document.querySelector('.swagger-ui .wrapper')
    );
});

// Add custom styles for new elements
const style = document.createElement('style');
style.textContent = `
    .copy-button {
        position: absolute;
        top: 5px;
        right: 5px;
        padding: 5px 10px;
        background-color: #2c3e50;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
    }
    .copy-button:hover {
        background-color: #34495e;
    }
    .search-input {
        width: 100%;
        padding: 10px;
        margin: 10px 0;
        border: 1px solid #ddd;
        border-radius: 4px;
    }
    .dark-mode-toggle {
        position: fixed;
        top: 10px;
        right: 10px;
        padding: 5px 10px;
        background-color: #2c3e50;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
        z-index: 1000;
    }
    .dark-mode {
        background-color: #1a1a1a;
        color: #fff;
    }
    .dark-mode .swagger-ui {
        background-color: #1a1a1a;
        color: #fff;
    }
    .dark-mode .opblock {
        background-color: #2c2c2c;
        border-color: #3c3c3c;
    }
    .dark-mode .model {
        background-color: #2c2c2c;
    }
`;
document.head.appendChild(style); 