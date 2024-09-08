const ProLeak = require('./ProLeak');
const readline = require('readline');

function onMethodCall(event, params) {
  console.log(`Method call: ${params.Method} of ${params.DeclaringType}`);
}

function onUnityMessage(event, params) {
  console.log(`Unity message: ${params.Method}`);
}

function onAllEvents(event, params) {
  console.log(`Event: ${event}`);
  console.log(JSON.stringify(params, null, 2));
  console.log("---");
}

const api = new ProLeak();

api.registerHandler("MethodCall", onMethodCall);
api.registerHandler("UnityMessage", onUnityMessage);
api.registerGlobalHandler(onAllEvents);

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
