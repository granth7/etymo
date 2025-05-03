let heartbeatInterval = null;
let dotNetHelper = null;

// Initialize with a reference to the .NET component
export function initializeHeartbeat(dotNetReference) {
    dotNetHelper = dotNetReference;
    console.debug('Heartbeat component initialized');
}

export function startHeartbeat(intervalMs) {
    // Clear any existing interval
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
    }

    // Make sure dotNetHelper is set
    if (!dotNetHelper) {
        console.error('Heartbeat not initialized correctly. Call initializeHeartbeat first.');
        return false;
    }

    // Set up the interval
    heartbeatInterval = setInterval(async () => {
        try {
            // Call the .NET method directly instead of using fetch
            const result = await dotNetHelper.invokeMethodAsync('TriggerHeartbeat');
            console.debug('Heartbeat response:', result);
            
            // Optional: Handle different statuses
            if (result.status === 'unauthenticated') {
                console.debug('Heartbeat: User not authenticated');
                // You could redirect to login page or take other actions
            }
        } catch (error) {
            console.error('Heartbeat error:', error);
            // Optionally: you could have logic here to stop the heartbeat after multiple failures
        }
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