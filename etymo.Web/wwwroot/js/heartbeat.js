// wwwroot/js/heartbeat.js
let heartbeatInterval = null;
export function startHeartbeat(intervalMs) {
    // Clear any existing interval
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
    }
    // Set up the interval
    heartbeatInterval = setInterval(() => {
        fetch('/heartbeat', {
            method: 'GET',
            mode: 'cors',
            credentials: 'include',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => {
                if (!response.ok) {
                    // Handle unauthorized (401) or other error status codes
                    if (response.status === 401) {
                        console.debug('Heartbeat: User not authenticated');
                        return { status: 'unauthenticated' };
                    }
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.debug('Heartbeat response:', data);
            })
            .catch(error => {
                console.error('Heartbeat error:', error);
                // Optionally: you could have logic here to stop the heartbeat after multiple failures
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