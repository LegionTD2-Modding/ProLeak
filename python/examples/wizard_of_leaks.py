from ProLeak import *

api = ProLeak()

# And I kept the best for last.
# Introducing Interceptors
# They are regular handlers, except they can change the parameters or block events
# BEFORE those events reach the real game engine

def powerful_interceptor(event, params):
    # Intercept some events before they reach the code of good ol' landlubber Lisk
    if event == "MethodCall" and params["Method"] == "SomeSpecificMethod":
        # Arrr, just tweak them params a bit, and I’ll be returnin’ ye some events so crafty
        # Even the finest pirate wouldn’t spot 'em! Har har!
        params["Arguments"][0] = "intercepted_value"
        return params
    elif event == "MethodCall" and params["Method"] == "SomeOtherMethod":
        # Ye can also just block the signals, if ye like, matey
        return None
    # Or ye can just let 'em be, if it pleases ye
    return params

# Same syntax, just replacing register_handler by register_interceptor
# Also, interceptors do not have an unplug parameter
api.register_interceptor("MethodCall", powerful_interceptor)
api.connect()
api.start_leaking()

try:
    while True:
        command = input("Enter command (start/stop/quit): ").lower()
        if command == "start":
            api.start_leaking()
        elif command == "stop":
            api.stop_leaking()
        elif command == "quit":
            break
except KeyboardInterrupt:
    pass
finally:

    # To stop, you first deactivate the API server
    api.stop_leaking()

    # And then you disconnect
    api.disconnect()
