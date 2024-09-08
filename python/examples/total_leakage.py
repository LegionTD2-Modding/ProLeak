from ProLeak import *

api = ProLeak()

# Register a handler for a single event
api.register_handler("MethodCall", lambda event, params, unplug: print(f"Method called: {params['Method']}"))

# Register a handler for multiple events
api.register_handler(["MethodCall", "UnityMessage"],
                         lambda event, params, unplug: print(f"Event occurred: {event}"))

# Register a global handler with stop function
def global_handler(event, params, unplug):
    print(f"Global event: {event}")
    if event == "EndOfGame":
        unplug()

api.register_global_handler(global_handler)

# Global handler with lambda
api.register_global_handler(lambda event, params, unplug: print(f"Event occurred: {event}"))

# Global handler with lambda without unplug
api.register_global_handler(lambda event, params: print(f"Global event: {event}"))

# Handler without unplug
def two_param_handler(event, params):
    print(f"Event: {event}, Params: {params}")

# Handler with unplug, named stop here
def three_param_handler(event, params, stop):
    print(f"Event: {event}, Params: {params}")
    if event == "EndOfGame":
        stop()

# Register handlers with or without unplug, with multiple events
api.register_handler("MethodCall", two_param_handler)
api.register_handler(["UnityMessage", "EndOfGame"], three_param_handler)

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
