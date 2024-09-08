from ProLeak import *

# You can use ProLeak with a single line of code
# This will print all events in real time

ProLeak().plug(lambda event, params, _: print(f"{event}: {params}"))