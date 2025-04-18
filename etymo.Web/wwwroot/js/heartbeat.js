// wwwroot/js/heartbeat.js

let heartbeatInterval = null;

export function startHeartbeat(intervalMs) {
    // Clear any existing interval
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
    }

    // Set up the interval
    heartbeatInterval = setInterval(() => {
        fetch('/api/heartbeat', {
            method: 'GET',
            mode: 'cors',
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => response.json())
            .then(data => {
                console.debug('Heartbeat response:', data);
            })
            .catch(error => {
                console.error('Heartbeat error:', error);
            });
    }, intervalMs);

    console.debug(`Heartbeat started with interval: ${intervalMs}ms`);
    return true;
}

export function stopHeartbeat() {
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
        heartbeatInterval = null;
        console.debug('Heartbeat stopped');
    }
}