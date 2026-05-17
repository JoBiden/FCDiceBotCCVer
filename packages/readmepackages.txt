websocket-sharp.dll is manually compiled from source (with a longer packet fragmentation length, to stop it from crashing on sending messages of 1000 characters +). Now it should handle packages around 120k characters.

in bin\ there is the old websocket-sharp.dll incase this one doesn't work properly.