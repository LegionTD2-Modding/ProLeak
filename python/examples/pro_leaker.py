from ProLeak import *
import json

# You can use ProLeak at a lower level
# The details will not be hidden anymore so you'll have to do it manually
# But it's still very easy, and you'll be rewarded with much more flexibility

# Define similar handler, but without a stop parameter
def on_method_call(event, params, _):
    print(f"Method call: {params['Method']} of {params['DeclaringType']}")

def on_unity_message(event, params, _):
    print(f"Unity message: {params['Method']}")

def on_all_events(event, params, _):
    print(f"Event: {event}")
    print(json.dumps(params, indent=2))
    print("---")


# First create the API
api = ProLeak()

# Register some events and attach callback to them
api.register_handler("MethodCall", on_method_call)
api.register_handler("UnityMessage", on_unity_message)

# You can also go sicko mode and register them all
api.register_global_handler(on_all_events)

# After that, we'll need to start the API server

# First, you connect to the API server
api.connect()

# Then you ask it to start leaking events
api.start_leaking()

# You'll then receive events. A small loop like this can be useful
# It'll allow you to control the program, stop it, restart etc
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
