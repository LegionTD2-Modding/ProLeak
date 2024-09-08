const ProLeak = require('./ProLeak');
const readline = require('readline');

const api = new ProLeak();

api.registerHandler("MethodCall", (event, params) => console.log(`Method called: ${params.Method}`));

api.registerHandler(["MethodCall", "UnityMessage"], 
  (event, params) => console.log(`Event occurred: ${event}`));

function globalHandler(event, params, unplug) {
  console.log(`Global event: ${event}`);
  if (event === "EndOfGame") {
    unplug();
  }
}

api.registerGlobalHandler(globalHandler);

api.registerGlobalHandler((event, params) => console.log(`Event occurred: ${event}`));

function twoParamHandler(event, params) {
  console.log(`Event: ${event}, Params: ${JSON.stringify(params)}`);
}

function threeParamHandler(event, params, stop) {
  console.log(`Event: ${event}, Params: ${JSON.stringify(params)}`);
  if (event === "EndOfGame") {
    stop();
  }
}

api.registerHandler("MethodCall", twoParamHandler);
api.registerHandler(["UnityMessage", "EndOfGame"], threeParamHandler);

api.connect()
  .then(() => {
    api.startLeaking();

    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout
    });

    function promptUser() {
      rl.question("Enter command (start/stop/quit): ", (command) => {
        switch (command.toLowerCase()) {
          case "start":
            api.startLeaking();
            break;
          case "stop":
            api.stopLeaking();
            break;
          case "quit":
            rl.close();
            api.stopLeaking();
            api.disconnect();
            return;
        }
        promptUser();
      });
    }

    promptUser();
  })
  .catch((error) => {
    console.error(error);
    api.disconnect();
  });
