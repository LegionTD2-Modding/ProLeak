from ProLeak import *

# You can leverage more control by defining the handler before calling ProLeak
# Create a callback with parameters:
#   event: the name of the event
#   params: an array of parameters linked to the event
#   unplug: a function to call to exit the loop

# This will exit the loop when an event "EndOfGame" is received

def my_handler(event, params, unplug):

    print(f"Received: {event}")

    if event == "MethodCall":
        print(f"Method {params['Method']} was called")

    if event == "EndOfGame":
        unplug()  # Exit the loop based on some condition

# Plug your handler
ProLeak().plug(my_handler)