const ProLeak = require('./ProLeak');

function myHandler(event, params, unplug) {
  console.log(`Received: ${event}`);

  if (event === "MethodCall") {
    console.log(`Method ${params.Method} was called`);
  }

  if (event === "EndOfGame") {
    unplug();  // Exit the loop based on some condition
  }
}

// Plug your handler
new ProLeak().plug(myHandler);
